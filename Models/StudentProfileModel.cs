﻿using Microsoft.AspNetCore.Mvc.Rendering;
using System.ComponentModel.DataAnnotations;

namespace FITNSS.Models
{
    public class StudentProfileModel
    {
        public string userId { get; set; }
        public string ProfileImagePath { get; set; } = "/images/profile.png";

        public string FirstName { get; set; }
        public string LastName { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public string Course { get; set; } = string.Empty;
        public string YearLevel { get; set; } = string.Empty;
        public string ContactNumber { get; set; } = string.Empty;
        public string EmergencyContact { get; set; } = string.Empty;
        //Orig Code
        //public string DateOfBirth { get; set; } = string.Empty;

        //NEW! CHANGE THE DOB TO THIS DUE TO ERROR 
        [DataType(DataType.Date)]
        public DateTime? DateOfBirth { get; set; }
        //END OF NEW

        public string Age { get; set; } = string.Empty;
        public string Sport { get; set; } = string.Empty;
        public string Weight { get; set; } = string.Empty;
        public string Height { get; set; } = string.Empty;
        public string Status { get; set; } = string.Empty;

        //NEW! FOR BMI Computed Fields
        public string Bmi { get; set; } // can be computed from Weight & Height
        public string BmiCategory { get; set; }

        //NEW FOR TARGET BIOMETRICS DEPENDS ON THE BMI
        public int? TargetHeartbeat { get; set; }  // bpm
        public int? TargetSteps { get; set; }      // steps per day
        public int? TargetCalories { get; set; }   // kcal per day
        public DateTime? BmiLastUpdated { get; set; }
        //END OF NEW

        // student running
        public decimal TotalKm { get; set; }
        public decimal KmPercentage { get; set; }
        public string KmTotalDays { get; set; } = string.Empty;


        // student sleeping
        public decimal TotalHours { get; set; }
        public decimal HoursPercentage { get; set; }
        public string SleepTotalDays { get; set; } = string.Empty;


        // student calories
        public decimal TotalCalories { get; set; }
        public decimal CaloriesPercentage { get; set; }
        public string CaloriesTotalDays { get; set; } = string.Empty;


        public string coachId { get; set; }
        public List<SelectListItem> Coaches { get; set; } = new List<SelectListItem>();
        public List<NotificationModel> Notifications { get; set; } = new List<NotificationModel>();

    }
}

public class NotificationModel
{
    public int Id { get; set; }
    public string Description { get; set; }
    public string TimeStamp { get; set; }
}