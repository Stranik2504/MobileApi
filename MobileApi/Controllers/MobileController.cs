using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using Database;
using Microsoft.AspNetCore.Mvc;
using MobileApi.Models;
using MobileApi.Utils;
using Newtonsoft.Json;

namespace MobileApi.Controllers;

[ApiController]
[Route("mobile/api/v1/")]
public class MobileController(
    ILogger<MobileController> logger,
    IDatabase database
) : ControllerBase
{
    private readonly ILogger<MobileController> _logger = logger;
    private readonly IDatabase _database = database;

    [HttpGet("ping")]
    public Task<IActionResult> Get()
    {
        return Task.FromResult<IActionResult>(Ok("Pong"));
    }

    [HttpGet("tasks")]
    public async Task<ActionResult<List<SolutionList>>> GetTasks(
        [FromQuery] int userId,
        [FromHeader] string? token
    )
    {
        _logger.LogInformation("[MobileController]: GetTasks start");

        var list = new List<SolutionList>();

        await foreach (var item in _database.GetAllRecords("tasks"))
        {
            if (string.IsNullOrWhiteSpace(item.Id))
                continue;

            var forAll = item.Fields.GetInt("for_all");

            if (forAll != 1)
                continue;

            var task = new SolutionList()
            {
                Id = item.Id.ToInt(),
                Title = item.Fields.GetString("title"),
                Description = item.Fields.GetString("description"),
            };
            list.Add(task);
        }

        if (userId <= 0)
        {
            return Ok(list);
        }

        if (string.IsNullOrEmpty(token))
        {
            _logger.LogWarning("[MobileController]: Token is null or empty");
            return BadRequest("Token is null or empty");
        }

        var user = await GetUser(userId, token);

        if (user == null)
        {
            _logger.LogWarning("[MobileController]: User not found or invalid");
            return NotFound("User isn't found or invalid");
        }

        await foreach (var item in _database.GetAllRecordsByField("task_user", "user_id", userId))
        {
            if (string.IsNullOrWhiteSpace(item.Id))
                continue;

            if (!item.Fields.ContainsKey("task_id"))
                continue;

            var taskId = item.Fields.GetString("task_id");
            var taskRecord = await _database.GetRecordById("tasks", taskId);

            var task = new SolutionList()
            {
                Id = taskRecord.Id.ToInt(),
                Title = taskRecord.Fields.GetString("title"),
                Description = taskRecord.Fields.GetString("description"),
            };

            if (list.Any(x => x.Id == task.Id))
            {
                _logger.LogInformation("[MobileController]: Task with Id {TaskId} already exists in the list", task.Id);
                continue; // Skip if task already exists in the list
            }
            list.Add(task);
        }

        _logger.LogInformation("[MobileController]: GetTasks end and return result");
        return Ok(list);
    }

    [HttpGet("solution-tasks")]
    public async Task<ActionResult<List<SolutionList>>> GetSolutionTasks(
        [FromQuery] int userId,
        [FromHeader] string? token
    )
    {
        _logger.LogInformation("[MobileController]: GetSolutionTasks start");

        if (userId <= 0)
        {
            return Ok(new List<SolutionList>());
        }

        if (string.IsNullOrEmpty(token))
        {
            _logger.LogWarning("[MobileController]: Token is null or empty");
            return BadRequest("Token is null or empty");
        }

        var user = await GetUser(userId, token);

        if (user == null)
        {
            _logger.LogWarning("[MobileController]: User not found or invalid");
            return NotFound("User isn't found or invalid");
        }

        var list = new List<SolutionList>();

        await foreach (var item in _database.GetAllRecordsByField("user_solution", "user_id", userId.ToString()))
        {
            if (string.IsNullOrWhiteSpace(item.Id))
                continue;

            if (!item.Fields.ContainsKey("task_id"))
                continue;

            var taskId = item.Fields.GetInt("task_id");

            if (taskId <= 0)
                continue;

            var taskRecord = await _database.GetRecordById("tasks", taskId.ToString());

            if (string.IsNullOrWhiteSpace(taskRecord.Id))
                continue;

            var task = new SolutionList()
            {
                Id = taskRecord.Id.ToInt(),
                Title = taskRecord.Fields.GetString("title"),
                Description = taskRecord.Fields.GetString("description"),
                FullDescription = taskRecord.Fields.GetString("full_description"),
                Solutions = JsonConvert.DeserializeObject<List<bool>>(item.Fields.GetString("solution")) ?? []
            };

            list.Add(task);
        }

        _logger.LogInformation("[MobileController]: GetSolutionTasks end and return result");
        return Ok(list);
    }

    [HttpPost("login")]
    public async Task<ActionResult<LoginResponse>> Login(
        [FromBody] LoginRequest request
    )
    {
        _logger.LogInformation("[MobileController]: Login start");

        var user = await GetUser(request.Username, request.Password);

        if (user == null)
        {
            _logger.LogWarning("[MobileController]: User not found or invalid");
            return NotFound(new LoginResponse()
            {
                Message = "User isn't found or invalid"
            });
        }

        var response = new LoginResponse()
        {
            Id = user.Id,
            IsAdmin = user.IsAdmin,
            Token = user.Token,
            Message = "Ok"
        };

        _logger.LogInformation("[MobileController]: Login end and return user");
        return Ok(response);
    }

    [HttpPost("register")]
    public async Task<ActionResult<LoginResponse>> Register(
        [FromBody] LoginRequest request
    )
    {
        _logger.LogInformation("[MobileController]: Register start");

        var user = await GetUser(request.Username, request.Password);

        if (user != null)
        {
            _logger.LogWarning("[MobileController]: User not found or invalid");
            return NotFound(new LoginResponse()
            {
                Message = "User already exists"
            });
        }

        var newUser = new FullUser()
        {
            Username = request.Username,
            Password = request.Password,
            PassCount = 0,
            Count = 0,
            ErrorCount = 0,
            CountTasks = 0,
            IsAdmin = false,
            Token = GenToken()
        };

        var result = await _database.Create(
            "users",
            new Dictionary<string, object?>
            {
                { "username", newUser.Username },
                { "password", newUser.Password },
                { "pass_count", 0 },
                { "count", 0 },
                { "error_count", 0 },
                { "count_tasks", 0 },
                { "is_admin", false },
                { "token", newUser.Token }
            }
        );

        if (!result.Success)
        {
            _logger.LogError("[MobileController]: Failed to create user");
            return BadRequest(new LoginResponse()
            {
                Message = "Failed to create user"
            });
        }

        var response = new LoginResponse()
        {
            Id = result.Id.ToInt(),
            IsAdmin = newUser.IsAdmin,
            Token = newUser.Token,
            Message = "Ok"
        };

        _logger.LogInformation("[MobileController]: Register end and return user");
        return Ok(response);
    }

    [HttpGet("user/info")]
    public async Task<ActionResult<UserInfo>> GetUserInfo(
        [FromHeader] int userId,
        [FromHeader] string? token
    )
    {
        _logger.LogInformation("[MobileController]: GetUserInfo start");

        if (userId <= 0)
        {
            _logger.LogWarning("[MobileController]: UserId is less than or equal to zero");
            return BadRequest("UserId is less than or equal to zero");
        }

        if (string.IsNullOrEmpty(token))
        {
            _logger.LogWarning("[MobileController]: Token is null or empty");
            return BadRequest("Token is null or empty");
        }

        var user = await GetUser(userId, token);

        if (user == null)
        {
            _logger.LogWarning("[MobileController]: User not found or invalid");
            return NotFound("User isn't found or invalid");
        }

        var userInfo = new UserInfo()
        {
            Username = user.Username,
            StatPass = user.Count > 0 ? user.PassCount / user.Count * 100 : 100,
            StatError = user.CountTasks > 0 ? user.ErrorCount / user.CountTasks * 100 : 0
        };

        _logger.LogInformation("[MobileController]: GetUserInfo end and return user info");
        return Ok(userInfo);
    }

    [HttpGet("solutions/{solutionId:int}")]
    public async Task<ActionResult<SolutionInfo>> GetSolutionInfo(
        [FromRoute] int solutionId,
        [FromHeader] int userId,
        [FromHeader] string? token
    )
    {
        _logger.LogInformation("[MobileController]: GetSolution start");

        if (solutionId <= 0)
        {
            _logger.LogWarning("[MobileController]: SolutionId is less than or equal to zero");
            return BadRequest("SolutionId is less than or equal to zero");
        }

        if (userId <= 0)
        {
            _logger.LogWarning("[MobileController]: UserId is less than or equal to zero");
            return BadRequest("UserId is less than or equal to zero");
        }

        if (string.IsNullOrEmpty(token))
        {
            _logger.LogWarning("[MobileController]: Token is null or empty");
            return BadRequest("Token is null or empty");
        }

        var user = await GetUser(userId, token);

        if (user == null)
        {
            _logger.LogWarning("[MobileController]: User not found or invalid");
            return NotFound("User isn't found or invalid");
        }

        var record = await _database.GetRecord("tasks", "id", solutionId.ToString());

        if (string.IsNullOrWhiteSpace(record.Id))
        {
            _logger.LogWarning("[MobileController]: Solution not found");
            return NotFound("Solution not found");
        }

        var taskRecord = await _database.GetRecord("task_user",
            new SearchField(userId.ToString(), "user_id", con: Connection.AND),
            new SearchField(solutionId.ToString(), "task_id")
        );

        if (string.IsNullOrWhiteSpace(taskRecord.Id) && record.Fields.GetInt("for_all") != 1)
        {
            _logger.LogWarning("[MobileController]: Task not found for solution");
            return NotFound("Task not found for solution");
        }

        var solutionInfo = new SolutionInfo()
        {
            Id = record.Id.ToInt(),
            Title = record.Fields.GetString("title"),
            Description = record.Fields.GetString("full_description"),
            CountAttempts = record.Fields.GetInt("count_attempts"),
            CountUserAttempts = string.IsNullOrWhiteSpace(taskRecord.Id) ? -1 : taskRecord.Fields.GetInt("count_attempts")
        };

        _logger.LogInformation("[MobileController]: GetSolution end and return solution info");

        return Ok(solutionInfo);
    }

    [HttpGet("users")]
    public async Task<ActionResult<List<User>>> GetUsers(
        [FromHeader] int userId,
        [FromHeader] string? token
    )
    {
        _logger.LogInformation("[MobileController]: GetUsers start");

        if (userId <= 0)
        {
            _logger.LogWarning("[MobileController]: UserId is less than or equal to zero");
            return BadRequest("UserId is less than or equal to zero");
        }

        if (string.IsNullOrEmpty(token))
        {
            _logger.LogWarning("[MobileController]: Token is null or empty");
            return BadRequest("Token is null or empty");
        }

        var user = await GetUser(userId, token);

        if (user is not { IsAdmin: true })
        {
            _logger.LogWarning("[MobileController]: User not found or not admin");
            return NotFound("User isn't found or not admin");
        }

        var users = new List<User>();

        await foreach (var item in _database.GetAllRecords("users"))
        {
            if (string.IsNullOrWhiteSpace(item.Id))
                continue;

            var u = new User()
            {
                Id = item.Id.ToInt(),
                Username = item.Fields.GetString("username")
            };

            users.Add(u);
        }

        _logger.LogInformation("[MobileController]: GetUsers end and return users list");
        return Ok(users);
    }

    [HttpGet("solutions/{solutionId:int}/tasks")]
    public async Task<ActionResult<List<TaskItem>>> GetTaskItems(
        [FromRoute] int solutionId,
        [FromHeader] int userId,
        [FromHeader] string? token
    )
    {
        _logger.LogInformation("[MobileController]: GetTaskItems start");

        if (solutionId <= 0)
        {
            _logger.LogWarning("[MobileController]: SolutionId is less than or equal to zero");
            return BadRequest("SolutionId is less than or equal to zero");
        }

        if (userId <= 0)
        {
            _logger.LogWarning("[MobileController]: UserId is less than or equal to zero");
            return BadRequest("UserId is less than or equal to zero");
        }

        if (string.IsNullOrEmpty(token))
        {
            _logger.LogWarning("[MobileController]: Token is null or empty");
            return BadRequest("Token is null or empty");
        }

        var user = await GetUser(userId, token);

        if (user == null)
        {
            _logger.LogWarning("[MobileController]: User not found or invalid");
            return NotFound("User isn't found or invalid");
        }

        var taskRecord = await _database.GetRecord("task_user",
            new SearchField(userId.ToString(), "user_id", con: Connection.AND),
            new SearchField(solutionId.ToString(), "task_id")
        );

        var record = await _database.GetRecord("tasks", "id", solutionId.ToString());

        if (string.IsNullOrWhiteSpace(taskRecord.Id) && record.Fields.GetInt("for_all") != 1)
        {
            _logger.LogWarning("[MobileController]: Task not found for solution");
            return NotFound("Task not found for solution");
        }

        var taskItems = new List<TaskItem>();

        await foreach (var item in _database.GetAllRecordsByField("task_items", "task_id", solutionId.ToString()))
        {
            if (string.IsNullOrWhiteSpace(item.Id))
                continue;

            var options = JsonConvert.DeserializeObject<List<AnswerOption>>(item.Fields.GetString("options")) ?? [];

            var taskItem = new TaskItem()
            {
                Question = item.Fields.GetString("question"),
                AnswerOptions = options.Select(x => { x.IsCorrect = false; return x; }).ToList()
            };

            taskItems.Add(taskItem);
        }

        _logger.LogInformation("[MobileController]: GetTaskItems end and return task items list");
        return Ok(taskItems);
    }

    [HttpGet("solutions/{solutionId:int}/users")]
    public async Task<ActionResult<List<UserSolution>>> GetSolutionUsers(
        [FromRoute] int solutionId,
        [FromHeader] int userId,
        [FromHeader] string? token
    )
    {
        _logger.LogInformation("[MobileController]: GetSolutionUsers start");

        if (solutionId <= 0)
        {
            _logger.LogWarning("[MobileController]: SolutionId is less than or equal to zero");
            return BadRequest("SolutionId is less than or equal to zero");
        }

        if (userId <= 0)
        {
            _logger.LogWarning("[MobileController]: UserId is less than or equal to zero");
            return BadRequest("UserId is less than or equal to zero");
        }

        if (string.IsNullOrEmpty(token))
        {
            _logger.LogWarning("[MobileController]: Token is null or empty");
            return BadRequest("Token is null or empty");
        }

        var user = await GetUser(userId, token);

        if (user is not { IsAdmin: true })
        {
            _logger.LogWarning("[MobileController]: User not found or not admin");
            return NotFound("User isn't found or not admin");
        }

        var userSolutions = new List<UserSolution>();

        await foreach (var item in _database.GetAllRecordsByField("user_solution", "task_id", solutionId.ToString()))
        {
            if (string.IsNullOrWhiteSpace(item.Id))
                continue;

            var userIdField = item.Fields.GetInt("user_id");

            var userRecord = await _database.GetRecord("users", "id", userIdField.ToString());

            if (string.IsNullOrWhiteSpace(userRecord.Id))
                continue;

            var solution = new UserSolution()
            {
                Id = userRecord.Id.ToInt(),
                Username = userRecord.Fields.GetString("username"),
                IsAdmin = userRecord.Fields.GetInt("is_admin") == 1,
                Solutions = JsonConvert.DeserializeObject<List<bool>>(item.Fields.GetString("solution")) ?? []
            };

            userSolutions.Add(solution);
        }

        _logger.LogInformation("[MobileController]: GetSolutionUsers end and return users list");
        return Ok(userSolutions);
    }

    [HttpPost("solutions/{solutionId:int}/submit")]
    public async Task<ActionResult<bool>> SubmitAnswers(
        [FromBody] AnswersRequest request,
        [FromRoute] int solutionId,
        [FromHeader] int userId,
        [FromHeader] string? token
    )
    {
        _logger.LogInformation("[MobileController]: SubmitAnswers start");

        if (solutionId <= 0)
        {
            _logger.LogWarning("[MobileController]: SolutionId is less than or equal to zero");
            return BadRequest("SolutionId is less than or equal to zero");
        }

        if (userId <= 0)
        {
            _logger.LogWarning("[MobileController]: UserId is less than or equal to zero");
            return BadRequest("UserId is less than or equal to zero");
        }

        if (string.IsNullOrEmpty(token))
        {
            _logger.LogWarning("[MobileController]: Token is null or empty");
            return BadRequest("Token is null or empty");
        }

        var user = await GetUser(userId, token);

        if (user == null)
        {
            _logger.LogWarning("[MobileController]: User not found or invalid");
            return NotFound("User isn't found or invalid");
        }

        // task_items_ids
        var record = await _database.GetRecordById("tasks", solutionId.ToString());
        var taskIds = JsonConvert.DeserializeObject<List<int>>(record.Fields.GetString("task_items_ids")) ?? [];
        var solution = new List<bool>(taskIds.Count);
        var countCorrect = 0;
        var i = 0;

        if (request.Answers.Count != taskIds.Count)
        {
            _logger.LogWarning("[MobileController]: Answers count does not match options count");
            return BadRequest("Answers count does not match options count");
        }

        foreach (var taskId in taskIds)
        {
            var taskItemRecord = await _database.GetRecord("task_items", "id", taskId.ToString());

            if (string.IsNullOrWhiteSpace(taskItemRecord.Id))
            {
                _logger.LogWarning("[MobileController]: Task item not found for taskId: {TaskId}", taskId);
                return NotFound($"Task item not found for taskId: {taskId}");
            }

            var options = JsonConvert.DeserializeObject<List<AnswerOption>>(taskItemRecord.Fields.GetString("options")) ?? [];

            if (request.Answers[i] < 0 || request.Answers[i] >= options.Count)
            {
                _logger.LogWarning("[MobileController]: Answer index out of range for taskId: {TaskId}", taskId);
                return BadRequest($"Answer index out of range for taskId: {taskId}");
            }

            var isCorrect = options[request.Answers[i]].IsCorrect;

            if (isCorrect)
                countCorrect++;

            solution.Add(isCorrect);
            i++;
        }


        var result = await _database.Create(
            "user_solution",
            new Dictionary<string, object?>
            {
                { "user_id", user.Id },
                { "solution", JsonConvert.SerializeObject(solution) },
                { "task_id", solutionId }
            }
        );

        if (!result.Success)
        {
            _logger.LogError("[MobileController]: Failed to submit answers");
            return BadRequest(false);
        }

        // Update user statistics
        var res = await _database.Update(
            "users",
            userId.ToString(),
            new Dictionary<string, object>
            {
                { "pass_count", user.PassCount + (countCorrect > taskIds.Count / 2 ? 1 : 0) },
                { "count", user.Count + 1 },
                { "error_count", user.ErrorCount + (taskIds.Count - countCorrect) },
                { "count_tasks", user.CountTasks + taskIds.Count }
            }
        );

        if (!res)
        {
            _logger.LogError("[MobileController]: Failed to submit answers");
            return BadRequest(false);
        }

        var taskUserRecord = await _database.GetRecord("task_user",
            new SearchField(userId.ToString(), "user_id", con: Connection.AND),
            new SearchField(solutionId.ToString(), "task_id")
        );

        if (string.IsNullOrWhiteSpace(taskUserRecord.Id))
        {
            // Create new task_user record if it doesn't exist
            await _database.Create(
                "task_user",
                new Dictionary<string, object?>
                {
                    { "user_id", userId },
                    { "task_id", solutionId },
                    { "count_attempts", 1 }
                }
            );
        }
        else
        {
            // Update existing task_user record
            await _database.Update(
                "task_user",
                taskUserRecord.Id,
                new Dictionary<string, object>
                {
                    { "count_attempts", taskUserRecord.Fields.GetInt("count_attempts") + 1 }
                }
            );
        }

        _logger.LogInformation("[MobileController]: SubmitAnswers end and return success");
        return Ok(true);
    }

    [HttpDelete("solutions/{solutionId:int}/delete")]
    public async Task<ActionResult<bool>> DeleteSolution(
        [FromRoute] int solutionId,
        [FromHeader] int userId,
        [FromHeader] string? token
    )
    {
        _logger.LogInformation("[MobileController]: DeleteSolution start");

        if (solutionId <= 0)
        {
            _logger.LogWarning("[MobileController]: SolutionId is less than or equal to zero");
            return BadRequest("SolutionId is less than or equal to zero");
        }

        if (userId <= 0)
        {
            _logger.LogWarning("[MobileController]: UserId is less than or equal to zero");
            return BadRequest("UserId is less than or equal to zero");
        }

        if (string.IsNullOrEmpty(token))
        {
            _logger.LogWarning("[MobileController]: Token is null or empty");
            return BadRequest("Token is null or empty");
        }

        var user = await GetUser(userId, token);

        if (user is not { IsAdmin: true })
        {
            _logger.LogWarning("[MobileController]: User not found or not admin");
            return NotFound("User isn't found or not admin");
        }

        await _database.DeleteByField("task_items", "task_id", solutionId.ToString());
        await _database.DeleteByField("user_solution", "task_id", solutionId.ToString());
        await _database.DeleteByField("task_user", "task_id", solutionId.ToString());

        var res = await _database.Delete("tasks", solutionId.ToString());

        if (!res)
        {
            _logger.LogError("[MobileController]: Failed to delete solution");
            return BadRequest(false);
        }

        _logger.LogInformation("[MobileController]: DeleteSolution end and return success");
        return Ok(true);
    }

    [HttpPost("tasks/create")]
    public async Task<ActionResult<bool>> CreateTask(
        [FromBody] CreateTaskRequest request,
        [FromHeader] int userId,
        [FromHeader] string? token
    )
    {
        _logger.LogInformation("[MobileController]: CreateTask start");

        if (userId <= 0)
        {
            _logger.LogWarning("[MobileController]: UserId is less than or equal to zero");
            return BadRequest("UserId is less than or equal to zero");
        }

        if (string.IsNullOrEmpty(token))
        {
            _logger.LogWarning("[MobileController]: Token is null or empty");
            return BadRequest("Token is null or empty");
        }

        var user = await GetUser(userId, token);

        if (user is not { IsAdmin: true })
        {
            _logger.LogWarning("[MobileController]: User not found or not admin");
            return NotFound("User isn't found or not admin");
        }

        var result = await _database.Create(
            "tasks",
            new Dictionary<string, object?>
            {
                { "title", request.SolutionList.Title },
                { "description", request.SolutionList.Description },
                { "full_description", request.SolutionList.FullDescription },
                { "count_attempts", -1 },
                { "for_all", request.Users.Count == 0 ? 1 : 0 },
                { "task_items_ids", "" },
            }
        );

        if (!result.Success)
        {
            _logger.LogError("[MobileController]: Failed to create task");
            return BadRequest(false);
        }

        var taskItemsIds = new List<int>();

        foreach (var item in request.Tasks)
        {
            var optionsJson = JsonConvert.SerializeObject(item.AnswerOptions);
            var taskItemResult = await _database.Create(
                "task_items",
                new Dictionary<string, object?>
                {
                    { "question", item.Question },
                    { "options", optionsJson },
                    { "task_id", result.Id }
                }
            );

            if (!taskItemResult.Success)
            {
                _logger.LogError("[MobileController]: Failed to create task item");
                return BadRequest(false);
            }

            taskItemsIds.Add(taskItemResult.Id.ToInt());
        }

        var taskItemsIdsJson = JsonConvert.SerializeObject(taskItemsIds);

        var res = await _database.Update(
            "tasks",
            result.Id.ToString(),
            new Dictionary<string, object>
            {
                { "task_items_ids", taskItemsIdsJson }
            }
        );

        if (!res)
        {
            _logger.LogError("[MobileController]: Failed to create task");
            return BadRequest(false);
        }

        await foreach (var u in _database.GetAllRecordsByField("users", "is_admin", "1"))
        {
            if (string.IsNullOrWhiteSpace(u.Id))
                continue;

            request.Users.Add(new User() { Id = u.Id.ToInt(), Username = u.Fields.GetString("username") });
        }

        if (request.Users.Count > 0)
        {
            foreach (var userIdToAdd in request.Users)
            {
                var taskUserResult = await _database.Create(
                    "task_user",
                    new Dictionary<string, object?>
                    {
                        { "user_id", userIdToAdd.Id },
                        { "task_id", result.Id },
                        { "count_attempts", 0 }
                    }
                );

                if (!taskUserResult.Success)
                {
                    _logger.LogError("[MobileController]: Failed to create task user record for userId: {UserId}", userIdToAdd);
                    return BadRequest(false);
                }
            }
        }

        _logger.LogInformation("[MobileController]: CreateTask end and return success");
        return Ok(true);
    }

    private async Task<FullUser?> GetUser(int userId, string? token)
    {
        if (userId <= 0)
        {
            _logger.LogWarning("[MobileController]: UserId is less than or equal to zero");
            return null;
        }

        if (string.IsNullOrEmpty(token))
        {
            _logger.LogWarning("[MobileController]: Token is null or empty");
            return null;
        }

        var record = await _database.GetRecord("users", new SearchField(userId.ToString(), "id", con: Connection.AND), new SearchField(token, "token"));

        if (string.IsNullOrWhiteSpace(record.Id))
        {
            _logger.LogWarning("[MobileController]: User not found");
            return null;
        }

        var user = new FullUser()
        {
            Id = record.Id.ToInt(),
            IsAdmin = record.Fields.GetInt("is_admin") == 1,
            Username = record.Fields.GetString("username"),
            Password = record.Fields.GetString("password"),
            PassCount = record.Fields.GetInt("pass_count"),
            Count = record.Fields.GetInt("count"),
            ErrorCount = record.Fields.GetInt("error_count"),
            CountTasks = record.Fields.GetInt("count_tasks"),
            Token = record.Fields.GetString("token"),
        };

        return user;
    }

    private async Task<FullUser?> GetUser(string username, string password)
    {
        if (string.IsNullOrWhiteSpace(username))
        {
            _logger.LogWarning("[MobileController]: Username is null or empty");
            return null;
        }

        if (string.IsNullOrEmpty(password))
        {
            _logger.LogWarning("[MobileController]: Token is null or empty");
            return null;
        }

        var record = await _database.GetRecord("users", new SearchField(username, "username", con: Connection.AND), new SearchField(password, "password"));

        if (string.IsNullOrWhiteSpace(record.Id))
        {
            _logger.LogWarning("[MobileController]: User not found");
            return null;
        }

        var user = new FullUser()
        {
            Id = record.Id.ToInt(),
            IsAdmin = record.Fields.GetInt("is_admin") == 1,
            Username = record.Fields.GetString("username"),
            Password = record.Fields.GetString("password"),
            PassCount = record.Fields.GetInt("pass_count"),
            Count = record.Fields.GetInt("count"),
            ErrorCount = record.Fields.GetInt("error_count"),
            CountTasks = record.Fields.GetInt("count_tasks"),
            Token = record.Fields.GetString("token"),
        };

        return user;
    }

    private static string GenToken()
    {
        const int tokenLength = 64;
        const string chars = "0123456789abcdefghijklmnopqrstuvwxyzABCDEFGHIJKLMNOPQRSTUVWXYZ_";

        var tokenChars = new char[tokenLength];
        var randomBytes = new byte[tokenLength];


        using var rng = RandomNumberGenerator.Create();
        rng.GetBytes(randomBytes);

        for (var i = 0; i < tokenLength; i++)
            tokenChars[i] = chars[randomBytes[i] % chars.Length];

        return new string(tokenChars);
    }
}