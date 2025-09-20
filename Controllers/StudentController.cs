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
using System.Reflection;

namespace FITNSS.Controllers
{
    public class StudentController : Controller
    {
        private readonly IConfiguration _configuration;

        private readonly IWebHostEnvironment _env;
        public StudentController(IConfiguration configuration,
                                 ILogger<StudentController> logger,
                                 IWebHostEnvironment env)
        {
            _configuration = configuration;
            _env = env;
        }
        public IActionResult Dashboard()
        {


            // Sample data – ideally galing sa database
            var days = new[] { "Mon", "Tue", "Wed", "Thu", "Fri", "Sat", "Sun" };


            string userId = HttpContext.Session.GetString("userId");
            if (string.IsNullOrEmpty(userId))
                return RedirectToAction("Login", "Login");


            DateTime today = DateTime.Today;
            // Hanapin start at end of week (Monday - Sunday)
            int diff = (7 + (today.DayOfWeek - DayOfWeek.Monday)) % 7;
            DateTime weekStart = today.AddDays(-1 * diff).Date;
            DateTime weekEnd = weekStart.AddDays(6);

            var daysOfWeek = new[] { "Mon", "Tue", "Wed", "Thu", "Fri", "Sat", "Sun" };

            var heartbeats = new List<int>();
            var steps = new List<int>();
            var calories = new List<int>();

            using (SqlConnection conn = new SqlConnection(_configuration.GetConnectionString("DefaultConnectionString")))
            {
                conn.Open();

                // HEARTBEATS
                string queryHeartbeat = @"
            SELECT DATENAME(WEEKDAY, date) AS DayName, SUM(heartbeat) AS Total
            FROM student_heartbeat
            WHERE users_id = @UserId
              AND date BETWEEN @WeekStart AND @WeekEnd
            GROUP BY DATENAME(WEEKDAY, date)";
                using (SqlCommand cmd = new SqlCommand(queryHeartbeat, conn))
                {
                    cmd.Parameters.AddWithValue("@UserId", userId);
                    cmd.Parameters.AddWithValue("@WeekStart", weekStart);
                    cmd.Parameters.AddWithValue("@WeekEnd", weekEnd);

                    var dict = daysOfWeek.ToDictionary(d => d, d => 0);

                    using (SqlDataReader reader = cmd.ExecuteReader())
                    {
                        while (reader.Read())
                        {
                            string day = reader["DayName"].ToString().Substring(0, 3); // Mon, Tue, ...
                            int total = Convert.ToInt32(reader["Total"]);
                            dict[day] = total;
                        }
                    }
                    heartbeats = dict.Values.ToList();
                }

                // RUNNING
                string querySteps = @"
            SELECT DATENAME(WEEKDAY, date) AS DayName, SUM(steps) AS Total
            FROM student_steps
            WHERE users_id = @UserId
              AND date BETWEEN @WeekStart AND @WeekEnd
            GROUP BY DATENAME(WEEKDAY, date)";
                using (SqlCommand cmd = new SqlCommand(querySteps, conn))
                {
                    cmd.Parameters.AddWithValue("@UserId", userId);
                    cmd.Parameters.AddWithValue("@WeekStart", weekStart);
                    cmd.Parameters.AddWithValue("@WeekEnd", weekEnd);

                    var dict = daysOfWeek.ToDictionary(d => d, d => 0);

                    using (SqlDataReader reader = cmd.ExecuteReader())
                    {
                        while (reader.Read())
                        {
                            string day = reader["DayName"].ToString().Substring(0, 3);
                            int total = Convert.ToInt32(reader["Total"]);
                            dict[day] = total;
                        }
                    }
                    steps = dict.Values.ToList();
                }

                // CALORIES
                string queryCalories = @"
            SELECT DATENAME(WEEKDAY, date) AS DayName, SUM(calories) AS Total
            FROM student_calories
            WHERE users_id = @UserId
              AND date BETWEEN @WeekStart AND @WeekEnd
            GROUP BY DATENAME(WEEKDAY, date)";
                using (SqlCommand cmd = new SqlCommand(queryCalories, conn))
                {
                    cmd.Parameters.AddWithValue("@UserId", userId);
                    cmd.Parameters.AddWithValue("@WeekStart", weekStart);
                    cmd.Parameters.AddWithValue("@WeekEnd", weekEnd);

                    var dict = daysOfWeek.ToDictionary(d => d, d => 0);

                    using (SqlDataReader reader = cmd.ExecuteReader())
                    {
                        while (reader.Read())
                        {
                            string day = reader["DayName"].ToString().Substring(0, 3);
                            int total = Convert.ToInt32(reader["Total"]);
                            dict[day] = total;
                        }
                    }
                    calories = dict.Values.ToList();
                }



                DateTime currentdate = DateTime.Today;

                string querySingleCalories = @"
SELECT 
    SUM(calories) AS Total,
    DATENAME(WEEKDAY, date) AS DayName
FROM student_calories
WHERE users_id = @UserId
  AND date = @currentdate
GROUP BY DATENAME(WEEKDAY, date)";

                using (SqlCommand cmd = new SqlCommand(querySingleCalories, conn))
                {
                    cmd.Parameters.AddWithValue("@UserId", userId);
                    cmd.Parameters.AddWithValue("@currentdate", currentdate);

                    using (SqlDataReader reader = cmd.ExecuteReader())
                    {
                        while (reader.Read())
                        {
                            string day = reader["DayName"].ToString();  // e.g. Monday
                            int total = Convert.ToInt32(reader["Total"]);

                            // Ibato sa ViewBag para ma-render sa HTML
                            ViewBag.CaloriesDay = day;
                            ViewBag.CaloriesValue = total;
                        }
                    }
                }


                string querySingleSteps = @"
SELECT 
    SUM(steps) AS Total,
    DATENAME(WEEKDAY, date) AS DayName
FROM student_steps
WHERE users_id = @UserId
  AND date = @currentdate
GROUP BY DATENAME(WEEKDAY, date)";

                using (SqlCommand cmd = new SqlCommand(querySingleSteps, conn))
                {
                    cmd.Parameters.AddWithValue("@UserId", userId);
                    cmd.Parameters.AddWithValue("@currentdate", currentdate);

                    using (SqlDataReader reader = cmd.ExecuteReader())
                    {
                        while (reader.Read())
                        {
                            string day = reader["DayName"].ToString();  // e.g. Monday
                            int total = Convert.ToInt32(reader["Total"]);

                            // Ibato sa ViewBag para ma-render sa HTML
                            ViewBag.StepsDay = day;
                            ViewBag.StepsValue = total;
                        }
                    }
                }


                string querySingleHeartbeat = @"
SELECT 
    SUM(heartbeat) AS Total,
    DATENAME(WEEKDAY, date) AS DayName
FROM student_heartbeat
WHERE users_id = @UserId
  AND date = @currentdate
GROUP BY DATENAME(WEEKDAY, date)";

                using (SqlCommand cmd = new SqlCommand(querySingleHeartbeat, conn))
                {
                    cmd.Parameters.AddWithValue("@UserId", userId);
                    cmd.Parameters.AddWithValue("@currentdate", currentdate);

                    using (SqlDataReader reader = cmd.ExecuteReader())
                    {
                        while (reader.Read())
                        {
                            string day = reader["DayName"].ToString();  // e.g. Monday
                            int total = Convert.ToInt32(reader["Total"]);

                            // Ibato sa ViewBag para ma-render sa HTML
                            ViewBag.HeartbeatDay = day;
                            ViewBag.HeartbeatValue = total;
                        }
                    }
                }
            }

            // I-pass sa View
            ViewBag.Days = JsonConvert.SerializeObject(daysOfWeek);
            ViewBag.Heartbeats = JsonConvert.SerializeObject(heartbeats);
            ViewBag.Steps = JsonConvert.SerializeObject(steps);
            ViewBag.Calories = JsonConvert.SerializeObject(calories);






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




                string queryNotifications = @"
    SELECT TOP 10 id, description, timestamp
    FROM notifications
    WHERE student_id = @UserId
    ORDER BY timestamp DESC";

                using (SqlCommand cmd = new SqlCommand(queryNotifications, conn))
                {
                    cmd.Parameters.AddWithValue("@UserId", Convert.ToInt32(userId));
                    using (SqlDataReader reader = cmd.ExecuteReader())
                    {
                        while (reader.Read())
                        {
                            model.Notifications.Add(new NotificationModel
                            {
                                Id = Convert.ToInt32(reader["id"]),
                                Description = reader["description"].ToString(),
                                TimeStamp = reader["timestamp"].ToString()
                            });
                        }
                    }
                }


                var events = new List<StudentCalendarModel>();

                string queryTrainingSchedule = @"
    -- Student’s own calendar events
    SELECT title, start_date, end_date, NULL AS time, 'Active Resting' AS ActivityType
    FROM student_calendar
    WHERE users_id = @UserId

    UNION

    -- Training schedules where student is selected
    SELECT cts.title, cts.start_date, cts.end_date, cts.time, 'Training' AS ActivityType
    FROM coach_training_schedule cts
    INNER JOIN coach_training_schedule_selected_athletes sa 
        ON cts.id = sa.coach_training_schedule_id
    WHERE sa.student_id = @UserId
";



                using (SqlCommand cmd = new SqlCommand(queryTrainingSchedule, conn))
                {
                    cmd.Parameters.AddWithValue("@UserId", userId);
                    using (SqlDataReader reader = cmd.ExecuteReader())
                    {
                        while (reader.Read())
                        {
                            events.Add(new StudentCalendarModel
                            {
                                Title = reader["title"].ToString(),
                                StartDate = Convert.ToDateTime(reader["start_date"]).ToString("yyyy-MM-dd"),
                                Time = reader["time"].ToString(),
                                ActivityType = reader["ActivityType"].ToString()
                            });
                        }
                    }
                }

                // ilagay sa ViewBag
                ViewBag.Events = events;


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

                var events = new List<StudentCalendarModel>();

                string queryTrainingSchedule = @"
    -- Student’s own calendar events
    SELECT title, start_date, end_date, NULL AS time, 'Active Resting' AS ActivityType
    FROM student_calendar
    WHERE users_id = @UserId

    UNION

    -- Training schedules where student is selected
    SELECT cts.title, cts.start_date, cts.end_date, cts.time, 'Training' AS ActivityType
    FROM coach_training_schedule cts
    INNER JOIN coach_training_schedule_selected_athletes sa 
        ON cts.id = sa.coach_training_schedule_id
    WHERE sa.student_id = @UserId
";



                using (SqlCommand cmd = new SqlCommand(queryTrainingSchedule, conn))
                {
                    cmd.Parameters.AddWithValue("@UserId", userId);
                    using (SqlDataReader reader = cmd.ExecuteReader())
                    {
                        while (reader.Read())
                        {
                            events.Add(new StudentCalendarModel
                            {
                                Title = reader["title"].ToString(),
                                StartDate = Convert.ToDateTime(reader["start_date"]).ToString("yyyy-MM-dd"),
                                Time = reader["time"].ToString(),
                                ActivityType = reader["ActivityType"].ToString()
                            });
                        }
                    }
                }

                // ilagay sa ViewBag
                ViewBag.Events = events;



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


            var sports = new[] { "Basketball", "Volleyball", "Tennis" };
            ViewBag.Sports = new SelectList(sports, model.Sport);

            var yearlevels = new[] { "1st Year", "2nd Year", "3rd Year", "4th Year" };
            ViewBag.YearLevels = new SelectList(yearlevels, model.YearLevel);

            return View(model);
        }

