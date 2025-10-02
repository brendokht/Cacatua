using System.Text.Json.Serialization;
namespace server.Models
{
    public class MessageModel
    {
        public string Text { get; set; } = string.Empty;

        public string Uid { get; set; } = string.Empty;
        public string? ServerUid { get; set; } = string.Empty;
        public string ChatUid { get; set; } = string.Empty;
    }
}
