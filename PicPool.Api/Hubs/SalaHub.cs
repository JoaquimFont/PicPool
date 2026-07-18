using Microsoft.AspNetCore.SignalR;

namespace PicPool.Api.Hubs
{
    public class SalaHub : Hub
    {
        public async Task EntrarSala(string salaPk)
        {
            await Groups.AddToGroupAsync(Context.ConnectionId, $"sala-{salaPk}");
        }

        public async Task SortirSala(string salaPk)
        {
            await Groups.RemoveFromGroupAsync(Context.ConnectionId, $"sala-{salaPk}");
        }
    }
}

