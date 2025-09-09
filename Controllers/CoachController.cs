using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Microsoft.Data.SqlClient;
using FITNSS.Models;
using BCrypt.Net;
using Microsoft.IdentityModel.Tokens;
using Microsoft.AspNetCore.Http;
using Newtonsoft.Json;
using Microsoft.AspNetCore.Mvc.Rendering;
using System.Security.Cryptography.X509Certificates;

namespace FITNSS.Controllers
{
    public class CoachController : Controller
    {
        private readonly IConfiguration _configuration;

        private readonly IWebHostEnvironment _env;
        public CoachController(IConfiguration configuration,
                                 ILogger<CoachController> logger,
                                 IWebHostEnvironment env)
        {
            _configuration = configuration;
            _env = env;
        }
        public IActionResult Dashboard()
        {
            // Sample data – ideally galing sa database
            var days = new[] { "Mon", "Tue", "Wed", "Thu", "Fri", "Sat", "Sun" };
            var heartbeats = new[] { 8000, 9500, 9000, 8800, 10000, 9200, 9100 };

            // Gamitin Newtonsoft.Json para gawing JSON string
            ViewBag.Days = JsonConvert.SerializeObject(days);
            ViewBag.Heartbeats = JsonConvert.SerializeObject(heartbeats);

            string userId = HttpContext.Session.GetString("userId");
            if (string.IsNullOrEmpty(userId))
                return RedirectToAction("Login", "Login");

            StudentProfileModel model = new StudentProfileModel();
            model.userId = userId;

            using (SqlConnection conn = new SqlConnection(_configuration.GetConnectionString("DefaultConnectionString")))
            {
                conn.Open();
                string query = @"SELECT users_id, profile_image, firstname, lastname, email, course, year_level, 
                                contact_number, emergency_contact, date_of_birth, age, sport
                         FROM student_profile
                         WHERE users_id = @UserId";

                using (SqlCommand cmd = new SqlCommand(query, conn))
                {
                    cmd.Parameters.AddWithValue("@UserId", Convert.ToInt32(userId));
                    using (SqlDataReader reader = cmd.ExecuteReader())
                    {

                        if (reader.Read())
                        {
                            model.userId = reader["users_id"].ToString();
                            model.FirstName = reader["firstname"].ToString();
                            model.LastName = reader["lastname"].ToString();
                            model.Email = reader["email"].ToString();
                            model.Course = reader["course"].ToString();
                            model.YearLevel = reader["year_level"].ToString();
                            model.ContactNumber = reader["contact_number"].ToString();
                            model.EmergencyContact = reader["emergency_contact"].ToString();
                            model.DateOfBirth = reader["date_of_birth"].ToString();
                            model.Age = reader["age"].ToString();
                            model.Sport = reader["sport"].ToString();
                            model.ProfileImagePath = reader["profile_image"].ToString();


                        }
                    }
                }

                string queryTotalAthlete = @"SELECT COUNT(id) totalathlete
                         FROM student_athlete_profile
                         WHERE coach_id = @UserId AND status = 1";

                using (SqlCommand cmd = new SqlCommand(queryTotalAthlete, conn))
                {
                    cmd.Parameters.AddWithValue("@UserId", Convert.ToInt32(userId));

                    object result = cmd.ExecuteScalar(); // mas simple since SUM returns single value
                    int totalAthlete = (result != DBNull.Value) ? Convert.ToInt32(result) : 0;

                    ViewBag.TotalAthlete = totalAthlete;
                }

                string queryPendingVerification = @"SELECT COUNT(id) pendingverification
                         FROM student_athlete_profile
                         WHERE coach_id = @UserId AND status = 0";

                using (SqlCommand cmd = new SqlCommand(queryPendingVerification, conn))
                {
                    cmd.Parameters.AddWithValue("@UserId", Convert.ToInt32(userId));

                    object result = cmd.ExecuteScalar(); // mas simple since SUM returns single value
                    int PendingVerification = (result != DBNull.Value) ? Convert.ToInt32(result) : 0;

                    ViewBag.PendingVerification = PendingVerification;
                }


                string queryTotalKmThisWeek = @"
    SELECT 
        ISNULL(SUM(km), 0) AS TotalKm,
        COUNT(DISTINCT CAST([date] AS DATE)) AS TotalDays
    FROM student_running
    WHERE users_id = @UserId
      AND [date] >= DATEADD(DAY, - (DATEDIFF(DAY, 0, CAST(GETDATE() AS date)) % 7), CAST(GETDATE() AS date))
      AND [date] <  DATEADD(DAY, 7 - (DATEDIFF(DAY, 0, CAST(GETDATE() AS date)) % 7), CAST(GETDATE() AS date))
";


                using (var cmd = new SqlCommand(queryTotalKmThisWeek, conn))
                {
                    cmd.Parameters.AddWithValue("@UserId", Convert.ToInt32(userId));
                    using (var reader = cmd.ExecuteReader())
                    {
                        if (reader.Read())
                        {
                            model.TotalKm = reader["TotalKm"] != DBNull.Value ? Convert.ToDecimal(reader["TotalKm"]) : 0;
                            int KmTotalDays = reader["TotalDays"] != DBNull.Value ? Convert.ToInt32(reader["TotalDays"]) : 0;

                            // ✅ save as "X/7" format
                            model.KmTotalDays = $"{KmTotalDays}/7";

                            // ✅ compute percentage base sa 5km target
                            decimal targetKm = 5;
                            decimal percentage = (model.TotalKm / targetKm) * 100;

                            if (percentage > 100)
                                percentage = 100;

                            model.KmPercentage = percentage;
                        }
                    }
                }




                string queryTotalHoursThisWeek = @"
SELECT 
    ISNULL(SUM(hours), 0) AS TotalHours,
    COUNT(DISTINCT CAST([date] AS DATE)) AS TotalDays
FROM student_sleeping
WHERE users_id = @UserId
  AND [date] >= DATEADD(DAY, 1 - DATEPART(WEEKDAY, GETDATE()), CAST(GETDATE() AS date))
  AND [date] <  DATEADD(DAY, 8 - DATEPART(WEEKDAY, GETDATE()), CAST(GETDATE() AS date))
";

                using (var cmd = new SqlCommand(queryTotalHoursThisWeek, conn))
                {
                    cmd.Parameters.AddWithValue("@UserId", Convert.ToInt32(userId));
                    using (var reader = cmd.ExecuteReader())
                    {
                        if (reader.Read())
                        {
                            var totalHoursFromDb = reader["TotalHours"] != DBNull.Value ? Convert.ToDecimal(reader["TotalHours"]) : 0;
                            int SleepTotalDays = reader["TotalDays"] != DBNull.Value ? Convert.ToInt32(reader["TotalDays"]) : 0;

                            model.SleepTotalDays = $"{SleepTotalDays}/7";

                            decimal targetHours = 7 * 7; // 49 hours
                            decimal percentage = (totalHoursFromDb / targetHours) * 100;

                            if (percentage > 100)
                                percentage = 100;

                            model.HoursPercentage = Math.Round(percentage, 0);
                        }
                    }
                }




                string queryTotalCaloriesThisWeek = @"
SELECT 
    ISNULL(SUM(calories), 0) AS TotalCalories,
    COUNT(DISTINCT CAST([date] AS DATE)) AS TotalDays
FROM student_calories
WHERE users_id = @UserId
  AND [date] >= DATEADD(DAY, 1 - DATEPART(WEEKDAY, GETDATE()), CAST(GETDATE() AS date))
  AND [date] <  DATEADD(DAY, 8 - DATEPART(WEEKDAY, GETDATE()), CAST(GETDATE() AS date))
";

                using (var cmd = new SqlCommand(queryTotalCaloriesThisWeek, conn))
                {
                    cmd.Parameters.AddWithValue("@UserId", Convert.ToInt32(userId));
                    using (var reader = cmd.ExecuteReader())
                    {
                        if (reader.Read())
                        {
                            var totalCaloriesFromDb = reader["TotalCalories"] != DBNull.Value ? Convert.ToDecimal(reader["TotalCalories"]) : 0;
                            int CaloriesTotalDays = reader["TotalDays"] != DBNull.Value ? Convert.ToInt32(reader["TotalDays"]) : 0;

                            model.CaloriesTotalDays = $"{CaloriesTotalDays}/7";

                            // ✅ target 1,000 calories/day × 7 days
                            decimal targetCalories = 1000 * 7; // 7,000 calories
                            decimal percentage = (totalCaloriesFromDb / targetCalories) * 100;

                            if (percentage > 100)
                                percentage = 100;

                            model.CaloriesPercentage = Math.Round(percentage, 0); // separate property
                        }
                    }
                }






            }

            return View(model);
        }

