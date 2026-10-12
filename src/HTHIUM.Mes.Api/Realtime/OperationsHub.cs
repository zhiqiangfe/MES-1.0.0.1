using Microsoft.AspNetCore.SignalR;
using Microsoft.AspNetCore.Authorization;

namespace HTHIUM.Mes.Api.Realtime;

[Authorize]
public sealed class OperationsHub : Hub
{
}
