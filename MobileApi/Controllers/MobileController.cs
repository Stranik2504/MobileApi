using Database;
using Microsoft.AspNetCore.Mvc;
using MobileApi.Models;
using MobileApi.Utils;

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

            var userIds = item.Fields.GetString("user_ids");

            if (userIds.Replace(" ", "") != "{-1}")
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

        await foreach (var item in _database.GetAllRecords("tasks"))
        {
            if (string.IsNullOrWhiteSpace(item.Id))
                continue;

            if (!item.Fields.ContainsKey("user_ids"))
                continue;

            var userIds = item.Fields.GetString("user_ids");

            if (!userIds.Contains(" " + userId + " ") && !userIds.Contains(" " + userId + ","))
                continue;

            var task = new SolutionList()
            {
                Id = item.Id.ToInt(),
                Title = item.Fields.GetString("title"),
                Description = item.Fields.GetString("description"),
            };

            list.Add(task);
        }

        _logger.LogInformation("[MobileController]: GetTasks end and return result");
        return Ok(list);
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

        var record = await _database.GetRecord("users", new SearchField("id", userId.ToString()), new SearchField("token", token));

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
}