        public IActionResult Athlete()
        {
            return View();
        }

        [HttpGet]
        public IActionResult GetUsers()
        {
            List<object> users = new List<object>();

            string connectionString = _configuration.GetConnectionString("DefaultConnectionString");

            using (SqlConnection conn = new SqlConnection(connectionString))
            {
                conn.Open();
                string query = "SELECT firstname, lastname, email FROM users";
                SqlCommand cmd = new SqlCommand(query, conn);
                SqlDataReader reader = cmd.ExecuteReader();

                while (reader.Read())
                {
                    users.Add(new
                    {
                        firstname = reader["firstname"].ToString(),
                        lastname = reader["lastname"].ToString(),
                        email = reader["email"].ToString()
                    });
                }
            }

            return Json(users);
        }

        [HttpGet]
        public IActionResult Profile()
        {
            string userId = HttpContext.Session.GetString("userId");
            if (string.IsNullOrEmpty(userId))
                return RedirectToAction("Login", "Login");

            CoachProfileModel model = new CoachProfileModel();
            model.userId = userId;

            using (SqlConnection conn = new SqlConnection(_configuration.GetConnectionString("DefaultConnectionString")))
            {
                conn.Open();

                // get profile
                string query = @"SELECT users_id, profile_image, firstname, lastname, email, course, year_level, 
                        contact_number, date_of_birth, age, sport, position
                        FROM coach_profile
                        WHERE users_id = @UserId";

                using (SqlCommand cmd = new SqlCommand(query, conn))
                {
                    cmd.Parameters.AddWithValue("@UserId", Convert.ToInt32(userId));
                    using (SqlDataReader reader = cmd.ExecuteReader())
                    {
                        if (reader.Read())
                        {
                            model.userId = reader["users_id"].ToString();
                            model.FirstName = reader["firstname"].ToString();
                            model.LastName = reader["lastname"].ToString();
                            model.Email = reader["email"].ToString();
                            model.Course = reader["course"].ToString();
                            model.YearLevel = reader["year_level"].ToString();
                            model.ContactNumber = reader["contact_number"].ToString();
                            model.DateOfBirth = reader["date_of_birth"].ToString();
                            model.Age = reader["age"].ToString();
                            model.Sport = reader["sport"].ToString();
                            model.Position = reader["position"].ToString();
                            model.ProfileImagePath = reader["profile_image"].ToString();
                        }
                    }
                }

                // get expertise
                string expertiseQuery = "SELECT expertise FROM coach_expertise WHERE users_id = @UserId";
                using (SqlCommand cmd = new SqlCommand(expertiseQuery, conn))
                {
                    cmd.Parameters.AddWithValue("@UserId", Convert.ToInt32(userId));
                    using (SqlDataReader reader = cmd.ExecuteReader())
                    {
                        while (reader.Read())
                        {
                            model.ExpertiseList.Add(reader["expertise"].ToString());
                        }
                    }
                }
            }

            return View(model);
        }


        [HttpPost]
        public IActionResult Profile(CoachProfileModel model, List<string> Expertise, IFormFile ProfilePhoto)
        {
            string userId = HttpContext.Session.GetString("userId");
            if (string.IsNullOrEmpty(userId))
                return RedirectToAction("Login", "Login");

            if (ProfilePhoto != null && ProfilePhoto.Length > 0)
            {
                var safeFileName = Path.GetFileNameWithoutExtension(ProfilePhoto.FileName);
                safeFileName = string.Join("_", safeFileName.Split(Path.GetInvalidFileNameChars()));
                var extension = Path.GetExtension(ProfilePhoto.FileName);
                var fileName = $"{userId}_{safeFileName}{extension}";

                var imagesFolder = Path.Combine(_env.WebRootPath, "images");
                if (!Directory.Exists(imagesFolder))
                    Directory.CreateDirectory(imagesFolder);

                var filePath = Path.Combine(imagesFolder, fileName);
                using (var stream = new FileStream(filePath, FileMode.Create))
                    ProfilePhoto.CopyTo(stream);

                model.ProfileImagePath = "/images/" + fileName;
            }

            using (SqlConnection conn = new SqlConnection(_configuration.GetConnectionString("DefaultConnectionString")))
            {
                conn.Open();

                // update profile
                string query = @"UPDATE coach_profile SET 
                    firstname=@FirstName,
                    lastname=@LastName,
                    email=@Email,
                    course=@Course,
                    year_level=@YearLevel,
                    contact_number=@ContactNumber,
                    date_of_birth=@DateOfBirth,
                    age=@Age,
                    position=@Position,
                    sport=@Sport,
                    profile_image=@ProfileImagePath
                WHERE users_id=@UserId";

                using (SqlCommand cmd = new SqlCommand(query, conn))
                {
                    cmd.Parameters.AddWithValue("@FirstName", model.FirstName ?? "");
                    cmd.Parameters.AddWithValue("@LastName", model.LastName ?? "");
                    cmd.Parameters.AddWithValue("@Email", model.Email ?? "");
                    cmd.Parameters.AddWithValue("@Course", model.Course ?? "");
                    cmd.Parameters.AddWithValue("@YearLevel", model.YearLevel ?? "");
                    cmd.Parameters.AddWithValue("@ContactNumber", model.ContactNumber ?? "");
                    cmd.Parameters.AddWithValue("@DateOfBirth", model.DateOfBirth ?? "");
                    cmd.Parameters.AddWithValue("@Age", model.Age ?? "");
                    cmd.Parameters.AddWithValue("@Sport", model.Sport ?? "");
                    cmd.Parameters.AddWithValue("@Position", model.Position ?? "");
                    cmd.Parameters.AddWithValue("@ProfileImagePath", model.ProfileImagePath ?? "");
                    cmd.Parameters.AddWithValue("@UserId", Convert.ToInt32(model.userId));

                    cmd.ExecuteNonQuery();
                }

                // delete old expertise
                string deleteSql = "DELETE FROM coach_expertise WHERE users_id=@UserId";
                using (SqlCommand cmd = new SqlCommand(deleteSql, conn))
                {
                    cmd.Parameters.AddWithValue("@UserId", Convert.ToInt32(model.userId));
                    cmd.ExecuteNonQuery();
                }

                // insert new expertise
                foreach (var exp in Expertise)
                {
                    if (!string.IsNullOrWhiteSpace(exp))
                    {
                        string insertSql = "INSERT INTO coach_expertise (users_id, expertise) VALUES (@UserId, @Expertise)";
                        using (SqlCommand cmd = new SqlCommand(insertSql, conn))
                        {
                            cmd.Parameters.AddWithValue("@UserId", Convert.ToInt32(model.userId));
                            cmd.Parameters.AddWithValue("@Expertise", exp);
                            cmd.ExecuteNonQuery();
                        }
                    }
                }
            }

            ViewBag.Message = "Profile updated successfully!";
            return RedirectToAction("Profile"); // reload para makita updates
        }