        [HttpPost]
        public IActionResult Profile(StudentProfileModel model, IFormFile ProfilePhoto)
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
                string query = @"UPDATE student_profile SET 
                            firstname=@FirstName,
                            lastname=@LastName,
                            email=@Email,
                            course=@Course,
                            year_level=@YearLevel,
                            contact_number=@ContactNumber,
                            emergency_contact=@EmergencyContact,
                            date_of_birth=@DateOfBirth,
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
                    cmd.Parameters.AddWithValue("@EmergencyContact", model.EmergencyContact ?? "");
                    cmd.Parameters.AddWithValue("@DateOfBirth", model.DateOfBirth ?? "");
                    cmd.Parameters.AddWithValue("@Sport", model.Sport ?? "");
                    cmd.Parameters.AddWithValue("@ProfileImagePath", model.ProfileImagePath ?? "");
                    cmd.Parameters.AddWithValue("@UserId", Convert.ToInt32(model.userId));

                    cmd.ExecuteNonQuery();
                }

                var events = new List<StudentCalendarModel>();



                string queryTrainingSchedule = @"
    -- Student’s own calendar events
    SELECT title, start_date, end_date, NULL AS time, 'Active Resting' AS ActivityType
    FROM student_calendar
    WHERE users_id = @UserId

    UNION

