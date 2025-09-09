using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Microsoft.Data.SqlClient;
using FITNSS.Models;

namespace FITNSS.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class ApiController : ControllerBase
    {
        private readonly IConfiguration _configuration;
        private readonly IWebHostEnvironment _env;
        private readonly ILogger<ApiController> _logger;

        public ApiController(IConfiguration configuration,
                             ILogger<ApiController> logger,
                             IWebHostEnvironment env)
        {
            _configuration = configuration;
            _logger = logger;
            _env = env;
        }

        [HttpGet("SaveHeartRate")]
        public IActionResult SaveHeartRate([FromQuery] int heartbeat, int userId)
        {
            _logger.LogInformation("Received heartbeat {bpm} for user {userId}", heartbeat, userId);

            try
            {
                string connectionString = _configuration.GetConnectionString("DefaultConnectionString");

                using (SqlConnection conn = new SqlConnection(connectionString))
                {
                    conn.Open();

                    // SQL Upsert
                    string query = @"
IF EXISTS (SELECT 1 FROM student_heartbeat 
           WHERE users_id = @userId AND CAST(date AS DATE) = CAST(GETDATE() AS DATE))
    UPDATE student_heartbeat
    SET heartbeat = @HeartBeat, date = GETDATE()
    WHERE users_id = @userId AND CAST(date AS DATE) = CAST(GETDATE() AS DATE)
ELSE
    INSERT INTO student_heartbeat (users_id, heartbeat, date)
    VALUES (@userId, @HeartBeat, GETDATE())";

                    using (SqlCommand cmd = new SqlCommand(query, conn))
                    {
                        cmd.Parameters.AddWithValue("@userId", userId);
                        cmd.Parameters.AddWithValue("@HeartBeat", heartbeat);
                        cmd.ExecuteNonQuery();
                    }
                }

                return Ok(new { success = true, message = $"Heart rate {heartbeat} saved!" });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error saving heart rate");
                return StatusCode(500, new { success = false, message = ex.Message });
            }
        }



        [HttpGet("SaveKm")]
        public IActionResult SaveKm([FromQuery] double km, int userId)
        {
            _logger.LogInformation("Received km {km} for user {userId}", km, userId);

            try
            {
                string connectionString = _configuration.GetConnectionString("DefaultConnectionString");

                using (SqlConnection conn = new SqlConnection(connectionString))
                {
                    conn.Open();

                    // SQL Upsert
                    string query = @"
IF EXISTS (SELECT 1 FROM student_running 
           WHERE users_id = @userId AND CAST(date AS DATE) = CAST(GETDATE() AS DATE))
    UPDATE student_running
    SET km = @Km, date = GETDATE()
    WHERE users_id = @userId AND CAST(date AS DATE) = CAST(GETDATE() AS DATE)
ELSE
    INSERT INTO student_running (users_id, km, date)
    VALUES (@userId, @Km, GETDATE())";

                    using (SqlCommand cmd = new SqlCommand(query, conn))
                    {
                        cmd.Parameters.AddWithValue("@userId", userId);
                        cmd.Parameters.AddWithValue("@Km", km); // double is fine
                        cmd.ExecuteNonQuery();
                    }
                }

                return Ok(new { success = true, message = $"Km {km} saved!" });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error saving Km");
                return StatusCode(500, new { success = false, message = ex.Message });
            }
        }




        [HttpGet("SaveCalories")]
        public IActionResult SaveCalories([FromQuery] double calories, int userId)
        {
            _logger.LogInformation("Received calories {calories} for user {userId}", calories, userId);

            try
            {
                string connectionString = _configuration.GetConnectionString("DefaultConnectionString");

                using (SqlConnection conn = new SqlConnection(connectionString))
                {
                    conn.Open();

                    // SQL Upsert
                    string query = @"
IF EXISTS (SELECT 1 FROM student_calories 
           WHERE users_id = @userId AND CAST(date AS DATE) = CAST(GETDATE() AS DATE))
    UPDATE student_calories
    SET calories = @Calories, date = GETDATE()
    WHERE users_id = @userId AND CAST(date AS DATE) = CAST(GETDATE() AS DATE)
ELSE
    INSERT INTO student_calories (users_id, calories, date)
    VALUES (@userId, @Calories, GETDATE())";

                    using (SqlCommand cmd = new SqlCommand(query, conn))
                    {
                        cmd.Parameters.AddWithValue("@userId", userId);
                        cmd.Parameters.AddWithValue("@Calories", calories);
                        cmd.ExecuteNonQuery();
                    }
                }

                return Ok(new { success = true, message = $"Calories {calories} saved!" });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error saving Calories");
                return StatusCode(500, new { success = false, message = ex.Message });
            }
        }



        [HttpGet("SaveHours")]
        public IActionResult SaveHours([FromQuery] int hours, int userId)
        {
            _logger.LogInformation("Received km {km} for user {userId}", hours, userId);

            try
            {
                string connectionString = _configuration.GetConnectionString("DefaultConnectionString");

                using (SqlConnection conn = new SqlConnection(connectionString))
                {
                    conn.Open();

                    // SQL Upsert
                    string query = @"
IF EXISTS (SELECT 1 FROM student_sleeping 
           WHERE users_id = @userId AND CAST(date AS DATE) = CAST(GETDATE() AS DATE))
    UPDATE student_sleeping
    SET hours = @Hours, date = GETDATE()
    WHERE users_id = @userId AND CAST(date AS DATE) = CAST(GETDATE() AS DATE)
ELSE
    INSERT INTO student_sleeping (users_id, hours, date)
    VALUES (@userId, @Hours, GETDATE())";

                    using (SqlCommand cmd = new SqlCommand(query, conn))
                    {
                        cmd.Parameters.AddWithValue("@userId", userId);
                        cmd.Parameters.AddWithValue("@Hours", hours); // double is fine
                        cmd.ExecuteNonQuery();
                    }
                }

                return Ok(new { success = true, message = $"Hours {hours} saved!" });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error saving Hours");
                return StatusCode(500, new { success = false, message = ex.Message });
            }
        }

        [HttpGet("Login")]
        public IActionResult Login([FromQuery] string username, [FromQuery] string password)
        {
            var connectionString = _configuration.GetConnectionString("DefaultConnectionString");

            try
            {
                using var connection = new SqlConnection(connectionString);
                connection.Open();

                var query = "SELECT id, password FROM users WHERE username = @Username";
                using var command = new SqlCommand(query, connection);
                command.Parameters.AddWithValue("@Username", username);

                using var reader = command.ExecuteReader();
                if (!reader.Read())
                {
                    // Return JSON instead of View
                    return Ok(new { success = false, message = "Invalid username or password." });
                }

                var userId = reader["id"]?.ToString();
                var dbPasswordHash = reader["password"]?.ToString();

                // TODO: Verify password if hashed
                if (string.IsNullOrEmpty(dbPasswordHash) || !BCrypt.Net.BCrypt.Verify(password, dbPasswordHash))
                {
                    return Ok(new { success = false, message = "Invalid password." });
                }
                //if (!VerifyPassword(password, dbPasswordHash)) 
                //     

                // Optionally save session (not necessary for Flutter)
                HttpContext.Session.SetString("userId", userId ?? "defaultUserId");

                // Return JSON
                return Ok(new { success = true, userId = userId });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error during login");
                return StatusCode(500, new { success = false, message = ex.Message });
            }
        }



    }
}