        [HttpGet]
        public IActionResult Bmi()
        {
            string userId = HttpContext.Session.GetString("userId");
            if (string.IsNullOrEmpty(userId))
                return RedirectToAction("Login", "Login");

            StudentProfileModel model = new StudentProfileModel();
            model.userId = userId;

            using (SqlConnection conn = new SqlConnection(_configuration.GetConnectionString("DefaultConnectionString")))
            {
                conn.Open();
                string query = @"SELECT users_id, age, weight, height
                         FROM student_profile
                         WHERE users_id = @UserId";

                using (SqlCommand cmd = new SqlCommand(query, conn))
                {
                    cmd.Parameters.AddWithValue("@UserId", Convert.ToInt32(userId));
                    using (SqlDataReader reader = cmd.ExecuteReader())
                    {

                        if (reader.Read())
                        {
                            model.userId = reader["users_id"].ToString();
                            model.Age = reader["age"].ToString();
                            model.Weight = reader["weight"].ToString();
                            model.Height = reader["height"].ToString();


                        }
                    }
                }
            }



            return View(model);
        }

       


        [HttpGet]
        public IActionResult TrainingSchedule()
        {

            string userId = HttpContext.Session.GetString("userId");
            if (string.IsNullOrEmpty(userId))
                return RedirectToAction("Login", "Login");

            CoachProfileModel model = new CoachProfileModel();
            model.userId = userId;

            using (SqlConnection conn = new SqlConnection(_configuration.GetConnectionString("DefaultConnectionString")))
            {
                conn.Open();
                string query = @"SELECT users_id, profile_image, firstname, lastname, email, course, year_level, 
                        contact_number, date_of_birth, age, sport, position
                        FROM coach_profile
                        WHERE users_id = @UserId";

                using (SqlCommand cmd = new SqlCommand(query, conn))
                {
                    cmd.Parameters.AddWithValue("@UserId", Convert.ToInt32(userId));
                    using (SqlDataReader reader = cmd.ExecuteReader())
                    {
                        if (reader.Read())
                        {
                            model.userId = reader["users_id"].ToString();
                            model.FirstName = reader["firstname"].ToString();
                            model.LastName = reader["lastname"].ToString();
                            model.Email = reader["email"].ToString();
                            model.Course = reader["course"].ToString();
                            model.YearLevel = reader["year_level"].ToString();
                            model.ContactNumber = reader["contact_number"].ToString();
                            model.DateOfBirth = reader["date_of_birth"].ToString();
                            model.Age = reader["age"].ToString();
                            model.Sport = reader["sport"].ToString();
                            model.Position = reader["position"].ToString();
                            model.ProfileImagePath = reader["profile_image"].ToString();
                        }
                    }
                }

                var studentModel = new CoachAthleteApplicationModel();
                studentModel.userId = userId;

                string coachApprovedQuery = @"
        SELECT sap.id, u.firstname, u.lastname, sp.email, sp.profile_image, sp.contact_number, sp.date_of_birth, sp.age, sp.height, sp.weight,
        sp.course, sp.year_level, sp.emergency_contact, sp.sport, sp.status
        FROM student_athlete_profile sap
        INNER JOIN users u ON sap.users_id = u.id
        INNER JOIN student_profile sp ON sp.users_id = u.id
        WHERE sap.coach_id = @UserId AND sap.status = 1";

                using (SqlCommand roleCmd = new SqlCommand(coachApprovedQuery, conn))
                {
                    roleCmd.Parameters.AddWithValue("@UserId", Convert.ToInt32(userId));

                    using (SqlDataReader roleReader = roleCmd.ExecuteReader())
                    {
                        while (roleReader.Read())
                        {
                            studentModel.StudentListApproved.Add(new StudentAthleteProfileModel
                            {
                                studentAthleteProfileId = roleReader["id"].ToString(),
                                FullName = roleReader["firstname"].ToString() + " " + roleReader["lastname"].ToString(),
                                Email = roleReader["email"].ToString(),
                                Photo = roleReader["profile_image"].ToString(),
                                ContactNumber = roleReader["contact_number"].ToString(),
                                DateOfBirth = roleReader["date_of_birth"].ToString(),
                                Age = roleReader["age"].ToString(),
                                Height = roleReader["height"].ToString(),
                                Weight = roleReader["weight"].ToString(),
                                Course = roleReader["course"].ToString(),
                                YearLevel = roleReader["year_level"].ToString(),
                                EmergencyContact = roleReader["emergency_contact"].ToString(),
                                Sport = roleReader["sport"].ToString(),
                                Status = roleReader["status"].ToString(),
                            });
                        }
                    }
                }

                ViewBag.StudentAthleteApproved = studentModel;
            }
            return View(model);
        }


        [HttpPost]
        public IActionResult TrainingSchedule(CoachTrainingScheduleModel model2, List<string> studentAthleteProfileId)
        {
            string userId = HttpContext.Session.GetString("userId");
            if (string.IsNullOrEmpty(userId))
                return RedirectToAction("Login", "Login");

            CoachProfileModel model = new CoachProfileModel();
            model.userId = userId;

            using (SqlConnection conn = new SqlConnection(_configuration.GetConnectionString("DefaultConnectionString")))
            {
                conn.Open();

                // Get coach profile
                string query = @"SELECT users_id, profile_image, firstname, lastname, email, course, year_level, 
                contact_number, date_of_birth, age, sport, position
                FROM coach_profile
                WHERE users_id = @UserId";

                using (SqlCommand cmd = new SqlCommand(query, conn))
                {
                    cmd.Parameters.AddWithValue("@UserId", Convert.ToInt32(userId));
                    using (SqlDataReader reader = cmd.ExecuteReader())
                    {
                        if (reader.Read())
                        {
                            model.userId = reader["users_id"].ToString();
                            model.FirstName = reader["firstname"].ToString();
                            model.LastName = reader["lastname"].ToString();
                            model.Email = reader["email"].ToString();
                            model.Course = reader["course"].ToString();
                            model.YearLevel = reader["year_level"].ToString();
                            model.ContactNumber = reader["contact_number"].ToString();
                            model.DateOfBirth = reader["date_of_birth"].ToString();
                            model.Age = reader["age"].ToString();
                            model.Sport = reader["sport"].ToString();
                            model.Position = reader["position"].ToString();
                            model.ProfileImagePath = reader["profile_image"].ToString();
                        }
                    }
                }

                string insertSql = @"INSERT INTO coach_training_schedule 
              (users_id, title, time, location, notes, start_date, end_date) 
               OUTPUT INSERTED.id
               VALUES (@UserId, @Title, @Time, @Location, @Notes, @StartDate, @StartDate)";

                int newScheduleId;
                using (SqlCommand cmd = new SqlCommand(insertSql, conn))
                {
                    cmd.Parameters.AddWithValue("@UserId", Convert.ToInt32(model.userId));
                    cmd.Parameters.AddWithValue("@Title", model2.Title);
                    cmd.Parameters.AddWithValue("@Time", model2.Time);
                    cmd.Parameters.AddWithValue("@Location", model2.Location);
                    cmd.Parameters.AddWithValue("@Notes", model2.Notes);
                    cmd.Parameters.AddWithValue("@StartDate", model2.StartDate);

                    newScheduleId = Convert.ToInt32(cmd.ExecuteScalar()); // safe conversion
                }

                // Insert selected athletes linked to new schedule
                foreach (var athleteId in studentAthleteProfileId)
                {
                    string sql = @"INSERT INTO coach_training_schedule_selected_athletes 
                           (coach_training_schedule_id, student_id) 
                           VALUES (@ScheduleId, @AthleteId)";
                    using (SqlCommand cmd = new SqlCommand(sql, conn))
                    {
                        cmd.Parameters.AddWithValue("@ScheduleId", newScheduleId);
                        cmd.Parameters.AddWithValue("@AthleteId", athleteId);
                        cmd.ExecuteNonQuery();
                    }
                }


                var studentModel = new CoachAthleteApplicationModel();
                studentModel.userId = userId;

                string coachApprovedQuery = @"
        SELECT sap.id, u.firstname, u.lastname, sp.email, sp.profile_image, sp.contact_number, sp.date_of_birth, sp.age, sp.height, sp.weight,
        sp.course, sp.year_level, sp.emergency_contact, sp.sport, sp.status
        FROM student_athlete_profile sap
        INNER JOIN users u ON sap.users_id = u.id
        INNER JOIN student_profile sp ON sp.users_id = u.id
        WHERE sap.coach_id = @UserId AND sap.status = 1";

                using (SqlCommand roleCmd = new SqlCommand(coachApprovedQuery, conn))
                {
                    roleCmd.Parameters.AddWithValue("@UserId", Convert.ToInt32(userId));

                    using (SqlDataReader roleReader = roleCmd.ExecuteReader())
                    {
                        while (roleReader.Read())
                        {
                            studentModel.StudentListApproved.Add(new StudentAthleteProfileModel
                            {
                                studentAthleteProfileId = roleReader["id"].ToString(),
                                FullName = roleReader["firstname"].ToString() + " " + roleReader["lastname"].ToString(),
                                Email = roleReader["email"].ToString(),
                                Photo = roleReader["profile_image"].ToString(),
                                ContactNumber = roleReader["contact_number"].ToString(),
                                DateOfBirth = roleReader["date_of_birth"].ToString(),
                                Age = roleReader["age"].ToString(),
                                Height = roleReader["height"].ToString(),
                                Weight = roleReader["weight"].ToString(),
                                Course = roleReader["course"].ToString(),
                                YearLevel = roleReader["year_level"].ToString(),
                                EmergencyContact = roleReader["emergency_contact"].ToString(),
                                Sport = roleReader["sport"].ToString(),
                                Status = roleReader["status"].ToString(),
                            });
                        }
                    }
                }

                ViewBag.StudentAthleteApproved = studentModel;

                // after insert balik sa view
                return RedirectToAction("TrainingSchedule");
            }
        }


