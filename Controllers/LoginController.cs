using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Microsoft.Data.SqlClient;
using FITNSS.Models;
using BCrypt.Net;
using Microsoft.IdentityModel.Tokens;
using Microsoft.AspNetCore.Http;
namespace FITNSS.Controllers
{
    public class LoginController : Controller
    {
        private readonly ILogger<LoginController> _logger;
        private readonly IConfiguration _configuration;

        public LoginController(ILogger<LoginController> logger, IConfiguration configuration)
        {
            _logger = logger;
            _configuration = configuration;
        }


        public IActionResult Login()
        {
            return View();
        }

        [HttpPost]
        public IActionResult Login(string email, string password)
        {
            if (string.IsNullOrWhiteSpace(email) || string.IsNullOrWhiteSpace(password))
            {
                ViewData["Message"] = "All fields are required.";
                return View();
            }

            var connectionString = _configuration.GetConnectionString("DefaultConnectionString");

            try
            {
                using var connection = new SqlConnection(connectionString);
                connection.Open();

                //Orig Code
                //var query = "SELECT id, role, password FROM users WHERE email = @Email";

                //NEW!! Added firstname for firstname sa dashboard
                var query = "SELECT id, role, password, firstname FROM users WHERE email = @Email";
                //END OF NEW
                using var command = new SqlCommand(query, connection);
                command.Parameters.AddWithValue("@Email", email);

                using var reader = command.ExecuteReader();
                if (!reader.Read())
                {
                    ViewData["Message"] = "Invalid email or password.";
                    return View();
                }

                var userId = reader["id"]?.ToString();
                var userRole = reader["role"]?.ToString();
                var dbPasswordHash = reader["password"]?.ToString();
                //NEW!!
                var firstName = reader["firstname"]?.ToString();
                //END OF NEW

                if (string.IsNullOrEmpty(dbPasswordHash) || !BCrypt.Net.BCrypt.Verify(password, dbPasswordHash))
                {
                    ViewData["Message"] = "Invalid email or password.";
                    return View();
                }

                HttpContext.Session.SetString("userId", userId ?? "defaultUserId");
                HttpContext.Session.SetString("userRole", userRole ?? "defaultUserRole");
                //NEW!!
                HttpContext.Session.SetString("firstName", firstName ?? "Guest");
                //END OF NEW

                // NEW!! Set greeting based on current time
                var now = DateTime.Now.TimeOfDay;
                string greeting;

                if (now >= TimeSpan.FromHours(0) && now < TimeSpan.FromHours(12))
                {
                    greeting = "Good Morning";
                }
                else if (now >= TimeSpan.FromHours(12) && now < TimeSpan.FromHours(18))
                {
                    greeting = "Good Afternoon";
                }
                else
                {
                    greeting = "Good Evening";
                }

                HttpContext.Session.SetString("greeting", greeting);
                // END OF NEW


                return userRole switch
                {
                    "3" => Redirect("/Student/Dashboard"),       // Student
                    "1" => Redirect("/Sdpo/Dashboard"), // Admin
                    "2" => Redirect("/Coach/Dashboard"), // Coach
                    _ => Redirect("/")                         // Client
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error during login");
                ViewData["Message"] = $"Login exception: {ex.Message}"; // show real error during testing
                return View();
            }
        }


        public IActionResult Logout()
        {
            // Destroy the session
            HttpContext.Session.Clear();

            // Redirect the user to the home page or login page
            return RedirectToAction("Index", "Home"); // Or your login page
        }
    }
}
