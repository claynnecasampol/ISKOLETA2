using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Microsoft.Data.SqlClient;
using FITNSS.Models;
using BCrypt.Net;
using Microsoft.IdentityModel.Tokens;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using System.Text.RegularExpressions;

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

        public IActionResult Login(string type = "student")
        {
            ViewBag.UserType = type;
            return View();
        }

        [HttpPost]
        public IActionResult Login(string loginInput, string password, string userType)
        {
            // Clean and validate inputs
            loginInput = loginInput?.Trim() ?? "";
            password = password?.Trim() ?? "";
            userType = userType?.Trim() ?? "student";

            if (string.IsNullOrWhiteSpace(loginInput) || string.IsNullOrWhiteSpace(password))
            {
                ViewData["Message"] = "All fields are required.";
                ViewBag.UserType = userType;
                return View();
            }

            var connectionString = _configuration.GetConnectionString("DefaultConnectionString");

            try
            {
                using var connection = new SqlConnection(connectionString);
                connection.Open();

                string query;

                // Determine if login is for SDPO (5-digit org ID) or Student (email)
                if (userType == "sdpo" || userType == "coach")
                {
                    if (!IsValidOrganizationId(loginInput))
                    {
                        ViewData["Message"] = "Please enter a valid 5-digit organization ID.";
                        ViewBag.UserType = userType;
                        return View();
                    }

                    query = "SELECT id, role, password, firstname FROM users WHERE organization_id = @LoginInput AND role IN (1, 2)";
                }
                else if (userType == "student")
                {
                    if (!IsValidEmail(loginInput))
                    {
                        ViewData["Message"] = "Please enter a valid email address.";
                        ViewBag.UserType = userType;
                        return View();
                    }
                    query = "SELECT id, role, password, firstname FROM users WHERE email = @LoginInput AND role = 3";
                }
                else
                {
                    ViewData["Message"] = "Invalid login type.";
                    ViewBag.UserType = userType;
                    return View();
                }

                using var command = new SqlCommand(query, connection);
                command.Parameters.AddWithValue("@LoginInput", loginInput);

                using var reader = command.ExecuteReader();
                if (!reader.Read())
                {
                    ViewData["Message"] = userType == "sdpo" ?
                        "Invalid organization ID or password." :
                        "Invalid email or password.";
                    ViewBag.UserType = userType;
                    return View();
                }

                var userId = reader["id"]?.ToString();
                var userRole = reader["role"]?.ToString();
                var dbPasswordHash = reader["password"]?.ToString();
                var firstName = reader["firstname"]?.ToString();

                // Enhanced password validation with debugging
                bool isValidPassword = ValidatePassword(password, dbPasswordHash);

                if (!isValidPassword)
                {
                    // Log for debugging (remove in production)
                    _logger.LogWarning($"Password validation failed for user {loginInput}. DB Hash length: {dbPasswordHash?.Length ?? 0}");

                    ViewData["Message"] = (userType == "sdpo" || userType == "coach")
                        ? "Invalid organization ID or password."
                        : "Invalid email or password.";

                    ViewBag.UserType = userType;
                    return View();
                }

                HttpContext.Session.SetString("userId", userId ?? "defaultUserId");
                HttpContext.Session.SetString("userRole", userRole ?? "defaultUserRole");
                HttpContext.Session.SetString("firstName", firstName ?? "Guest");
                HttpContext.Session.SetString("userType", userType);

                // Set greeting based on current time
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

                return userRole switch
                {
                    "3" => Redirect("/Student/Dashboard"),
                    "1" => Redirect("/Sdpo/Dashboard"),
                    "2" => Redirect("/Coach/Dashboard"),
                    _ => Redirect("/")
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error during login");
                ViewData["Message"] = $"Login exception: {ex.Message}";
                ViewBag.UserType = userType;
                return View();
            }
        }

        private bool ValidatePassword(string inputPassword, string dbPasswordHash)
        {
            if (string.IsNullOrEmpty(dbPasswordHash) || string.IsNullOrEmpty(inputPassword))
            {
                return false;
            }

            try
            {
                // Check if password appears to be BCrypt hashed
                if (dbPasswordHash.StartsWith("$2") && dbPasswordHash.Length >= 50)
                {
                    // BCrypt hashed password
                    return BCrypt.Net.BCrypt.Verify(inputPassword, dbPasswordHash);
                }
                else
                {
                    // Plain text password (legacy - should be migrated)
                    _logger.LogWarning("Plain text password detected - should be migrated to BCrypt");
                    return inputPassword == dbPasswordHash;
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error validating password");
                return false;
            }
        }

        private bool IsValidOrganizationId(string input)
        {
            if (string.IsNullOrWhiteSpace(input))
                return false;

            input = input.Trim();

            if (input.Length != 5)
                return false;

            foreach (char c in input)
            {
                if (!char.IsDigit(c))
                    return false;
            }

            return true;
        }

        private bool IsValidEmail(string input)
        {
            if (string.IsNullOrWhiteSpace(input))
                return false;

            try
            {
                input = input.Trim();
                var addr = new System.Net.Mail.MailAddress(input);
                return addr.Address == input;
            }
            catch
            {
                return false;
            }
        }

        // Method to hash a password (for testing/migration)
        public IActionResult GenerateHash(string password = "67890")
        {
            string hashed = BCrypt.Net.BCrypt.HashPassword(password);
            return Content($"Original: {password}<br>Hashed: {hashed}<br>Length: {hashed.Length}");
        }

        // Method to verify a password against a hash (for testing)
        public IActionResult VerifyPassword(string password, string hash)
        {
            if (string.IsNullOrEmpty(password) || string.IsNullOrEmpty(hash))
            {
                return Content("Both password and hash are required");
            }

            try
            {
                bool isValid = BCrypt.Net.BCrypt.Verify(password, hash);
                return Content($"Password: {password}<br>Hash: {hash}<br>Valid: {isValid}");
            }
            catch (Exception ex)
            {
                return Content($"Error: {ex.Message}");
            }
        }

        public IActionResult Logout()
        {
            HttpContext.Session.Clear();
            return RedirectToAction("Index", "Home");
        }
    }
}