        [HttpGet]
        public IActionResult CalendarData()
        {
            string userId = HttpContext.Session.GetString("userId");
            var events = new List<StudentCalendarModel>();

            using (SqlConnection conn = new SqlConnection(_configuration.GetConnectionString("DefaultConnectionString")))
            {
                conn.Open();
                string query = @"SELECT id, title, start_date, end_date FROM coach_training_schedule WHERE users_id = @UserId";

                using (SqlCommand cmd = new SqlCommand(query, conn))
                {
                    cmd.Parameters.AddWithValue("@UserId", Convert.ToInt32(userId));
                    using (SqlDataReader reader = cmd.ExecuteReader())
                    {
                        while (reader.Read())
                        {
                            events.Add(new StudentCalendarModel
                            {
                                Title = reader["title"].ToString(),
                                StartDate = Convert.ToDateTime(reader["start_date"]).ToString("yyyy-MM-dd"),
                                EndDate = Convert.ToDateTime(reader["end_date"]).ToString("yyyy-MM-dd")
                            });
                        }
                    }
                }
            }

            // Map to FullCalendar format
            return Json(events.Select(e => new {
                title = e.Title,
                start = e.StartDate,
                end = e.EndDate
            }));
        }

        [HttpGet]
        public IActionResult AthleteApplication()
        {
            string userId = HttpContext.Session.GetString("userId");
            if (string.IsNullOrEmpty(userId))
                return RedirectToAction("Login", "Login");

            // Load coach profile
            var coachModel = new CoachProfileModel();
            coachModel.userId = userId;

            using (SqlConnection conn = new SqlConnection(_configuration.GetConnectionString("DefaultConnectionString")))
            {
                conn.Open();

                string query = @"SELECT users_id, profile_image, firstname, lastname, email, course, year_level, 
                        contact_number, date_of_birth, age, sport, position
                        FROM coach_profile
                        WHERE users_id = @UserId";

                using (SqlCommand cmd = new SqlCommand(query, conn))
                {
                    cmd.Parameters.AddWithValue("@UserId", Convert.ToInt32(userId));
                    using (SqlDataReader reader = cmd.ExecuteReader())
                    {
                        if (reader.Read())
                        {
                            coachModel.userId = reader["users_id"].ToString();
                            coachModel.FirstName = reader["firstname"].ToString();
                            coachModel.LastName = reader["lastname"].ToString();
                            coachModel.Email = reader["email"].ToString();
                            coachModel.Course = reader["course"].ToString();
                            coachModel.YearLevel = reader["year_level"].ToString();
                            coachModel.ContactNumber = reader["contact_number"].ToString();
                            coachModel.DateOfBirth = reader["date_of_birth"].ToString();
                            coachModel.Age = reader["age"].ToString();
                            coachModel.Sport = reader["sport"].ToString();
                            coachModel.Position = reader["position"].ToString();
                            coachModel.ProfileImagePath = reader["profile_image"].ToString();
                        }
                    }
                }
                // Load student athlete dropdown for this coach
                var studentModel = new CoachAthleteApplicationModel();
                studentModel.userId = userId;


                string coachPendingQuery = @"
                SELECT sap.id, u.firstname, u.lastname, sp.email, sp.profile_image, sp.contact_number, sp.date_of_birth, sp.age, sp.height, sp.weight, 
                sp.course, sp.year_level, sp.emergency_contact, sp.sport, sp.status, sap.users_id
                FROM student_athlete_profile sap
                INNER JOIN users u ON sap.users_id = u.id
                INNER JOIN student_profile sp ON sp.users_id = u.id
                WHERE sap.coach_id = @UserId AND sap.status IS NULL";

                using (SqlCommand roleCmd = new SqlCommand(coachPendingQuery, conn))
                {
                    roleCmd.Parameters.AddWithValue("@UserId", Convert.ToInt32(userId));

                    using (SqlDataReader roleReader = roleCmd.ExecuteReader())
                    {
                        while (roleReader.Read())
                        {
                            studentModel.StudentListPending.Add(new StudentAthleteProfileModel
                            {
                                studentAthleteProfileId = roleReader["id"].ToString(),
                                userId = roleReader["users_id"].ToString(),
                                FullName = roleReader["firstname"].ToString() + " " + roleReader["lastname"].ToString(),
                                Email = roleReader["email"].ToString(),
                                Photo = roleReader["profile_image"].ToString(),
                                ContactNumber = roleReader["contact_number"].ToString(),
                                DateOfBirth = roleReader["date_of_birth"].ToString(),
                                Age = roleReader["age"].ToString(),
                                Height = roleReader["height"].ToString(),
                                Weight = roleReader["weight"].ToString(),
                                Course = roleReader["course"].ToString(),
                                YearLevel = roleReader["year_level"].ToString(),
                                EmergencyContact = roleReader["emergency_contact"].ToString(),
                                Sport = roleReader["sport"].ToString(),
                                Status = roleReader["status"].ToString(),
                            });
                        }
                    }
                }

                string coachApprovedQuery = @"
                SELECT sap.id, u.firstname, u.lastname, sp.email, sp.profile_image, sp.contact_number, sp.date_of_birth, sp.age, sp.height, sp.weight,
                sp.course, sp.year_level, sp.emergency_contact, sp.sport, sp.status
                FROM student_athlete_profile sap
                INNER JOIN users u ON sap.users_id = u.id
                INNER JOIN student_profile sp ON sp.users_id = u.id
                WHERE sap.coach_id = @UserId AND sap.status = 1";

                using (SqlCommand roleCmd = new SqlCommand(coachApprovedQuery, conn))
                {
                    roleCmd.Parameters.AddWithValue("@UserId", Convert.ToInt32(userId));

                    using (SqlDataReader roleReader = roleCmd.ExecuteReader())
                    {
                        while (roleReader.Read())
                        {
                            studentModel.StudentListApproved.Add(new StudentAthleteProfileModel
                            {
                                studentAthleteProfileId = roleReader["id"].ToString(),
                                FullName = roleReader["firstname"].ToString() + " " + roleReader["lastname"].ToString(),
                                Email = roleReader["email"].ToString(),
                                Photo = roleReader["profile_image"].ToString(),
                                ContactNumber = roleReader["contact_number"].ToString(),
                                DateOfBirth = roleReader["date_of_birth"].ToString(),
                                Age = roleReader["age"].ToString(),
                                Height = roleReader["height"].ToString(),
                                Weight = roleReader["weight"].ToString(),
                                Course = roleReader["course"].ToString(),
                                YearLevel = roleReader["year_level"].ToString(),
                                EmergencyContact = roleReader["emergency_contact"].ToString(),
                                Sport = roleReader["sport"].ToString(),
                                Status = roleReader["status"].ToString(),
                            });
                        }
                    }
                }

                // Count total unprocessed student applications
                string countPendingQuery = "SELECT COUNT(*) FROM student_athlete_profile WHERE coach_id = @UserId AND status IS NULL";
                using (SqlCommand countCmd = new SqlCommand(countPendingQuery, conn))
                {
                    countCmd.Parameters.AddWithValue("@UserId", Convert.ToInt32(userId));
                    int totalPendingApplications = (int)countCmd.ExecuteScalar();
                    ViewBag.totalPendingApplications = totalPendingApplications;
                }

                string countApprovedQuery = "SELECT COUNT(*) FROM student_athlete_profile WHERE coach_id = @UserId AND status = 1";
                using (SqlCommand countCmd = new SqlCommand(countApprovedQuery, conn))
                {
                    countCmd.Parameters.AddWithValue("@UserId", Convert.ToInt32(userId));
                    int totalApprovedApplications = (int)countCmd.ExecuteScalar();
                    ViewBag.totalApprovedApplications = totalApprovedApplications;
                }

                string countRejectQuery = "SELECT COUNT(*) FROM student_athlete_profile WHERE coach_id = @UserId AND status = 2";
                using (SqlCommand countCmd = new SqlCommand(countRejectQuery, conn))
                {
                    countCmd.Parameters.AddWithValue("@UserId", Convert.ToInt32(userId));
                    int totalRejectApplications = (int)countCmd.ExecuteScalar();
                    ViewBag.totalRejectApplications = totalRejectApplications;
                }



                ViewBag.StudentAthletePending = studentModel;
                ViewBag.StudentAthleteApproved = studentModel;

            }

            return View();
        }


