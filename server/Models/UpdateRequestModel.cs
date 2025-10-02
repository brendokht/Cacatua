using System.Text.Json.Serialization;

namespace server.Models
{
    public class UpdateRequestModel
    {
        [JsonPropertyName("displayName")]
        public string DisplayName { get; set; } = string.Empty;

        [JsonPropertyName("email")]
        public string Email { get; set; } = string.Empty;
        [JsonPropertyName("photoUrl")]
        public string PhotoUrl { get; set; } = string.Empty;
    }
}
