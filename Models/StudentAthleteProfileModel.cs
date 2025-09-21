using Microsoft.AspNetCore.Mvc.Rendering;
using System.ComponentModel.DataAnnotations;

namespace FITNSS.Models
{
    public class StudentAthleteProfileModel
    {
        public string userId { get; set; }

        public string studentAthleteProfileId { get; set; }

        public string Photo { get; set; } = "";
        public string ProfileImagePath { get; set; } = "";

        public string CourseOne { get; set; } = string.Empty;
        public string CourseTwo { get; set; } = string.Empty;
        public string CourseThree { get; set; } = string.Empty;
        public string ElementarySchool { get; set; } = string.Empty;
        public string ElementaryYearGraduated { get; set; } = string.Empty;
        public string SecondarySchool { get; set; } = string.Empty;
        public string SecondaryYearGraduated { get; set; } = string.Empty;
        public string SeniorHighSchool { get; set; } = string.Empty;
        public string SeniorHighYearGraduated { get; set; } = string.Empty;
        public string ShsTrackOrStrand { get; set; } = string.Empty;
        public string GTenGwa { get; set; } = string.Empty;
        public string GElevenGwa { get; set; } = string.Empty;
        public string GTwelveGwa { get; set; } = string.Empty;
        public string TransferStatus { get; set; } = string.Empty;
        public string PrevProgramOrCourse { get; set; } = string.Empty;
        public string Vaccination { get; set; } = string.Empty;
        public string PhilhealthNumber { get; set; } = string.Empty;
        public string Event { get; set; } = string.Empty;
        public string HomeAddress { get; set; } = string.Empty;
        public string ProvincialAddress { get; set; } = string.Empty;
        public string Portfolio { get; set; } = "";
        public string FullName { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public string ContactNumber { get; set; } = string.Empty;
        //Orig Code
        //public string DateOfBirth { get; set; } = string.Empty;

        //NEW! Replace
        [DataType(DataType.Date)]
        public DateTime? DateOfBirth { get; set; }
        //END OF NEW
        public string Age { get; set; } = string.Empty;
        public string Height { get; set; } = string.Empty;
        public string Weight { get; set; } = string.Empty;
        public string Course { get; set; } = string.Empty;
        public string YearLevel { get; set; } = string.Empty;
        public string EmergencyContact { get; set; } = string.Empty;
        public string Sport { get; set; } = string.Empty;
        public string Status { get; set; } = string.Empty;


        public string coachId { get; set; }
        public List<SelectListItem> Coaches { get; set; } = new List<SelectListItem>();

        public List<StudentSportsParticipationModel> Participations { get; set; } = new();




    }

    public class StudentSportsParticipationModel
    {
        public string Name { get; set; }
        public string Level { get; set; }
        public string Year { get; set; }
        public string Award { get; set; }
    }
}