        [HttpPost]
        public IActionResult Approved(CoachAthleteApplicationModel model)
        {
            string userId = HttpContext.Session.GetString("userId");
            if (string.IsNullOrEmpty(userId))
                return RedirectToAction("Login", "Login");

            using (SqlConnection conn = new SqlConnection(_configuration.GetConnectionString("DefaultConnectionString")))
            {
                conn.Open();
                string query = @"UPDATE student_athlete_profile 
                         SET status=@Status 
                         WHERE id=@studentAthleteProfileId";

                using (SqlCommand cmd = new SqlCommand(query, conn))
                {
                    cmd.Parameters.AddWithValue("@Status", "1");
                    cmd.Parameters.AddWithValue("@studentAthleteProfileId", Convert.ToInt32(model.studentAthleteProfileId));
                    cmd.ExecuteNonQuery();
                }
            }

            TempData["Message"] = "Status updated successfully!";

            // ✅ ibalik sa AthleteApplication (singular)
            return RedirectToAction("AthleteApplication", "Coach");
        }


        [HttpPost]
        public IActionResult Reject(CoachAthleteApplicationModel model)
        {
            string userId = HttpContext.Session.GetString("userId");
            if (string.IsNullOrEmpty(userId))
                return RedirectToAction("Login", "Login");

            using (SqlConnection conn = new SqlConnection(_configuration.GetConnectionString("DefaultConnectionString")))
            {
                conn.Open();

                // 1️⃣ Update student status
                string updateQuery = @"UPDATE student_athlete_profile 
                               SET status=@Status 
                               WHERE id=@studentAthleteProfileId";

                using (SqlCommand cmd = new SqlCommand(updateQuery, conn))
                {
                    cmd.Parameters.AddWithValue("@Status", "2"); // 2 = rejected
                    cmd.Parameters.AddWithValue("@studentAthleteProfileId", model.studentAthleteProfileId);
                    cmd.ExecuteNonQuery();
                }

                // 2️⃣ Insert notification
                string insertNotification = @"INSERT INTO notifications (coach_id, student_id, description, timestamp) 
                                      VALUES (@CoachId, @StudentId, @Description, @Timestamp)";

                using (SqlCommand cmd = new SqlCommand(insertNotification, conn))
                {
                    cmd.Parameters.AddWithValue("@CoachId", userId);                 // from session
                    cmd.Parameters.AddWithValue("@StudentId", model.studentId);     // from modal input
                    cmd.Parameters.AddWithValue("@Description", model.notifDescription); // message text
                    cmd.Parameters.AddWithValue("@Timestamp", DateTime.Now);          // current date & time

                    cmd.ExecuteNonQuery();
                }
            }

            TempData["Message"] = "Status updated and notification sent successfully!";
            return RedirectToAction("AthleteApplication", "Coach");
        }




        public IActionResult AcademicMonitoring()
        {

            string userId = HttpContext.Session.GetString("userId");
            if (string.IsNullOrEmpty(userId))
                return RedirectToAction("Login", "Login");

            StudentProfileModel model = new StudentProfileModel();
            model.userId = userId;

            using (SqlConnection conn = new SqlConnection(_configuration.GetConnectionString("DefaultConnectionString")))
            {
                conn.Open();
                string query = @"SELECT users_id, profile_image, firstname, lastname, email, course, year_level, 
                                contact_number, emergency_contact, date_of_birth, age, sport
                         FROM student_profile
                         WHERE users_id = @UserId";

                using (SqlCommand cmd = new SqlCommand(query, conn))
                {
                    cmd.Parameters.AddWithValue("@UserId", Convert.ToInt32(userId));
                    using (SqlDataReader reader = cmd.ExecuteReader())
                    {

                        if (reader.Read())
                        {
                            model.userId = reader["users_id"].ToString();
                            model.FirstName = reader["firstname"].ToString();
                            model.LastName = reader["lastname"].ToString();
                            model.Email = reader["email"].ToString();
                            model.Course = reader["course"].ToString();
                            model.YearLevel = reader["year_level"].ToString();
                            model.ContactNumber = reader["contact_number"].ToString();
                            model.EmergencyContact = reader["emergency_contact"].ToString();
                            model.DateOfBirth = reader["date_of_birth"].ToString();
                            model.Age = reader["age"].ToString();
                            model.Sport = reader["sport"].ToString();
                            model.ProfileImagePath = reader["profile_image"].ToString();


                        }
                    }
                }
            }

            var grades = new List<StudentAcademicMonitoringModel>();

            double total = 0;
            int count = 0;


            using (SqlConnection conn = new SqlConnection(_configuration.GetConnectionString("DefaultConnectionString")))
            {
                conn.Open();
                string query = "SELECT id, users_id, subject, grade FROM student_grade WHERE users_id=@UserId";

                using (SqlCommand cmd = new SqlCommand(query, conn))
                {
                    cmd.Parameters.AddWithValue("@UserId", userId);
                    using (SqlDataReader reader = cmd.ExecuteReader())
                    {
                        while (reader.Read())
                        {
                            double grade = Convert.ToDouble(reader["grade"]);
                            total += grade;
                            count++;
                            grades.Add(new StudentAcademicMonitoringModel
                            {
                                Subject = reader["subject"].ToString(),
                                Grade = grade.ToString()
                            });
                        }
                    }
                }
            }

            ViewBag.Grades = grades;
            ViewBag.GWA = count > 0 ? (total / count) : 0; // compute GWA




            return View(model);
        }

        [HttpPost]
        public IActionResult AcademicMonitoringGrade(int userId, List<string> Subject, List<string> Grade)
        {
            string connStr = _configuration.GetConnectionString("DefaultConnectionString");

            using (SqlConnection conn = new SqlConnection(connStr))
            {
                conn.Open();

                for (int i = 0; i < Subject.Count; i++)
                {
                    string sql = "INSERT INTO student_grade (users_id, subject, grade) VALUES (@userId, @subject, @grade)";

                    using (SqlCommand cmd = new SqlCommand(sql, conn))
                    {
                        cmd.Parameters.AddWithValue("@userId", userId);
                        cmd.Parameters.AddWithValue("@subject", Subject[i]);
                        cmd.Parameters.AddWithValue("@grade", Grade[i]);
                        cmd.ExecuteNonQuery();
                    }
                }
            }

            // after insert balik sa view
            return RedirectToAction("AcademicMonitoring");
        }



