using Microsoft.AspNetCore.SignalR;

namespace PicPool.Api.Hubs
{
    public class SalaHub : Hub
    {
        /// <summary>
        /// Explicació: afegeix la connexió SignalR actual al grup associat a una sala.
        /// Precondicions: <paramref name="salaPk"/> ha d'identificar la sala a la qual es vol escoltar.
        /// Postcondicions: la connexió rebrà els esdeveniments enviats al grup de la sala.
        /// </summary>
        public async Task EntrarSala(string salaPk)
        {
            await Groups.AddToGroupAsync(Context.ConnectionId, $"sala-{salaPk}");
        }

        /// <summary>
        /// Explicació: treu la connexió SignalR actual del grup associat a una sala.
        /// Precondicions: <paramref name="salaPk"/> ha d'identificar la sala de la qual es vol deixar d'escoltar.
        /// Postcondicions: la connexió deixa de rebre els esdeveniments enviats al grup de la sala.
        /// </summary>
        public async Task SortirSala(string salaPk)
        {
            await Groups.RemoveFromGroupAsync(Context.ConnectionId, $"sala-{salaPk}");
        }
    }
}

