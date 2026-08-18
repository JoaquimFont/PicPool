using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PicPool.Infrastructure.Services
{
    public class SalaOperacioCua
    {
        private readonly ConcurrentDictionary<string, SemaphoreSlim> _semaforsSala = new();

        /// <summary>
        /// Explicació: serialitza operacions per sala perquè no s'executin simultàniament sobre la mateixa sala.
        /// Precondicions: <paramref name="salaPk"/> ha d'identificar la sala i <paramref name="operacio"/> ha de contenir la feina a executar.
        /// Postcondicions: l'operació retorna el seu resultat després d'obtenir el torn; el semàfor de la sala s'allibera sempre en finalitzar.
        /// </summary>
        public async Task<TResult> ExecutarEnCuaAsync<TResult>(
            string salaPk,
            Func<Task<TResult>> operacio,
            Func<Task>? quanEntraEnCua = null,
            CancellationToken cancellationToken = default)
        {
            var semafor = _semaforsSala.GetOrAdd(
                salaPk,
                _ => new SemaphoreSlim(1, 1)
            );

            var haEntratDirecte = await semafor.WaitAsync(0, cancellationToken);

            if (!haEntratDirecte)
            {
                if (quanEntraEnCua != null)
                {
                    await quanEntraEnCua();
                }

                await semafor.WaitAsync(cancellationToken);
            }

            try
            {
                return await operacio();
            }
            finally
            {
                semafor.Release();
            }
        }
    }
}