        [HttpPost]
        public IActionResult AcademicMonitoringAcademicRegistration(StudentAcademicMonitoringModel model, IFormFile File)
        {
            string userId = HttpContext.Session.GetString("userId");
            if (string.IsNullOrEmpty(userId))
                return RedirectToAction("Login", "Login");

            // --- Handle file upload ---
            if (File != null && File.Length > 0)
            {
                var safeFileName = Path.GetFileNameWithoutExtension(File.FileName);
                safeFileName = string.Join("_", safeFileName.Split(Path.GetInvalidFileNameChars()));
                var extension = Path.GetExtension(File.FileName);
                var fileName = $"{userId}_{safeFileName}{extension}";

                var imagesFolder = Path.Combine(_env.WebRootPath, "files");
                if (!Directory.Exists(imagesFolder))
                    Directory.CreateDirectory(imagesFolder);

                var filePath = Path.Combine(imagesFolder, fileName);
                using (var stream = new FileStream(filePath, FileMode.Create))
                    File.CopyTo(stream);

                model.File = "/files/" + fileName;
            }

            // --- Save to database ---
            using (SqlConnection conn = new SqlConnection(_configuration.GetConnectionString("DefaultConnectionString")))
            {
                conn.Open();
                string query = @"INSERT INTO student_academic_registration (users_id, files) 
                         VALUES (@UserId, @File)";

                using (SqlCommand cmd = new SqlCommand(query, conn))
                {
                    cmd.Parameters.AddWithValue("@UserId", Convert.ToInt32(model.userId));
                    cmd.Parameters.AddWithValue("@File", model.File ?? "");
                    cmd.ExecuteNonQuery();
                }
            }

            ViewBag.Message = "Academic Registration created successfully!";
            return RedirectToAction("AcademicMonitoring");
        }




        public IActionResult AthleteProfile()
        {

            string userId = HttpContext.Session.GetString("userId");
            if (string.IsNullOrEmpty(userId))
                return RedirectToAction("Login", "Login");

            StudentProfileModel model = new StudentProfileModel();
            model.userId = userId;

            using (SqlConnection conn = new SqlConnection(_configuration.GetConnectionString("DefaultConnectionString")))
            {
                conn.Open();
                string query = @"SELECT users_id, profile_image, firstname, lastname, email, course, year_level, 
                                contact_number, emergency_contact, date_of_birth, age, sport
                         FROM student_profile
                         WHERE users_id = @UserId";

                using (SqlCommand cmd = new SqlCommand(query, conn))
                {
                    cmd.Parameters.AddWithValue("@UserId", Convert.ToInt32(userId));
                    using (SqlDataReader reader = cmd.ExecuteReader())
                    {

                        if (reader.Read())
                        {
                            model.userId = reader["users_id"].ToString();
                            model.FirstName = reader["firstname"].ToString();
                            model.LastName = reader["lastname"].ToString();
                            model.Email = reader["email"].ToString();
                            model.Course = reader["course"].ToString();
                            model.YearLevel = reader["year_level"].ToString();
                            model.ContactNumber = reader["contact_number"].ToString();
                            model.EmergencyContact = reader["emergency_contact"].ToString();
                            model.DateOfBirth = reader["date_of_birth"].ToString();
                            model.Age = reader["age"].ToString();
                            model.Sport = reader["sport"].ToString();
                            model.ProfileImagePath = reader["profile_image"].ToString();


                        }
                    }
                }
            }

            return View(model);
        }


        [HttpPost]
        public IActionResult AthleteProfile(StudentAthleteProfileModel model, IFormFile Photo, IFormFile Portfolio, List<string> Name, List<string> Level, List<string> Year, List<string> Award)
        {
            string connStr = _configuration.GetConnectionString("DefaultConnectionString");


            if (Photo != null && Photo.Length > 0)
            {
                var safeFileName = Path.GetFileNameWithoutExtension(Photo.FileName);
                safeFileName = string.Join("_", safeFileName.Split(Path.GetInvalidFileNameChars()));
                var extension = Path.GetExtension(Photo.FileName);
                var fileName = $"{safeFileName}{extension}";

                var imagesFolder = Path.Combine(_env.WebRootPath, "images");
                if (!Directory.Exists(imagesFolder))
                    Directory.CreateDirectory(imagesFolder);

                var filePath = Path.Combine(imagesFolder, fileName);
                using (var stream = new FileStream(filePath, FileMode.Create))
                    Photo.CopyTo(stream);

                model.Photo = "/images/" + fileName;
            }

            if (Portfolio != null && Portfolio.Length > 0)
            {
                var safeFileName = Path.GetFileNameWithoutExtension(Portfolio.FileName);
                safeFileName = string.Join("_", safeFileName.Split(Path.GetInvalidFileNameChars()));
                var extension = Path.GetExtension(Portfolio.FileName);
                var fileName = $"{safeFileName}{extension}";

                var imagesFolder = Path.Combine(_env.WebRootPath, "files");
                if (!Directory.Exists(imagesFolder))
                    Directory.CreateDirectory(imagesFolder);

                var filePath = Path.Combine(imagesFolder, fileName);
                using (var stream = new FileStream(filePath, FileMode.Create))
                    Portfolio.CopyTo(stream);

                model.Portfolio = "/files/" + fileName;
            }

            using (SqlConnection conn = new SqlConnection(connStr))
            {
                conn.Open();

                // 1. Insert sa student_athlete_profile at kunin yung ID
                string sqlProfile = "INSERT INTO student_athlete_profile (photo,course_one,course_two,course_three,elementary_school,elementary_year_graduated,secondary_school,secondary_year_graduated,senior_high_school,senior_high_year_graduated,shs_track_or_strand,g_ten_gwa,g_eleven_gwa,g_twelve_gwa,transfer_status,prev_program_or_course,vaccination,philhealth_number,event,home_address,provincial_address,portfolio) VALUES (@Photo,@CourseOne,@CourseTwo,@CourseThree,@ElementarySchool,@ElementaryYearGraduated,@SecondarySchool,@SecondaryYearGraduated,@SeniorHighSchool,@SeniorHighYearGraduated,@ShsTrackOrStrand,@GTenGwa,@GElevenGwa,@GTwelveGwa,@TransferStatus,@PrevProgramOrCourse,@Vaccination,@PhilhealthNumber,@Event,@HomeAddress,@ProvincialAddress,@Portfolio); SELECT SCOPE_IDENTITY();";
                int studentAthleteProfileId;
                using (SqlCommand cmd = new SqlCommand(sqlProfile, conn))
                {
                    cmd.Parameters.AddWithValue("@Photo", model.Photo ?? "");
                    cmd.Parameters.AddWithValue("@CourseOne", model.CourseOne ?? "");
                    cmd.Parameters.AddWithValue("@CourseTwo", model.CourseTwo ?? "");
                    cmd.Parameters.AddWithValue("@CourseThree", model.CourseThree ?? "");
                    cmd.Parameters.AddWithValue("@ElementarySchool", model.ElementarySchool ?? "");
                    cmd.Parameters.AddWithValue("@ElementaryYearGraduated", model.ElementaryYearGraduated ?? "");
                    cmd.Parameters.AddWithValue("@SecondarySchool", model.SecondarySchool ?? "");
                    cmd.Parameters.AddWithValue("@SecondaryYearGraduated", model.SecondaryYearGraduated ?? "");
                    cmd.Parameters.AddWithValue("@SeniorHighSchool", model.SeniorHighSchool ?? "");
                    cmd.Parameters.AddWithValue("@SeniorHighYearGraduated", model.SeniorHighYearGraduated ?? "");
                    cmd.Parameters.AddWithValue("@ShsTrackOrStrand", model.ShsTrackOrStrand ?? "");
                    cmd.Parameters.AddWithValue("@GTenGwa", model.GTenGwa ?? "");
                    cmd.Parameters.AddWithValue("@GElevenGwa", model.GElevenGwa ?? "");
                    cmd.Parameters.AddWithValue("@GTwelveGwa", model.GTwelveGwa ?? "");
                    cmd.Parameters.AddWithValue("@TransferStatus", model.TransferStatus ?? "");
                    cmd.Parameters.AddWithValue("@PrevProgramOrCourse", model.PrevProgramOrCourse ?? "");
                    cmd.Parameters.AddWithValue("@Vaccination", model.Vaccination ?? "");
                    cmd.Parameters.AddWithValue("@PhilhealthNumber", model.PhilhealthNumber ?? "");
                    cmd.Parameters.AddWithValue("@Event", model.Event ?? "");
                    cmd.Parameters.AddWithValue("@HomeAddress", model.HomeAddress ?? "");
                    cmd.Parameters.AddWithValue("@ProvincialAddress", model.ProvincialAddress ?? "");
                    cmd.Parameters.AddWithValue("@Portfolio", model.Portfolio ?? "");
                    studentAthleteProfileId = Convert.ToInt32(cmd.ExecuteScalar()); // <-- dito nakukuha yung last inserted ID
                }

                // 2. Insert sa student_sports_participation gamit yung last inserted ID
                for (int i = 0; i < Name.Count; i++)
                {
                    string sqlParticipation = "INSERT INTO student_sports_participation (student_athlete_profile_id, name, level, year, award) VALUES (@studentAthleteProfileId, @Name, @Level, @Year, @Award)";

                    using (SqlCommand cmd = new SqlCommand(sqlParticipation, conn))
                    {
                        cmd.Parameters.AddWithValue("@studentAthleteProfileId", studentAthleteProfileId);
                        cmd.Parameters.AddWithValue("@Name", Name[i]);
                        cmd.Parameters.AddWithValue("@Level", Level[i]);
                        cmd.Parameters.AddWithValue("@Year", Year[i]);
                        cmd.Parameters.AddWithValue("@Award", Award[i]);
                        cmd.ExecuteNonQuery();
                    }
                }
            }

            return RedirectToAction("AthleteProfile");
        }


