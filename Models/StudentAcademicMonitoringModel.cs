namespace FITNSS.Models
{
    public class StudentAcademicMonitoringModel
    {
        public string userId { get; set; }

        public string Subject { get; set; }
        public string Grade { get; set; } = string.Empty;

        public string File { get; set; } = "";

    }
}
