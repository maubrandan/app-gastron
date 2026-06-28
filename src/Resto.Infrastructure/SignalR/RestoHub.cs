using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;
using Resto.Infrastructure.Identity;

namespace Resto.Infrastructure.SignalR;

[Authorize]
public sealed class RestoHub : Hub<IRestoClient>
{
    public Task JoinKitchen()
    {
        if (!Context.User.HasAnyRole(AppRoles.Kitchen, AppRoles.Manager, AppRoles.Admin))
            throw new HubException("No tenés permiso para unirte al grupo de cocina.");

        return Groups.AddToGroupAsync(Context.ConnectionId, "Cocina");
    }

    public Task JoinSalon()
    {
        if (!Context.User.HasAnyRole(AppRoles.Waiter, AppRoles.Manager, AppRoles.Admin))
            throw new HubException("No tenés permiso para unirte al grupo de salón.");

        return Groups.AddToGroupAsync(Context.ConnectionId, "Salon");
    }
}
