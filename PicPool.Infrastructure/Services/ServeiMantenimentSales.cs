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

        /// <summary>
        /// Explicació: inicialitza el servei de manteniment de sales amb accés a dades, fitxers, notificacions i logging.
        /// Precondicions: totes les dependències han d'estar registrades al contenidor d'injecció.
        /// Postcondicions: el servei pot avisar sales properes a expirar i eliminar sales caducades.
        /// </summary>
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

        /// <summary>
        /// Explicació: executa totes les tasques de manteniment previstes sobre les sales.
        /// Precondicions: el context de dades ha d'estar disponible i el token de cancel·lació ha de representar l'operació actual.
        /// Postcondicions: s'han processat avisos d'expiració i eliminacions de sales expirades segons la data actual.
        /// </summary>
        public async Task ExecutarMantenimentAsync(
            CancellationToken cancellationToken = default)
        {
            var ara = DateTime.UtcNow;

            await AvisarSalesProperesAExpirarAsync(ara, cancellationToken);
            await EliminarSalesExpiradesAsync(ara, cancellationToken);
        }

        /// <summary>
        /// Explicació: envia avisos als usuaris de sales que expiraran dins del marge definit.
        /// Precondicions: la data de referència ha de ser coherent i les sales han de tenir usuaris carregables amb email.
        /// Postcondicions: les sales avisades queden marcades amb la data d'avís per evitar notificacions duplicades.
        /// </summary>
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

        /// <summary>
        /// Explicació: elimina sales actives que ja han superat la seva data d'expiració.
        /// Precondicions: la data de referència ha de ser coherent i les sales expirades han de poder carregar les seves relacions.
        /// Postcondicions: s'intenten eliminar fitxers remots i s'eliminen de la base de dades les sales i entitats associades.
        /// </summary>
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

        /// <summary>
        /// Explicació: envia un correu a tots els usuaris amb email associats a una sala.
        /// Precondicions: la sala ha de tenir la col·lecció d'usuaris carregada i l'assumpte/cos han d'estar informats.
        /// Postcondicions: s'intenta enviar el correu a cada destinatari únic; els errors individuals es registren i no interrompen la resta d'enviaments.
        /// </summary>
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
