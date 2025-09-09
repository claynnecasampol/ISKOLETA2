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

                var query = "SELECT id, role, password FROM users WHERE email = @Email";
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

                if (string.IsNullOrEmpty(dbPasswordHash) || !BCrypt.Net.BCrypt.Verify(password, dbPasswordHash))
                {
                    ViewData["Message"] = "Invalid email or password.";
                    return View();
                }

                HttpContext.Session.SetString("userId", userId ?? "defaultUserId");
                HttpContext.Session.SetString("userRole", userRole ?? "defaultUserRole");

                return userRole switch
                {
                    "3" => Redirect("/Student/Dashboard"),       // Admin
                    "1" => Redirect("/Sdpo/Dashboard"), // Dentist
                    "2" => Redirect("/Coach/Dashboard"), // Receptionist
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
