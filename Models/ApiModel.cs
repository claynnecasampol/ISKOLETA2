using System.Text.Json.Serialization;

namespace FITNSS.Models
{
    public class ApiModel
    {
        
        [JsonPropertyName("userId")]

        public string userId { get; set; }

        [JsonPropertyName("HeartBeat")]
        public int HeartBeat { get; set; }

        [JsonPropertyName("Km")]
        public int Km { get; set; }

        [JsonPropertyName("Calories")]
        public int Calories { get; set; }

        [JsonPropertyName("Hours")]
        public int Hours { get; set; }

        public string Username { get; set; }
        public string Password { get; set; }
    }
}
