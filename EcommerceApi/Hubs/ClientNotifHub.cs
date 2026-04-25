using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;
using System.Security.Claims;

namespace EcommerceApi.Hubs;

[Authorize(Roles = "Client")]
public class ClientNotifHub : Hub
{
    private string? GetClientId()
    {
        return Context.User?.FindFirst("clientId")?.Value
            ?? Context.User?.FindFirst("ClientId")?.Value
            ?? Context.User?.FindFirst(ClaimTypes.NameIdentifier)?.Value
            ?? Context.User?.FindFirst("sub")?.Value;
    }

    public override async Task OnConnectedAsync()
    {
        var cid = GetClientId();
        if (!string.IsNullOrWhiteSpace(cid))
            await Groups.AddToGroupAsync(Context.ConnectionId, $"client:{cid}");

        await base.OnConnectedAsync();
    }

    public override async Task OnDisconnectedAsync(Exception? exception)
    {
        var cid = GetClientId();
        if (!string.IsNullOrWhiteSpace(cid))
            await Groups.RemoveFromGroupAsync(Context.ConnectionId, $"client:{cid}");

        await base.OnDisconnectedAsync(exception);
    }
}