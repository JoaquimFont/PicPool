using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using PicPool.Domain.Entities;
using PicPool.Infrastructure.Data;

namespace PicPool.Infrastructure.Services
{
    public class ServeiMantenimentSales
    {
        private readonly PicPoolDbContext _context;
        private readonly CloudflareR2Service _cloudflareR2Service;
        private readonly ServeiNotificacions _serveiNotificacions;
        private readonly ILogger<ServeiMantenimentSales> _logger;

        public ServeiMantenimentSales(
            PicPoolDbContext context,
            CloudflareR2Service cloudflareR2Service,
            ServeiNotificacions serveiNotificacions,
            ILogger<ServeiMantenimentSales> logger)
        {
            _context = context;
            _cloudflareR2Service = cloudflareR2Service;
            _serveiNotificacions = serveiNotificacions;
            _logger = logger;
        }

        public async Task ExecutarMantenimentAsync(
            CancellationToken cancellationToken = default)
        {
            var ara = DateTime.UtcNow;

            await AvisarSalesProperesAExpirarAsync(ara, cancellationToken);
            await EliminarSalesExpiradesAsync(ara, cancellationToken);
        }

        private async Task AvisarSalesProperesAExpirarAsync(
            DateTime ara,
            CancellationToken cancellationToken)
        {
            var dataLimitAvis = ara.AddDays(1);

            var sales = await _context.Sales
                .Include(s => s.UsuarisSala)
                    .ThenInclude(su => su.Usuari)
                .Where(s =>
                    s.Activa &&
                    s.DataExpiracio.HasValue &&
                    s.DataExpiracio.Value > ara &&
                    s.DataExpiracio.Value <= dataLimitAvis &&
                    s.DataAvisExpiracioEnviat == null)
                .ToListAsync(cancellationToken);

            foreach (var sala in sales)
            {
                await EnviarCorreuUsuarisSalaAsync(
                    sala,
                    "La sala expirarà aviat",
                    $"La sala \"{sala.Nom}\" expirarà el {sala.DataExpiracio:dd/MM/yyyy HH:mm}.\n\n" +
                    "Quan arribi aquesta data, la sala i totes les seves imatges s'eliminaran automàticament.\n\n" +
                    "Aquest és un avís automàtic de PicPool."
                );

                sala.DataAvisExpiracioEnviat = ara;
            }

            await _context.SaveChangesAsync(cancellationToken);
        }

        private async Task EliminarSalesExpiradesAsync(
            DateTime ara,
            CancellationToken cancellationToken)
        {
            var sales = await _context.Sales
                .Include(s => s.UsuarisSala)
                    .ThenInclude(su => su.Usuari)
                .Include(s => s.ImatgesSala)
                    .ThenInclude(si => si.Imatge)
                .Include(s => s.LinksCompartits)
                .AsSplitQuery()
                .Where(s =>
                    s.Activa &&
                    s.DataExpiracio.HasValue &&
                    s.DataExpiracio.Value <= ara)
                .ToListAsync(cancellationToken);

            foreach (var sala in sales)
            {
                await EnviarCorreuUsuarisSalaAsync(
                    sala,
                    "Sala eliminada per expiració",
                    $"La sala \"{sala.Nom}\" ha estat eliminada perquè ha arribat a la seva data d'expiració.\n\n" +
                    "També s'han eliminat les imatges associades a aquesta sala.\n\n" +
                    "Aquest és un avís automàtic de PicPool."
                );

                var imatges = sala.ImatgesSala
                    .Where(si => si.Imatge != null)
                    .Select(si => si.Imatge!)
                    .DistinctBy(i => i.ImatgePK)
                    .ToList();

                foreach (var imatge in imatges)
                {
                    try
                    {
                        await _cloudflareR2Service.EliminarFitxerAsync(
                            imatge.RutaStorage,
                            cancellationToken
                        );
                    }
                    catch (Exception ex)
                    {
                        _logger.LogWarning(
                            ex,
                            "No s'ha pogut eliminar el fitxer {RutaStorage} de la sala {SalaPK}.",
                            imatge.RutaStorage,
                            sala.SalaPK
                        );
                    }
                }

                _context.SalaLinksCompartits.RemoveRange(sala.LinksCompartits);
                _context.SalaImatges.RemoveRange(sala.ImatgesSala);
                _context.Imatges.RemoveRange(imatges);
                _context.SalaUsuaris.RemoveRange(sala.UsuarisSala);
                _context.Sales.Remove(sala);
            }

            await _context.SaveChangesAsync(cancellationToken);
        }

        private async Task EnviarCorreuUsuarisSalaAsync(
            Sala sala,
            string assumpte,
            string cos)
        {
            var correus = sala.UsuarisSala
                .Where(su => su.Usuari != null)
                .Select(su => su.Usuari.Email)
                .Where(email => !string.IsNullOrWhiteSpace(email))
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .ToArray();

            foreach (var correu in correus)
            {
                try
                {
                    await _serveiNotificacions.EnviarCorreuAsync(
                        correu,
                        assumpte,
                        cos
                    );
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(
                        ex,
                        "No s'ha pogut enviar el correu de manteniment a {Email} per la sala {SalaPK}.",
                        correu,
                        sala.SalaPK
                    );
                }
            }
        }
    }
}