    -- Training schedules where student is selected
    SELECT cts.title, cts.start_date, cts.end_date, cts.time, 'Training' AS ActivityType
    FROM coach_training_schedule cts
    INNER JOIN coach_training_schedule_selected_athletes sa 
        ON cts.id = sa.coach_training_schedule_id
    WHERE sa.student_id = @UserId
";



                using (SqlCommand cmd = new SqlCommand(queryTrainingSchedule, conn))
                {
                    cmd.Parameters.AddWithValue("@UserId", userId);
                    using (SqlDataReader reader = cmd.ExecuteReader())
                    {
                        while (reader.Read())
                        {
                            events.Add(new StudentCalendarModel
                            {
                                Title = reader["title"].ToString(),
                                StartDate = Convert.ToDateTime(reader["start_date"]).ToString("yyyy-MM-dd"),
                                Time = reader["time"].ToString(),
                                ActivityType = reader["ActivityType"].ToString()
                            });
                        }
                    }
                }

                // ilagay sa ViewBag
                ViewBag.Events = events;


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

            ViewBag.Message = "Profile updated successfully!";


            var sports = new[] { "Basketball", "Volleyball", "Tennis" };
            ViewBag.Sports = new SelectList(sports, model.Sport);

            var yearlevels = new[] { "1st Year", "2nd Year", "3rd Year", "4th Year" };
            ViewBag.YearLevels = new SelectList(yearlevels, model.YearLevel);
            return View(model);
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


                var events = new List<StudentCalendarModel>();

                string queryTrainingSchedule = @"
    -- Student’s own calendar events
    SELECT title, start_date, end_date, NULL AS time, 'Active Resting' AS ActivityType
    FROM student_calendar
    WHERE users_id = @UserId

    UNION

