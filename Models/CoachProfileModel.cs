namespace FITNSS.Models
{
    public class CoachProfileModel
    {
        public string userId { get; set; }
        public string ProfileImagePath { get; set; } = "/images/profile.png";

        public string FirstName { get; set; }
        public string LastName { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public string Course { get; set; } = string.Empty;
        public string YearLevel { get; set; } = string.Empty;
        public string ContactNumber { get; set; } = string.Empty;
        public string DateOfBirth { get; set; } = string.Empty;
        public string Age { get; set; } = string.Empty;
        public string Sport { get; set; } = string.Empty;
        public string Position { get; set; } = string.Empty;
        public List<string> ExpertiseList { get; set; } = new List<string>();


    }
}
