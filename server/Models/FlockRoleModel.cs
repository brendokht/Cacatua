namespace server.Models
{
    public class FlockRoleModel
    {
        public string flockId { get; set; }
        public string? roleId { get; set; }
        public string? roleName { get; set; }
        public string[]? userIds { get; set; }
    }
}