    -- Training schedules where student is selected
    SELECT cts.title, cts.start_date, cts.end_date, cts.time, 'Training' AS ActivityType
    FROM coach_training_schedule cts
    INNER JOIN coach_training_schedule_selected_athletes sa 
        ON cts.id = sa.coach_training_schedule_id
    WHERE sa.student_id = @UserId
";



                using (SqlCommand cmd = new SqlCommand(queryTrainingSchedule, conn))
                {
                    cmd.Parameters.AddWithValue("@UserId", userId);
                    using (SqlDataReader reader = cmd.ExecuteReader())
                    {
                        while (reader.Read())
                        {
                            events.Add(new StudentCalendarModel
                            {
                                Title = reader["title"].ToString(),
                                StartDate = Convert.ToDateTime(reader["start_date"]).ToString("yyyy-MM-dd"),
                                Time = reader["time"].ToString(),
                                ActivityType = reader["ActivityType"].ToString()
                            });
                        }
                    }
                }

                // ilagay sa ViewBag
                ViewBag.Events = events;


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

        [HttpPost]
        public IActionResult Bmi(StudentProfileModel model)
        {
            string userId = HttpContext.Session.GetString("userId");
            if (string.IsNullOrEmpty(userId))
                return RedirectToAction("Login", "Login");

            using (SqlConnection conn = new SqlConnection(_configuration.GetConnectionString("DefaultConnectionString")))
            {
                conn.Open();
                string query = @"UPDATE student_profile SET 
                            age=@Age,
                            weight=@Weight,
                            height=@Height
                         WHERE users_id=@UserId";

                using (SqlCommand cmd = new SqlCommand(query, conn))
                {
                    cmd.Parameters.AddWithValue("@Age", model.Age ?? "");
                    cmd.Parameters.AddWithValue("@Weight", model.Weight ?? "");
                    cmd.Parameters.AddWithValue("@Height", model.Height ?? "");
                    cmd.Parameters.AddWithValue("@UserId", Convert.ToInt32(model.userId));

                    cmd.ExecuteNonQuery();
                }

                var events = new List<StudentCalendarModel>();

                string queryTrainingSchedule = @"
    -- Student’s own calendar events
    SELECT title, start_date, end_date, NULL AS time, 'Active Resting' AS ActivityType
    FROM student_calendar
    WHERE users_id = @UserId

    UNION

    -- Training schedules where student is selected
    SELECT cts.title, cts.start_date, cts.end_date, cts.time, 'Training' AS ActivityType
    FROM coach_training_schedule cts
    INNER JOIN coach_training_schedule_selected_athletes sa 
        ON cts.id = sa.coach_training_schedule_id
    WHERE sa.student_id = @UserId
";



                using (SqlCommand cmd = new SqlCommand(queryTrainingSchedule, conn))
                {
                    cmd.Parameters.AddWithValue("@UserId", userId);
                    using (SqlDataReader reader = cmd.ExecuteReader())
                    {
                        while (reader.Read())
                        {
                            events.Add(new StudentCalendarModel
                            {
                                Title = reader["title"].ToString(),
                                StartDate = Convert.ToDateTime(reader["start_date"]).ToString("yyyy-MM-dd"),
                                Time = reader["time"].ToString(),
                                ActivityType = reader["ActivityType"].ToString()
                            });
                        }
                    }
                }

                // ilagay sa ViewBag
                ViewBag.Events = events;


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

            ViewBag.Message = "BMI updated successfully!";


            return View(model);
        }


        [HttpGet]
        public IActionResult Calendar()
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

                var events = new List<StudentCalendarModel>();

                string queryTrainingSchedule = @"
    -- Student’s own calendar events
    SELECT title, start_date, end_date, NULL AS time, 'Active Resting' AS ActivityType
    FROM student_calendar
    WHERE users_id = @UserId

    UNION

    -- Training schedules where student is selected
    SELECT cts.title, cts.start_date, cts.end_date, cts.time, 'Training' AS ActivityType
    FROM coach_training_schedule cts
    INNER JOIN coach_training_schedule_selected_athletes sa 
        ON cts.id = sa.coach_training_schedule_id
    WHERE sa.student_id = @UserId
";



                using (SqlCommand cmd = new SqlCommand(queryTrainingSchedule, conn))
                {
                    cmd.Parameters.AddWithValue("@UserId", userId);
                    using (SqlDataReader reader = cmd.ExecuteReader())
                    {
                        while (reader.Read())
                        {
                            events.Add(new StudentCalendarModel
                            {
                                Title = reader["title"].ToString(),
                                StartDate = Convert.ToDateTime(reader["start_date"]).ToString("yyyy-MM-dd"),
                                Time = reader["time"].ToString(),
                                ActivityType = reader["ActivityType"].ToString()
                            });
                        }
                    }
                }

                // ilagay sa ViewBag
                ViewBag.Events = events;



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

        [HttpGet]
        public IActionResult CalendarData()
        {
            string userId = HttpContext.Session.GetString("userId");
            var events = new List<StudentCalendarModel>();

            using (SqlConnection conn = new SqlConnection(_configuration.GetConnectionString("DefaultConnectionString")))
            {
                conn.Open();

                string query = @"
            -- Student’s own calendar events
            SELECT title, start_date, end_date
            FROM student_calendar
            WHERE users_id = @UserId

            UNION

            -- Training schedules where student is selected
            SELECT cts.title, cts.start_date, cts.end_date
            FROM coach_training_schedule cts
            INNER JOIN coach_training_schedule_selected_athletes sa 
                ON cts.id = sa.coach_training_schedule_id
            WHERE sa.student_id = @UserId
        ";

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

                var events = new List<StudentCalendarModel>();

                string queryTrainingSchedule = @"
    -- Student’s own calendar events
    SELECT title, start_date, end_date, NULL AS time, 'Active Resting' AS ActivityType
    FROM student_calendar
    WHERE users_id = @UserId

    UNION

    -- Training schedules where student is selected
    SELECT cts.title, cts.start_date, cts.end_date, cts.time, 'Training' AS ActivityType
    FROM coach_training_schedule cts
    INNER JOIN coach_training_schedule_selected_athletes sa 
        ON cts.id = sa.coach_training_schedule_id
    WHERE sa.student_id = @UserId
";



                using (SqlCommand cmd = new SqlCommand(queryTrainingSchedule, conn))
                {
                    cmd.Parameters.AddWithValue("@UserId", userId);
                    using (SqlDataReader reader = cmd.ExecuteReader())
                    {
                        while (reader.Read())
                        {
                            events.Add(new StudentCalendarModel
                            {
                                Title = reader["title"].ToString(),
                                StartDate = Convert.ToDateTime(reader["start_date"]).ToString("yyyy-MM-dd"),
                                Time = reader["time"].ToString(),
                                ActivityType = reader["ActivityType"].ToString()
                            });
                        }
                    }
                }

                // ilagay sa ViewBag
                ViewBag.Events = events;
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
                            //Orig Code
                            //                double grade = Convert.ToDouble(reader["grade"]);
                            //                total += grade;
                            //                count++;
                            //                grades.Add(new StudentAcademicMonitoringModel
                            //                {
                            //                    Subject = reader["subject"].ToString(),
                            //                    Grade = grade.ToString()
                            //                });

                            //NEW CODE!!
                            string rawGrade = reader["grade"].ToString().Trim();
                            string subject = reader["subject"].ToString();

                            if (double.TryParse(rawGrade, out double parsedGrade))
                            {
                                total += parsedGrade;
                                count++;
                            }

                            // Always add the grade to the list, whether numeric or not
                            grades.Add(new StudentAcademicMonitoringModel
                            {
                                Subject = subject,
                                Grade = rawGrade
                            });
                            //END OF NEW
                        }
                    }
                }
            }

            ViewBag.Grades = grades;
            ViewBag.GWA = count > 0 ? (total / count) : 0; // compute GWA

            //NEW!! If may 4, 5, Incomplete, Dropped, Withdrawn means at risk 
            bool hasDisqualifyingGrade = grades.Any(g =>
                    g.Grade == "4" ||
                    g.Grade == "5" ||
                    g.Grade.Equals("Incomplete", StringComparison.OrdinalIgnoreCase) ||
                    g.Grade.Equals("Dropped", StringComparison.OrdinalIgnoreCase) ||
                    g.Grade.Equals("Withdrawn", StringComparison.OrdinalIgnoreCase)
                );

            double gwa = count > 0 ? (total / count) : 0;
            ViewBag.GWA = gwa;
            ViewBag.IsQualified = (gwa <= 3) && !hasDisqualifyingGrade;
            //END OF NEW


            // 🔹 Check kung may upload record na
            using (SqlConnection conn = new SqlConnection(_configuration.GetConnectionString("DefaultConnectionString")))
            {
                conn.Open();
                string query = "SELECT COUNT(*) FROM student_academic_registration WHERE users_id=@UserId";
                using (SqlCommand cmd = new SqlCommand(query, conn))
                {
                    cmd.Parameters.AddWithValue("@UserId", userId);
                    int exists = (int)cmd.ExecuteScalar();
                    ViewBag.HasSubmitted = exists > 0; // true kung may record
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

        [HttpPost]
        //Orig code
        //public IActionResult AcademicMonitoringGrade(int userId, List<string> Subject, List<string> Grade)
        
        public IActionResult AcademicMonitoringGrade(int userId, List<string> Subject, List<string> Grade, IFormFile GradeFile)
        {
            string connStr = _configuration.GetConnectionString("DefaultConnectionString");
            //Orig Code
            //StudentProfileModel model = new StudentProfileModel();

            //To make the proof of file submission work
            StudentAcademicMonitoringModel model = new StudentAcademicMonitoringModel();
            model.userId = userId.ToString();

            // NEW!!! Submission of proof of grades
            Console.WriteLine("GradeFile is null? " + (GradeFile == null));
            Console.WriteLine("GradeFile length: " + (GradeFile?.Length ?? 0));
            // --- Handle file upload ---
            if (GradeFile != null && GradeFile.Length > 0)
            {
                var safeFileName = Path.GetFileNameWithoutExtension(GradeFile.FileName);
                safeFileName = string.Join("_", safeFileName.Split(Path.GetInvalidFileNameChars()));
                var extension = Path.GetExtension(GradeFile.FileName);
                var fileName = $"{userId}_{safeFileName}{extension}";

                var imagesFolder = Path.Combine(_env.WebRootPath, "files");
                if (!Directory.Exists(imagesFolder))
                    Directory.CreateDirectory(imagesFolder);

                var filePath = Path.Combine(imagesFolder, fileName);
                using (var stream = new FileStream(filePath, FileMode.Create))
                    GradeFile.CopyTo(stream);

                model.GradeFile = "/files/" + fileName;      
            }

            using (SqlConnection conn = new SqlConnection(connStr))
            {
                conn.Open();

                for (int i = 0; i < Subject.Count; i++)
                {
                    //NEW!!
                    Console.WriteLine($"Inserting: Subject={Subject[i]}, Grade={Grade[i]}, File={model.GradeFile}");
                    //Orig Code
                    //string sql = "INSERT INTO student_grade (users_id, subject, grade) VALUES (@userId, @subject, @grade)";

                    //Added the file_path for file submission here 
                    string sql = "INSERT INTO student_grade (users_id, subject, grade, file_path) VALUES (@userId, @subject, @grade, @filePath)"; 

                    using (SqlCommand cmd = new SqlCommand(sql, conn))
                    {
                        cmd.Parameters.AddWithValue("@userId", userId);
                        cmd.Parameters.AddWithValue("@subject", Subject[i]);
                        cmd.Parameters.AddWithValue("@grade", Grade[i]);
                        //added this for file submission
                        cmd.Parameters.AddWithValue("@filePath", model.GradeFile ?? "");
                        cmd.ExecuteNonQuery();
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
            }
            //NEW!! lagay sa view
            TempData["FilePath"] = model.GradeFile;
            // after insert balik sa view
            return RedirectToAction("AcademicMonitoring");
        }


//COR submission
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
            StudentProfileModel profileModel = new StudentProfileModel();
            profileModel.userId = userId;

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
                            profileModel.TotalKm = reader["TotalKm"] != DBNull.Value ? Convert.ToDecimal(reader["TotalKm"]) : 0;
                            int KmTotalDays = reader["TotalDays"] != DBNull.Value ? Convert.ToInt32(reader["TotalDays"]) : 0;

                            // ✅ save as "X/7" format
                            profileModel.KmTotalDays = $"{KmTotalDays}/7";

                            // ✅ compute percentage base sa 5km target
                            decimal targetKm = 5;
                            decimal percentage = (profileModel.TotalKm / targetKm) * 100;

                            if (percentage > 100)
                                percentage = 100;

                            profileModel.KmPercentage = percentage;
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

                            profileModel.SleepTotalDays = $"{SleepTotalDays}/7";

                            decimal targetHours = 7 * 7; // 49 hours
                            decimal percentage = (totalHoursFromDb / targetHours) * 100;

                            if (percentage > 100)
                                percentage = 100;

                            profileModel.HoursPercentage = Math.Round(percentage, 0);
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

                            profileModel.CaloriesTotalDays = $"{CaloriesTotalDays}/7";

                            // ✅ target 1,000 calories/day × 7 days
                            decimal targetCalories = 1000 * 7; // 7,000 calories
                            decimal percentage = (totalCaloriesFromDb / targetCalories) * 100;

                            if (percentage > 100)
                                percentage = 100;

                            profileModel.CaloriesPercentage = Math.Round(percentage, 0); // separate property
                        }
                    }
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


                // kunin lahat ng roles mula users table
                string coachQuery = "SELECT id, firstname, lastname FROM users WHERE role = 2";
                using (SqlCommand roleCmd = new SqlCommand(coachQuery, conn))
                {
                    using (SqlDataReader roleReader = roleCmd.ExecuteReader())
                    {
                        while (roleReader.Read())
                        {
                            model.Coaches.Add(new SelectListItem
                            {
                                Value = roleReader["id"].ToString(), // ilalagay sa option value
                                Text = roleReader["firstname"].ToString() + " " + roleReader["lastname"].ToString() // display name
                            });
                        }
                    }
                }


            }

            // 🔹 Check kung may upload record na
            using (SqlConnection conn = new SqlConnection(_configuration.GetConnectionString("DefaultConnectionString")))
            {
                conn.Open();
                string query = "SELECT COUNT(*) FROM student_athlete_profile WHERE users_id=@UserId AND status!=2";
                using (SqlCommand cmd = new SqlCommand(query, conn))
                {
                    cmd.Parameters.AddWithValue("@UserId", userId);
                    int exists = (int)cmd.ExecuteScalar();
                    ViewBag.HasSubmitted = exists > 0; // true kung may record
                }
            }




            using (SqlConnection conn = new SqlConnection(_configuration.GetConnectionString("DefaultConnectionString")))
            {
                conn.Open();




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
                   sap.coach_id, sap.coach_id, sap.course_one, sap.course_two, sap.course_three,
                   sp.profile_image
            FROM student_athlete_profile sap 
            INNER JOIN users u ON sap.users_id = u.id 
            INNER JOIN student_profile sp ON sp.users_id = u.id 
            WHERE sp.users_id = @StudentId";

                StudentAthleteProfileModel student = null;

                using (SqlCommand cmd = new SqlCommand(studentProfileQuery, conn))
                {
                    cmd.Parameters.AddWithValue("@StudentId", userId);
                    using (SqlDataReader reader = cmd.ExecuteReader())
                    {
                        if (reader.Read())
                        {
                            student = new StudentAthleteProfileModel
                            {
                                userId = reader["users_id"].ToString(),
                                coachId = reader["coach_id"].ToString(),
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
                            };
                        }
                    }
                }
                model.coachId = student.coachId;

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
                ViewBag.StudentAthleteProfileAndRecords = studentModel;



                var events = new List<StudentCalendarModel>();

                string queryTrainingSchedule = @"
    -- Student’s own calendar events
    SELECT title, start_date, end_date, NULL AS time, 'Active Resting' AS ActivityType
    FROM student_calendar
    WHERE users_id = @UserId

    UNION

    -- Training schedules where student is selected
    SELECT cts.title, cts.start_date, cts.end_date, cts.time, 'Training' AS ActivityType
    FROM coach_training_schedule cts
    INNER JOIN coach_training_schedule_selected_athletes sa 
        ON cts.id = sa.coach_training_schedule_id
    WHERE sa.student_id = @UserId
";



                using (SqlCommand cmd = new SqlCommand(queryTrainingSchedule, conn))
                {
                    cmd.Parameters.AddWithValue("@UserId", userId);
                    using (SqlDataReader reader = cmd.ExecuteReader())
                    {
                        while (reader.Read())
                        {
                            events.Add(new StudentCalendarModel
                            {
                                Title = reader["title"].ToString(),
                                StartDate = Convert.ToDateTime(reader["start_date"]).ToString("yyyy-MM-dd"),
                                Time = reader["time"].ToString(),
                                ActivityType = reader["ActivityType"].ToString()
                            });
                        }
                    }
                }

                // ilagay sa ViewBag
                ViewBag.Events = events;


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


        [HttpPost]
        public IActionResult AthleteProfile(StudentAthleteProfileModel model, IFormFile Photo, IFormFile Portfolio, List<string> Name, List<string> Level, List<string> Year, List<string> Award)
        {

            string userId = HttpContext.Session.GetString("userId");
            if (string.IsNullOrEmpty(userId))
                return RedirectToAction("Login", "Login");
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
                int studentAthleteProfileId = 0;

                // 1️⃣ Check kung may existing record
                string checkSql = "SELECT id FROM student_athlete_profile WHERE users_id = @userId";
                int? existingId = null;
                using (SqlCommand cmd = new SqlCommand(checkSql, conn))
                {
                    cmd.Parameters.AddWithValue("@userId", model.userId ?? "");
                    var result = cmd.ExecuteScalar();
                    if (result != null)
                        existingId = Convert.ToInt32(result);
                }

                if (existingId.HasValue)
                {
                    // 🔹 Build dynamic SQL para sa update
                    var updateFields = new List<string>
    {
        "coach_id=@coachId",
        "course_one=@CourseOne",
        "course_two=@CourseTwo",
        "course_three=@CourseThree",
        "elementary_school=@ElementarySchool",
        "elementary_year_graduated=@ElementaryYearGraduated",
        "secondary_school=@SecondarySchool",
        "secondary_year_graduated=@SecondaryYearGraduated",
        "senior_high_school=@SeniorHighSchool",
        "senior_high_year_graduated=@SeniorHighYearGraduated",
        "shs_track_or_strand=@ShsTrackOrStrand",
        "g_ten_gwa=@GTenGwa",
        "g_eleven_gwa=@GElevenGwa",
        "g_twelve_gwa=@GTwelveGwa",
        "transfer_status=@TransferStatus",
        "prev_program_or_course=@PrevProgramOrCourse",
        "vaccination=@Vaccination",
        "philhealth_number=@PhilhealthNumber",
        "event=@Event",
        "home_address=@HomeAddress",
        "provincial_address=@ProvincialAddress",
        "status=0"
    };

                    // 🔹 Idagdag lang yung photo at portfolio kung may bagong upload
                    if (!string.IsNullOrEmpty(model.Photo))
                        updateFields.Add("photo=@Photo");

                    if (!string.IsNullOrEmpty(model.Portfolio))
                        updateFields.Add("portfolio=@Portfolio");

                    string updateSql = $"UPDATE student_athlete_profile SET {string.Join(", ", updateFields)} WHERE id=@Id";

                    using (SqlCommand cmd = new SqlCommand(updateSql, conn))
                    {
                        cmd.Parameters.AddWithValue("@Id", existingId.Value);
                        cmd.Parameters.AddWithValue("@coachId", model.coachId ?? "");
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

                        if (!string.IsNullOrEmpty(model.Photo))
                            cmd.Parameters.AddWithValue("@Photo", model.Photo);

                        if (!string.IsNullOrEmpty(model.Portfolio))
                            cmd.Parameters.AddWithValue("@Portfolio", model.Portfolio);

                        cmd.ExecuteNonQuery();
                    }

                    studentAthleteProfileId = existingId.Value;
                }

                else
                {
                    // 3️⃣ Insert new record
                    string insertSql = @"INSERT INTO student_athlete_profile 
            (users_id,coach_id,photo,course_one,course_two,course_three,elementary_school,elementary_year_graduated,
             secondary_school,secondary_year_graduated,senior_high_school,senior_high_year_graduated,shs_track_or_strand,
             g_ten_gwa,g_eleven_gwa,g_twelve_gwa,transfer_status,prev_program_or_course,vaccination,philhealth_number,
             event,home_address,provincial_address,portfolio,status)
            VALUES
            (@userId,@coachId,@Photo,@CourseOne,@CourseTwo,@CourseThree,@ElementarySchool,@ElementaryYearGraduated,
             @SecondarySchool,@SecondaryYearGraduated,@SeniorHighSchool,@SeniorHighYearGraduated,@ShsTrackOrStrand,
             @GTenGwa,@GElevenGwa,@GTwelveGwa,@TransferStatus,@PrevProgramOrCourse,@Vaccination,@PhilhealthNumber,
             @Event,@HomeAddress,@ProvincialAddress,@Portfolio,0);
            SELECT SCOPE_IDENTITY();";

                    using (SqlCommand cmd = new SqlCommand(insertSql, conn))
                    {
                        cmd.Parameters.AddWithValue("@userId", model.userId ?? "");
                        cmd.Parameters.AddWithValue("@coachId", model.coachId ?? "");
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
                        studentAthleteProfileId = Convert.ToInt32(cmd.ExecuteScalar());
                    }
                }

                // 4️⃣ Insert participations (optional: delete old participations if updating)
                string deleteParticipationSql = "DELETE FROM student_sports_participation WHERE student_athlete_profile_id=@Id";
                using (SqlCommand cmd = new SqlCommand(deleteParticipationSql, conn))
                {
                    cmd.Parameters.AddWithValue("@Id", studentAthleteProfileId);
                    cmd.ExecuteNonQuery();
                }

                for (int i = 0; i < Name.Count; i++)
                {
                    string insertParticipation = "INSERT INTO student_sports_participation (student_athlete_profile_id, name, level, year, award) VALUES (@studentAthleteProfileId, @Name, @Level, @Year, @Award)";
                    using (SqlCommand cmd = new SqlCommand(insertParticipation, conn))
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






            // 🔹 Check kung may upload record na
            using (SqlConnection conn = new SqlConnection(_configuration.GetConnectionString("DefaultConnectionString")))
            {
                conn.Open();
                string query = "SELECT COUNT(*) FROM student_athlete_profile WHERE users_id=@UserId AND status!=2";
                using (SqlCommand cmd = new SqlCommand(query, conn))
                {
                    cmd.Parameters.AddWithValue("@UserId", userId);
                    int exists = (int)cmd.ExecuteScalar();
                    ViewBag.HasSubmitted = exists > 0; // true kung may record
                }


            }


            using (SqlConnection conn = new SqlConnection(_configuration.GetConnectionString("DefaultConnectionString")))
            {
                conn.Open();




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
                   sap.coach_id, sap.course_one, sap.course_two, sap.course_three,
                   sp.profile_image
            FROM student_athlete_profile sap 
            INNER JOIN users u ON sap.users_id = u.id 
            INNER JOIN student_profile sp ON sp.users_id = u.id 
            WHERE sp.users_id = @StudentId";

                StudentAthleteProfileModel student = null;

                using (SqlCommand cmd = new SqlCommand(studentProfileQuery, conn))
                {
                    cmd.Parameters.AddWithValue("@StudentId", userId);
                    using (SqlDataReader reader = cmd.ExecuteReader())
                    {
                        if (reader.Read())
                        {
                            student = new StudentAthleteProfileModel
                            {
                                userId = reader["users_id"].ToString(),
                                coachId = reader["coach_id"].ToString(),
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
                            };
                        }
                    }
                }
                model.coachId = student?.coachId; // nullable safe

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
                ViewBag.StudentAthleteProfileAndRecords = studentModel;

            }

            return RedirectToAction("AthleteProfile");
        }




        public IActionResult Logout()
        {
            HttpContext.Session.Clear(); // burahin lahat ng session
            return RedirectToAction("Login", "Login");
        }






    }
}