        [HttpGet]
        public IActionResult AthleteInformation()
        {
            string userId = HttpContext.Session.GetString("userId");
            if (string.IsNullOrEmpty(userId))
                return RedirectToAction("Login", "Login");

            // Load coach profile
            var coachModel = new CoachProfileModel();
            coachModel.userId = userId;

            using (SqlConnection conn = new SqlConnection(_configuration.GetConnectionString("DefaultConnectionString")))
            {
                conn.Open();

                string query = @"SELECT users_id, profile_image, firstname, lastname, email, course, year_level, 
                        contact_number, date_of_birth, age, sport, position
                        FROM coach_profile
                        WHERE users_id = @UserId";

                using (SqlCommand cmd = new SqlCommand(query, conn))
                {
                    cmd.Parameters.AddWithValue("@UserId", Convert.ToInt32(userId));
                    using (SqlDataReader reader = cmd.ExecuteReader())
                    {
                        if (reader.Read())
                        {
                            coachModel.userId = reader["users_id"].ToString();
                            coachModel.FirstName = reader["firstname"].ToString();
                            coachModel.LastName = reader["lastname"].ToString();
                            coachModel.Email = reader["email"].ToString();
                            coachModel.Course = reader["course"].ToString();
                            coachModel.YearLevel = reader["year_level"].ToString();
                            coachModel.ContactNumber = reader["contact_number"].ToString();
                            coachModel.DateOfBirth = reader["date_of_birth"].ToString();
                            coachModel.Age = reader["age"].ToString();
                            coachModel.Sport = reader["sport"].ToString();
                            coachModel.Position = reader["position"].ToString();
                            coachModel.ProfileImagePath = reader["profile_image"].ToString();
                        }
                    }
                }
                // Load student athlete dropdown for this coach
                var studentModel = new CoachAthleteApplicationModel();
                studentModel.userId = userId;

                string studentProfileAndRecords = @"
                SELECT sap.id, u.firstname, u.lastname, sp.email, sp.profile_image, sp.contact_number, sp.date_of_birth, sp.age, sp.height, sp.weight,
                sp.course, sp.year_level, sp.emergency_contact, sp.sport, sp.status, sp.users_id
                FROM student_athlete_profile sap
                INNER JOIN users u ON sap.users_id = u.id
                INNER JOIN student_profile sp ON sp.users_id = u.id
                WHERE sap.coach_id = @UserId";

                using (SqlCommand roleCmd = new SqlCommand(studentProfileAndRecords, conn))
                {
                    roleCmd.Parameters.AddWithValue("@UserId", Convert.ToInt32(userId));

                    using (SqlDataReader roleReader = roleCmd.ExecuteReader())
                    {
                        while (roleReader.Read())
                        {
                            studentModel.StudentListApproved.Add(new StudentAthleteProfileModel
                            {
                                userId = roleReader["users_id"].ToString(),
                                studentAthleteProfileId = roleReader["id"].ToString(),
                                FullName = roleReader["firstname"].ToString() + " " + roleReader["lastname"].ToString(),
                                Email = roleReader["email"].ToString(),
                                Photo = roleReader["profile_image"].ToString(),
                                ContactNumber = roleReader["contact_number"].ToString(),
                                DateOfBirth = roleReader["date_of_birth"].ToString(),
                                Age = roleReader["age"].ToString(),
                                Height = roleReader["height"].ToString(),
                                Weight = roleReader["weight"].ToString(),
                                Course = roleReader["course"].ToString(),
                                YearLevel = roleReader["year_level"].ToString(),
                                EmergencyContact = roleReader["emergency_contact"].ToString(),
                                Sport = roleReader["sport"].ToString(),
                                Status = roleReader["status"].ToString(),
                            });
                        }
                    }
                }

                // Count total unprocessed student applications
                string countPendingQuery = "SELECT COUNT(*) FROM student_athlete_profile WHERE coach_id = @UserId AND status IS NULL";
                using (SqlCommand countCmd = new SqlCommand(countPendingQuery, conn))
                {
                    countCmd.Parameters.AddWithValue("@UserId", Convert.ToInt32(userId));
                    int totalPendingApplications = (int)countCmd.ExecuteScalar();
                    ViewBag.totalPendingApplications = totalPendingApplications;
                }

                string countApprovedQuery = "SELECT COUNT(*) FROM student_athlete_profile WHERE coach_id = @UserId AND status = 1";
                using (SqlCommand countCmd = new SqlCommand(countApprovedQuery, conn))
                {
                    countCmd.Parameters.AddWithValue("@UserId", Convert.ToInt32(userId));
                    int totalApprovedApplications = (int)countCmd.ExecuteScalar();
                    ViewBag.totalApprovedApplications = totalApprovedApplications;
                }

                string countRejectQuery = "SELECT COUNT(*) FROM student_athlete_profile WHERE coach_id = @UserId AND status = 2";
                using (SqlCommand countCmd = new SqlCommand(countRejectQuery, conn))
                {
                    countCmd.Parameters.AddWithValue("@UserId", Convert.ToInt32(userId));
                    int totalRejectApplications = (int)countCmd.ExecuteScalar();
                    ViewBag.totalRejectApplications = totalRejectApplications;
                }



                ViewBag.StudentAthleteProfileAndRecords = studentModel;

            }

            return View();
        }





