namespace FITNSS.Models
{
    public class StudentAcademicMonitoringModel
    {
        public string userId { get; set; }
        public string Subject { get; set; }
        public string Grade { get; set; } = string.Empty;
        //NEW!!
        public string GradeFile { get; set; } = "";

        public string File { get; set; } = "";
        //NEW
        public decimal TotalKm { get; set; }
        public string KmTotalDays { get; set; }
        public decimal KmPercentage { get; set; }

        public string SleepTotalDays { get; set; }
        public decimal HoursPercentage { get; set; }

        public string CaloriesTotalDays { get; set; }
        public decimal CaloriesPercentage { get; set; }

    }
}
