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
