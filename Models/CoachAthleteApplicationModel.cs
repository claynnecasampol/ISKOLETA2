using Microsoft.AspNetCore.Mvc.Rendering;

namespace FITNSS.Models
{
    public class CoachAthleteApplicationModel
    {
        public string userId { get; set; }
        public string studentId { get; set; }
        public string notifDescription { get; set; }

        public string studentAthleteProfileId { get; set; }
        public string Portfolio { get; set; } = "";
        public string Name { get; set; } = string.Empty;
        public string Photo { get; set; } = "";
        public string Level { get; set; } = string.Empty;
        public string Year { get; set; } = string.Empty;
        public string CourseOne { get; set; } = string.Empty;
        public string CourseTwo { get; set; } = string.Empty;
        public string CourseThree { get; set; } = string.Empty;

        public string coachId { get; set; }
        public List<SelectListItem> Coaches { get; set; } = new List<SelectListItem>();

        // <-- ADD THIS
        public List<StudentAthleteProfileModel> StudentListPending { get; set; } = new List<StudentAthleteProfileModel>();
        public List<StudentAthleteProfileModel> StudentListApproved { get; set; } = new List<StudentAthleteProfileModel>();
    }
}


