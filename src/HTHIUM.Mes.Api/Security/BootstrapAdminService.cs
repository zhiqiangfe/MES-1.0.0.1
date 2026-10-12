using HTHIUM.Mes.Application.Identity;
using HTHIUM.Mes.Contracts.Identity;
using HTHIUM.Mes.Domain.Identity;
using Microsoft.Extensions.Options;

namespace HTHIUM.Mes.Api.Security;

public sealed class BootstrapAdminService
{
    private readonly IUserService _userService;
    private readonly BootstrapAdminOptions _options;
    private readonly ILogger<BootstrapAdminService> _logger;

    public BootstrapAdminService(
        IUserService userService,
        IOptions<BootstrapAdminOptions> options,
        ILogger<BootstrapAdminService> logger)
    {
        _userService = userService;
        _options = options.Value;
        _logger = logger;
    }

    public async Task InitializeAsync(CancellationToken cancellationToken = default)
    {
        if (await _userService.HasAnyUserAsync(cancellationToken)) return;
        if (string.IsNullOrWhiteSpace(_options.Password))
        {
            throw new InvalidOperationException(
                "数据库中没有账号，请通过BootstrapAdmin__Password环境变量设置首次管理员密码");
        }

        await _userService.CreateUserAsync(new CreateUserRequest
        {
            Username = _options.Username,
            DisplayName = _options.DisplayName,
            Password = _options.Password,
            Role = UserRoles.Administrator,
        }, cancellationToken);
        _logger.LogInformation("Created initial MES administrator account {Username}", _options.Username);
    }
}
