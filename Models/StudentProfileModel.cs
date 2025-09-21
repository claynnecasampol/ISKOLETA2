using Microsoft.AspNetCore.Mvc.Rendering;
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

        [DataType(DataType.Date)]
        public DateTime? DateOfBirth { get; set; }

        public string Age { get; set; } = string.Empty;
        public string Sport { get; set; } = string.Empty;
        public string Weight { get; set; } = string.Empty;
        public string Height { get; set; } = string.Empty;
        public string Status { get; set; } = string.Empty;

        // BMI Computed Fields
        public string Bmi { get; set; }
        public string BmiCategory { get; set; }

        // Target Biometrics
        public int? TargetHeartbeat { get; set; }
        public int? TargetSteps { get; set; }
        public int? TargetCalories { get; set; }
        public DateTime? BmiLastUpdated { get; set; }

        // Student Activity Tracking
        public decimal TotalKm { get; set; }
        public decimal KmPercentage { get; set; }
        public string KmTotalDays { get; set; } = string.Empty;

        public decimal TotalHours { get; set; }
        public decimal HoursPercentage { get; set; }
        public string SleepTotalDays { get; set; } = string.Empty;

        public decimal TotalCalories { get; set; }
        public decimal CaloriesPercentage { get; set; }
        public string CaloriesTotalDays { get; set; } = string.Empty;

        // Coach and Notification Lists
        public string coachId { get; set; }
        public List<SelectListItem> Coaches { get; set; } = new List<SelectListItem>();
        public List<SelectListItem> CoachList { get; set; } = new List<SelectListItem>();
        public List<NotificationModel> Notifications { get; set; } = new List<NotificationModel>();

        // 🔹 NEW: Coach Verification Properties
        public string VerificationStatus { get; set; } = string.Empty; // "pending", "verified", "rejected", or empty
        public string VerificationRemarks { get; set; } = string.Empty; // Coach's remarks for rejection
        public string SelectedCoachId { get; set; } = string.Empty; // ID of the coach for verification
        public string SelectedSport { get; set; } = string.Empty; // Sport selected for verification
        public DateTime? DateRequested { get; set; } // When verification was requested
        public DateTime? DateVerified { get; set; } // When verification was completed
        public bool HasPendingVerification { get; set; } = false; // Quick check for pending status
    }
}

public class NotificationModel
{
    public int Id { get; set; }
    public string Description { get; set; }
    public string TimeStamp { get; set; }
}