using System.Text.Json.Serialization;
using Swashbuckle.AspNetCore.Swagger;

namespace server.Models
{
    public enum StatusEnum
    {
        PLANNED,
        PROGRESS,
        PENDING_COMPLETION,
        COMPLETED
    };

    public enum PriorityEnum
    {
        LOW,
        MEDIUM,
        HIGH
    }

    public class ProjectModel
    {
        [JsonIgnore]
        public string Id { get; set; }
        public string Name { get; set; }
        public string Description { get; set; }
        public DateTime CreatedDate { get; set; }
        public DateTime? EndDate { get; set; }
        public List<SprintModel>? SprintList { get; set; } = null;
        public List<UserInfo>? Members { get; set; } = null;
    }

    public class SprintModel
    {
        [JsonIgnore]
        public string Id { get; set; }
        public string Name { get; set; }
        public string Description { get; set; }
        public DateTime StartDate { get; set; }
        public DateTime EndDate { get; set; }
        public StatusEnum SprintStatus { get; set; } = StatusEnum.PLANNED;
        public PriorityEnum Priority { get; set; } = PriorityEnum.MEDIUM;
        public List<TaskModel>? TaskList { get; set; } = new List<TaskModel>();
    }

    public class TaskModel
    {
        [JsonIgnore]
        public string Id { get; set; }
        public string Name { get; set; }
        public string Description { get; set; }
        public StatusEnum TaskStatus { get; set; } = StatusEnum.PLANNED;
        public PriorityEnum Priority { get; set; } = PriorityEnum.LOW;
        public List<UserInfo> Assignees { get; set; } = new List<UserInfo>();
        public List<TaskLogModel>? LogList { get; set; } = new List<TaskLogModel>();
        public List<string>? Comments { get; set; } = new List<string>();
    }

    public class TaskLogModel
    {
        [JsonIgnore]
        public string Id { get; set; }
        public string UserUid { get; set; }
        public DateTime StartDate { get; set; }
        public DateTime EndDate { get; set; }
        public string Comment { get; set; } = "";
        public StatusEnum Status { get; set; } = StatusEnum.PROGRESS;
        public UserInfo User { get; set; }
    }
}
