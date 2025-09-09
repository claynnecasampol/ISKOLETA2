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
    public class SdpoController : Controller
    {
        private readonly IConfiguration _configuration;

        public SdpoController(IConfiguration configuration)
        {
            _configuration = configuration;
        }
        public IActionResult Dashboard()
        {
            return View();
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

    }
}
