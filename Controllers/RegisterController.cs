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
    public class RegisterController : Controller
    {
        private readonly ILogger<RegisterController> _logger;
        private readonly IConfiguration _configuration;

        public RegisterController(ILogger<RegisterController> logger, IConfiguration configuration)
        {
            _logger = logger;
            _configuration = configuration;
        }


        public IActionResult Register()
        {
            return View();
        }       

        [HttpPost]
        public IActionResult Register(string firstname, string middlename, string lastname, string email, string password)
        {
            if (string.IsNullOrWhiteSpace(firstname) || string.IsNullOrWhiteSpace(middlename) || string.IsNullOrWhiteSpace(lastname) ||
                string.IsNullOrWhiteSpace(email) || string.IsNullOrWhiteSpace(password))
            {
                ViewData["Message"] = "All fields are required.";
                return View();
            }

            var passwordHash = BCrypt.Net.BCrypt.HashPassword(password);


            var connectionString = _configuration.GetConnectionString("DefaultConnectionString");

            try
            {
                using (var connection = new SqlConnection(connectionString))
                {
                    connection.Open();

                    // Check if the email already exists in the database
                    var checkQuery = "SELECT COUNT(1) FROM users WHERE email = @Email";
                    using (var checkCommand = new SqlCommand(checkQuery, connection))
                    {
                        checkCommand.Parameters.AddWithValue("@Email", email);
                        var existingCount = (int)checkCommand.ExecuteScalar();

                        if (existingCount > 0)
                        {
                            ViewData["Message"] = "An account with this email already exists.";
                            return View();
                        }
                    }

                    // If no duplicate is found, proceed to insert the new user
                    var insertQuery = @"
                INSERT INTO users (firstname, middlename, lastname, email, password, role) 
                VALUES (@FirstName, @MiddleName, @LastName, @Email, @PasswordHash, @Role)";

                    using (var insertCommand = new SqlCommand(insertQuery, connection))
                    {
                        insertCommand.Parameters.AddWithValue("@FirstName", firstname);
                        insertCommand.Parameters.AddWithValue("@MiddleName", middlename);
                        insertCommand.Parameters.AddWithValue("@LastName", lastname);
                        insertCommand.Parameters.AddWithValue("@Email", email);
                        insertCommand.Parameters.AddWithValue("@PasswordHash", passwordHash);
                        //CHANGE THE ROLE INTO 3 SINCE THIS IS A STUDENT
                        insertCommand.Parameters.AddWithValue("@Role", 3);

                        insertCommand.ExecuteNonQuery();

                        //NEW!!
                        //For updating the name depends on which user
                        HttpContext.Session.SetString("firstName", firstname);
                        //For prefilled the first name, last name, email in the edit profile 
                        HttpContext.Session.SetString("lastName", lastname);
                        HttpContext.Session.SetString("email", email);
                        //END OF NEW
                    }
                }

                ViewData["Message"] = "Registration successful!";
            }
            catch (SqlException sqlEx)
            {
                _logger.LogError($"SQL Error during registration: {sqlEx.Message} \nStack Trace: {sqlEx.StackTrace}");
                ViewData["Message"] = $"Database error: {sqlEx.Message}";
            }
            catch (Exception ex)
            {
                _logger.LogError($"General Error during registration: {ex.Message} \nStack Trace: {ex.StackTrace}");
                ViewData["Message"] = "An unexpected error occurred during registration.";
            }

            return View();
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