        [HttpGet("Coach/ViewProfileAthlete/{id}")]
        public IActionResult ViewProfileAthlete(int id)
        {
            string userId = HttpContext.Session.GetString("userId");
            if (string.IsNullOrEmpty(userId))
                return RedirectToAction("Login", "Login");

            // Load coach profile
            var coachModel = new CoachProfileModel();
            coachModel.userId = userId;

            using (SqlConnection conn = new SqlConnection(_configuration.GetConnectionString("DefaultConnectionString")))
            {
                conn.Open();

                string query = @"SELECT users_id, profile_image, firstname, lastname, email, course, year_level, 
                        contact_number, date_of_birth, age, sport, position 
                        FROM coach_profile WHERE users_id = @UserId";

                using (SqlCommand cmd = new SqlCommand(query, conn))
                {
                    cmd.Parameters.AddWithValue("@UserId", Convert.ToInt32(userId));
                    using (SqlDataReader reader = cmd.ExecuteReader())
                    {
                        if (reader.Read())
                        {
                            coachModel.userId = reader["users_id"].ToString();
                            coachModel.FirstName = reader["firstname"].ToString();
                            coachModel.LastName = reader["lastname"].ToString();
                            coachModel.Email = reader["email"].ToString();
                            coachModel.Course = reader["course"].ToString();
                            coachModel.YearLevel = reader["year_level"].ToString();
                            coachModel.ContactNumber = reader["contact_number"].ToString();
                            coachModel.DateOfBirth = reader["date_of_birth"].ToString();
                            coachModel.Age = reader["age"].ToString();
                            coachModel.Sport = reader["sport"].ToString();
                            coachModel.Position = reader["position"].ToString();
                            coachModel.ProfileImagePath = reader["profile_image"].ToString();
                        }
                    }
                }

                // Load specific student athlete by ID
                var studentModel = new CoachAthleteApplicationModel();
                studentModel.userId = userId;

                string studentProfileQuery = @"
            SELECT sap.id, u.firstname, u.lastname, sp.email, sp.profile_image, sp.contact_number, 
                   sp.date_of_birth, sp.age, sp.height, sp.weight, sp.course, sp.year_level, 
                   sp.emergency_contact, sp.sport, sp.status, sp.users_id, sap.event, sap.home_address, 
                   sap.provincial_address, sap.vaccination, sap.philhealth_number,
                   sap.elementary_school, sap.elementary_year_graduated, sap.secondary_school, 
                   sap.secondary_year_graduated, sap.senior_high_school, sap.senior_high_year_graduated,
                   sap.shs_track_or_strand, sap.g_ten_gwa, sap.g_eleven_gwa, sap.g_twelve_gwa,
                   sap.transfer_status, sap.prev_program_or_course,
                   sap.course_one, sap.course_two, sap.course_three,
                   sp.profile_image, sap.portfolio
            FROM student_athlete_profile sap 
            INNER JOIN users u ON sap.users_id = u.id 
            INNER JOIN student_profile sp ON sp.users_id = u.id 
            WHERE sp.users_id = @StudentId";

                StudentAthleteProfileModel student = null;

                using (SqlCommand cmd = new SqlCommand(studentProfileQuery, conn))
                {
                    cmd.Parameters.AddWithValue("@StudentId", id);
                    using (SqlDataReader reader = cmd.ExecuteReader())
                    {
                        if (reader.Read())
                        {
                            student = new StudentAthleteProfileModel
                            {
                                userId = reader["users_id"].ToString(),
                                studentAthleteProfileId = reader["id"].ToString(),
                                FullName = reader["firstname"].ToString() + " " + reader["lastname"].ToString(),
                                Email = reader["email"].ToString(),
                                Photo = reader["profile_image"].ToString(),
                                ContactNumber = reader["contact_number"].ToString(),
                                DateOfBirth = reader["date_of_birth"].ToString(),
                                Age = reader["age"].ToString(),
                                Height = reader["height"].ToString(),
                                Weight = reader["weight"].ToString(),
                                Course = reader["course"].ToString(),
                                YearLevel = reader["year_level"].ToString(),
                                EmergencyContact = reader["emergency_contact"].ToString(),
                                Sport = reader["sport"].ToString(),
                                Status = reader["status"].ToString(),
                                HomeAddress = reader["home_address"].ToString(),
                                ProvincialAddress = reader["provincial_address"].ToString(),
                                Vaccination = reader["vaccination"].ToString(),
                                PhilhealthNumber = reader["philhealth_number"].ToString(),
                                Event = reader["event"].ToString(),
                                ElementarySchool = reader["elementary_school"].ToString(),
                                ElementaryYearGraduated = reader["elementary_year_graduated"].ToString(),
                                SecondarySchool = reader["secondary_school"].ToString(),
                                SecondaryYearGraduated = reader["secondary_year_graduated"].ToString(),
                                SeniorHighSchool = reader["senior_high_school"].ToString(),
                                SeniorHighYearGraduated = reader["senior_high_year_graduated"].ToString(),
                                ShsTrackOrStrand = reader["shs_track_or_strand"].ToString(),
                                GTenGwa = reader["g_ten_gwa"].ToString(),
                                GElevenGwa = reader["g_eleven_gwa"].ToString(),
                                GTwelveGwa = reader["g_twelve_gwa"].ToString(),
                                TransferStatus = reader["transfer_status"].ToString(),
                                PrevProgramOrCourse = reader["prev_program_or_course"].ToString(),
                                ProfileImagePath = reader["profile_image"].ToString(),
                                CourseOne = reader["course_one"].ToString(),
                                CourseTwo = reader["course_two"].ToString(),
                                CourseThree = reader["course_three"].ToString(),
                                Portfolio = reader["portfolio"].ToString(),

                            };
                        }
                    }
                }

                if (student != null)
                {
                    // Load sports participations for this student
                    string participationQuery = @"
                SELECT name, level, year, award 
                FROM student_sports_participation 
                WHERE student_athlete_profile_id = @StudentAthleteProfileId";

                    using (SqlCommand cmd = new SqlCommand(participationQuery, conn))
                    {
                        cmd.Parameters.AddWithValue("@StudentAthleteProfileId", Convert.ToInt32(student.studentAthleteProfileId));
                        using (SqlDataReader reader = cmd.ExecuteReader())
                        {
                            while (reader.Read())
                            {
                                student.Participations.Add(new StudentSportsParticipationModel
                                {
                                    Name = reader["name"].ToString(),
                                    Level = reader["level"].ToString(),
                                    Year = reader["year"].ToString(),
                                    Award = reader["award"].ToString()
                                });
                            }
                        }
                    }

                    // Create the list that the view expects
                    studentModel.StudentListApproved = new List<StudentAthleteProfileModel> { student };
                }
                else
                {
                    // Student not found, return to previous page or show error
                    TempData["Error"] = "Student not found.";
                    return RedirectToAction("ManageAthletes", "Coach");
                }

                // Count statistics (keep existing code)
                string countPendingQuery = "SELECT COUNT(*) FROM student_athlete_profile WHERE coach_id = @UserId AND status IS NULL";
                using (SqlCommand countCmd = new SqlCommand(countPendingQuery, conn))
                {
                    countCmd.Parameters.AddWithValue("@UserId", Convert.ToInt32(userId));
                    int totalPendingApplications = (int)countCmd.ExecuteScalar();
                    ViewBag.totalPendingApplications = totalPendingApplications;
                }

                string countApprovedQuery = "SELECT COUNT(*) FROM student_athlete_profile WHERE coach_id = @UserId AND status = 1";
                using (SqlCommand countCmd = new SqlCommand(countApprovedQuery, conn))
                {
                    countCmd.Parameters.AddWithValue("@UserId", Convert.ToInt32(userId));
                    int totalApprovedApplications = (int)countCmd.ExecuteScalar();
                    ViewBag.totalApprovedApplications = totalApprovedApplications;
                }

                string countRejectQuery = "SELECT COUNT(*) FROM student_athlete_profile WHERE coach_id = @UserId AND status = 2";
                using (SqlCommand countCmd = new SqlCommand(countRejectQuery, conn))
                {
                    countCmd.Parameters.AddWithValue("@UserId", Convert.ToInt32(userId));
                    int totalRejectApplications = (int)countCmd.ExecuteScalar();
                    ViewBag.totalRejectApplications = totalRejectApplications;
                }

                ViewBag.StudentAthleteProfileAndRecords = studentModel;
            }

            return View();
        }








    }
}
