using HTHIUM.Mes.Application.Identity;
using HTHIUM.Mes.Contracts.Common;
using HTHIUM.Mes.Contracts.Identity;
using HTHIUM.Mes.Domain.Identity;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace HTHIUM.Mes.Api.Controllers;

[ApiController]
[Authorize(Roles = UserRoles.Administrator)]
[Route("api/v1/admin/users")]
public sealed class AdminUsersController : ControllerBase
{
    private readonly IUserService _userService;

    public AdminUsersController(IUserService userService)
    {
        _userService = userService;
    }

    [HttpGet]
    public async Task<ActionResult<ApiResponse<IReadOnlyList<UserDto>>>> GetUsers(
        CancellationToken cancellationToken) =>
        Ok(Wrap(await _userService.GetUsersAsync(cancellationToken)));

    [HttpPost]
    public async Task<ActionResult<ApiResponse<UserDto>>> CreateUser(
        CreateUserRequest request,
        CancellationToken cancellationToken)
    {
        try
        {
            var user = await _userService.CreateUserAsync(request, cancellationToken);
            return Created($"/api/v1/admin/users/{user.Id}", Wrap(user));
        }
        catch (ArgumentException exception)
        {
            return BadRequest(Error("VALIDATION_FAILED", exception.Message));
        }
        catch (InvalidOperationException exception)
        {
            return Conflict(Error("USERNAME_EXISTS", exception.Message));
        }
    }

    [HttpDelete("{userId:guid}")]
    public async Task<ActionResult<ApiResponse<object>>> DeleteUser(
        Guid userId,
        CancellationToken cancellationToken)
    {
        if (!Guid.TryParse(User.FindFirstValue(ClaimTypes.NameIdentifier), out var currentUserId))
        {
            return Unauthorized(Error("INVALID_SESSION", "登录会话无效，请重新登录"));
        }

        var result = await _userService.DeleteUserAsync(
            userId,
            currentUserId,
            cancellationToken);
        return result switch
        {
            UserDeletionResult.Deleted => Ok(Wrap<object>(new { })),
            UserDeletionResult.NotFound => NotFound(Error("USER_NOT_FOUND", "账号不存在或已经被删除")),
            UserDeletionResult.CurrentUser => Conflict(Error("CURRENT_USER", "不能删除当前正在登录的账号")),
            UserDeletionResult.LastAdministrator => Conflict(Error("LAST_ADMINISTRATOR", "不能删除系统最后一个管理员")),
            _ => StatusCode(StatusCodes.Status500InternalServerError,
                Error("DELETE_FAILED", "账号删除失败")),
        };
    }

    private ApiResponse<T> Wrap<T>(T data) => new()
    {
        TraceId = HttpContext.TraceIdentifier,
        Data = data,
    };

    private ApiResponse<object> Error(string code, string message) => new()
    {
        Success = false,
        Code = code,
        Message = message,
        TraceId = HttpContext.TraceIdentifier,
    };
}
