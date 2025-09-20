﻿using System.Diagnostics;
using FITNSS.Models;
using Microsoft.AspNetCore.Mvc;

namespace FITNSS.Controllers
{
    public class HomeController : Controller
    {
        private readonly ILogger<HomeController> _logger;

        public HomeController(ILogger<HomeController> logger)
        {
            _logger = logger;
        }

        public IActionResult Index()
        {
            //NEW!! For showing of first name in dashboard
            var firstName = HttpContext.Session.GetString("firstName");
            // ✅ Pass to ViewBag para magamit sa View
            ViewBag.FirstName = firstName;
            //END OF NEW

            return View();
        }

        public IActionResult Privacy()
        {
            return View();
        }

        [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
        public IActionResult Error()
        {
            return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
        }
    }
}
