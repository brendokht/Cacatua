using System.ComponentModel.DataAnnotations.Schema;
using System.Text.Json.Serialization;

namespace server.Models
{
    public class deprecated_SprintModel
    {

        [JsonPropertyName("sprintName")]
        public string SprintName { get; set; } = string.Empty;

        [JsonPropertyName("sprintDescription")]
        public string SprintDescription { get; set; } = string.Empty;

        [JsonPropertyName("sprintStartDate")]
        public DateTime SprintStartDate { get; set; }

        [JsonPropertyName("sprintEndDate")]
        public DateTime SprintEndDate { get; set; }

        [JsonPropertyName("sprintStatus")]
        public string SprintStatus { get; set; } = string.Empty;

        [JsonPropertyName("sprintTasks")]
        public List<string> SprintTasks { get; set; } = new List<string>();
        [JsonPropertyName("sprintFinished")]
        public Boolean SprintFinished{ get; set; } = false;
    }
}
