using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Newtonsoft.Json;
using server.Interfaces;
using server.Models;
using server.Services;
using System.Security.Claims;

namespace server.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    //[Authorize]
    public class SprintController : Controller
    {
        private readonly IConfiguration _configuration;
        private readonly IWebHostEnvironment _env;
        private readonly IEmailService _emailService;
        private readonly IFirebaseStorageService _firebaseStorageService;
        private readonly IFirestoreService _firestoreService;

        public SprintController(IWebHostEnvironment env, IConfiguration configuration, IEmailService emailService, IFirebaseStorageService firebaseStorageService, IFirestoreService firestoreService)
        {
            _env = env;
            _configuration = configuration;
            _emailService = emailService;
            _firebaseStorageService = firebaseStorageService;
            _firestoreService = firestoreService;
        }

        [HttpPost("create-project")]
        public async Task<IActionResult> CreateProject([FromForm] ProjectModel projectModel)
        {
            if (projectModel == null)
            {
                return BadRequest("Invalid project model.");
            }

            try
            {
                string currentUserUid = User.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? User.FindFirst("sub")?.Value;
                string projectId = Guid.NewGuid().ToString();

                projectModel.CreatedDate = DateTime.UtcNow;
                projectModel.Id = projectId;

                if (!string.IsNullOrEmpty(currentUserUid))
                {
                    var userInfo = await _firestoreService.GetUserFromDbByUidAsync(currentUserUid);
                    projectModel.Members = new List<UserInfo> { userInfo };
                }

                //var crud = new Crud();
                await Crud.SetDataAsync(projectId, projectModel);

                return Ok(new
                {
                    Message = "Project created successfully.",
                    ProjectId = projectId
                });
            }
            catch (Exception ex)
            {
                return StatusCode(500, $"Error creating project: {ex.Message}");
            }
        }

        [HttpDelete("delete-project")]
        public async void DeleteProject(string projectId)
        {
            try
            {
                //var crud = new Crud();
                await Crud.DeleteDataAsync(projectId);
            }
            catch (Exception)
            {
                Console.WriteLine("Failed DeleteTeam");
            }
        }

        [HttpGet("get-project")]
        public async Task<IActionResult> GetProject(string projectId)
        {
            try
            {
                //var crud = new Crud();
                var projectData = await Crud.GetDataAsync<ProjectModel>(projectId);

                if (projectData == null)
                {
                    return NotFound($"Project with ID {projectId} not found.");
                }

                return Ok(projectData);
            }
            catch (Exception ex)
            {
                return StatusCode(500, $"Error getting project: {ex.Message}");
            }
        }

        [HttpGet("get-all-projects")]
        public async Task<IActionResult> GetAllProjects()
        {
            try
            {
                //var crud = new Crud();
                var projectData = await Crud.GetDataAsync<Dictionary<string,ProjectModel>>("/");

                if (projectData == null)
                {
                    return NotFound("No projects found.");
                }

                return Ok(projectData);
            }
            catch (Exception ex)
            {
                return StatusCode(500, $"Error getting projects: {ex.Message}");
            }
        }



        [HttpPut("add-member")]
        public async Task<IActionResult> AddMember(string userUid, string projectId)
        {
            try
            {
                //var crud = new Crud();
                var projectData = await Crud.GetDataAsync<ProjectModel>(projectId);

                if (projectData == null)
                {
                    return NotFound($"Project with ID {projectId} not found.");
                }

                var userInfo = await _firestoreService.GetUserFromDbByUidAsync(userUid);
                projectData.Members ??= new List<UserInfo>();
                projectData.Members.Add(userInfo);

                await Crud.SetDataAsync(projectId, projectData);

                return Ok(new { Message = "Member added successfully." });
            }
            catch (Exception ex)
            {
                return StatusCode(500, $"Error adding member: {ex.Message}");
            }
        }

        [HttpPut("add-sprint")]
        public async Task<IActionResult> AddSprint([FromForm] SprintModel sprintModel, string projectId)
        {
            try
            {
                //var crud = new Crud();
                var projectData = await Crud.GetDataAsync<ProjectModel>(projectId);

                if (projectData == null)
                {
                    return NotFound($"Project with ID {projectId} not found.");
                }

                projectData.SprintList ??= new List<SprintModel>();
                sprintModel.Id = Guid.NewGuid().ToString();
                projectData.SprintList.Add(sprintModel);

                await Crud.SetDataAsync(projectId, projectData);

                return Ok(new { Message = "Sprint added successfully." });
            }
            catch (Exception ex)
            {
                return StatusCode(500, $"Error adding sprint: {ex.Message}");
            }
        }

        [HttpPut("add-task")]
        public async Task<IActionResult> AddTask([FromForm] TaskModel taskModel, string projectId, string sprintId)
        {
            try
            {
                //var crud = new Crud();
                var projectData = await Crud.GetDataAsync<ProjectModel>(projectId);

                if (projectData == null)
                {
                    return NotFound($"Project with ID {projectId} not found.");
                }

                var sprint = projectData.SprintList?.FirstOrDefault(s => s.Id == sprintId);
                if (sprint == null)
                {
                    return NotFound($"Sprint with ID {sprintId} not found in project {projectId}.");
                }

                sprint.TaskList ??= new List<TaskModel>();
                taskModel.Id = Guid.NewGuid().ToString();
                sprint.TaskList.Add(taskModel);

                //var crud = new Crud();
                await Crud.SetDataAsync(projectId, projectData);

                return Ok(new { Message = "Task added successfully." });
            }
            catch (Exception ex)
            {
                return StatusCode(500, $"Error adding task: {ex.Message}");
            }
        }

        [HttpPut("add-log")]
        public async Task<IActionResult> AddLog([FromForm] TaskLogModel taskLogModel, string projectId, string sprintId, string taskId)
        {
            try
            {
                //var crud = new Crud();
                var projectData = await Crud.GetDataAsync<ProjectModel>(projectId);

                if (projectData == null)
                {
                    return NotFound($"Project with ID {projectId} not found.");
                }

                var sprint = projectData.SprintList?.FirstOrDefault(s => s.Id == sprintId);
                if (sprint == null)
                {
                    return NotFound($"Sprint with ID {sprintId} not found in project {projectId}.");
                }

                var task = sprint.TaskList?.FirstOrDefault(t => t.Id == taskId);
                if (task == null)
                {
                    return NotFound($"Task with ID {taskId} not found in sprint {sprintId}.");
                }

                var userInfo = await _firestoreService.GetUserFromDbByUidAsync(taskLogModel.UserUid);
                if (userInfo == null)
                {
                    return NotFound($"User with ID {taskLogModel.UserUid} not found.");
                }

                task.LogList ??= new List<TaskLogModel>();
                taskLogModel.Id = Guid.NewGuid().ToString();
                taskLogModel.User = userInfo;
                task.LogList.Add(taskLogModel);


                //var crud = new Crud();
                await Crud.SetDataAsync(projectId, projectData);

                return Ok(new { Message = "Log added successfully." });
            }
            catch (Exception ex)
            {
                return StatusCode(500, $"Error adding log: {ex.Message}");
            }
        }

    }
}
