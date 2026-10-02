using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;

namespace VigiaTrujillo.Hubs;

[AllowAnonymous]
public class VigiaHub : Hub
{
    public const string GrupoSupervisores = "Supervisores";
    public const string GrupoMunicipal = "Municipal";
    public const string GrupoTablero = "Tablero";
    public const string GrupoPortalPublico = "PortalPublico";

    public override async Task OnConnectedAsync()
    {
        await Groups.AddToGroupAsync(Context.ConnectionId, GrupoTablero);
        
        await Groups.AddToGroupAsync(Context.ConnectionId, GrupoPortalPublico);

        var user = Context.User;
        if (user?.Identity?.IsAuthenticated == true)
        {
            if (user.IsInRole("Supervisor") || user.IsInRole("Administrador"))
                await Groups.AddToGroupAsync(Context.ConnectionId, GrupoSupervisores);

            if (user.IsInRole("PersonalMunicipal") || user.IsInRole("Administrador"))
                await Groups.AddToGroupAsync(Context.ConnectionId, GrupoMunicipal);
        }

        await base.OnConnectedAsync();
    }
}