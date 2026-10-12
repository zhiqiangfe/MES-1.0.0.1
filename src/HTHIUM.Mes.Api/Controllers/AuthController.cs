using System.Security.Claims;
using HTHIUM.Mes.Application.Identity;
using HTHIUM.Mes.Contracts.Common;
using HTHIUM.Mes.Contracts.Identity;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace HTHIUM.Mes.Api.Controllers;

[ApiController]
[Route("api/v1/auth")]
public sealed class AuthController : ControllerBase
{
    private readonly IUserService _userService;

    public AuthController(IUserService userService)
    {
        _userService = userService;
    }

    [AllowAnonymous]
    [HttpPost("login")]
    public async Task<ActionResult<ApiResponse<CurrentUserDto>>> Login(
        LoginRequest request,
        CancellationToken cancellationToken)
    {
        var user = await _userService.AuthenticateAsync(
            request.Username,
            request.Password,
            cancellationToken);
        if (user is null)
        {
            return Unauthorized(new ApiResponse<object>
            {
                Success = false,
                Code = "INVALID_CREDENTIALS",
                Message = "用户名或密码错误",
                TraceId = HttpContext.TraceIdentifier,
            });
        }

        var claims = new[]
        {
            new Claim(ClaimTypes.NameIdentifier, user.Id.ToString("D")),
            new Claim(ClaimTypes.Name, user.Username),
            new Claim("display_name", user.DisplayName),
            new Claim(ClaimTypes.Role, user.Role),
        };
        var principal = new ClaimsPrincipal(
            new ClaimsIdentity(claims, CookieAuthenticationDefaults.AuthenticationScheme));
        await HttpContext.SignInAsync(
            CookieAuthenticationDefaults.AuthenticationScheme,
            principal,
            new AuthenticationProperties
            {
                IsPersistent = false,
                AllowRefresh = true,
                ExpiresUtc = DateTimeOffset.UtcNow.AddHours(8),
            });

        return Ok(Wrap(ToCurrentUser(user)));
    }

    [Authorize]
    [HttpGet("session")]
    public ActionResult<ApiResponse<CurrentUserDto>> GetSession() =>
        Ok(Wrap(CurrentUserFromClaims()));

    [Authorize]
    [HttpPost("logout")]
    public async Task<ActionResult<ApiResponse<object>>> Logout()
    {
        await HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
        return Ok(Wrap<object>(new { }));
    }

    private CurrentUserDto CurrentUserFromClaims() => new()
    {
        Username = User.Identity?.Name ?? string.Empty,
        DisplayName = User.FindFirstValue("display_name") ?? User.Identity?.Name ?? string.Empty,
        Role = User.FindFirstValue(ClaimTypes.Role) ?? string.Empty,
    };

    private static CurrentUserDto ToCurrentUser(UserDto user) => new()
    {
        Username = user.Username,
        DisplayName = user.DisplayName,
        Role = user.Role,
    };

    private ApiResponse<T> Wrap<T>(T data) => new()
    {
        TraceId = HttpContext.TraceIdentifier,
        Data = data,
    };
}
