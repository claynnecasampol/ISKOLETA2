namespace FITNSS.Models
{
    public class StudentBmi
    {
        public int UserId { get; set; }
        public int Age { get; set; }
        public double Weight { get; set; }
        public double Height { get; set; }

        // ✅ Add these for calculated result
        public double? Bmi { get; set; }  // nullable kasi pwedeng wala pa
        public string BmiCategory { get; set; }
    }
}
