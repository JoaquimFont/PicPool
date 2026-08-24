using Microsoft.EntityFrameworkCore;
using PicPool.Domain.Entities;
using PicPool.Infrastructure.Data;
using System.IO.Compression;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using static Microsoft.EntityFrameworkCore.DbLoggerCategory;

namespace PicPool.Infrastructure.Services
{
    public class ServeiSala
    {
        private readonly PicPoolDbContext _context;
        private readonly CloudflareR2Service _cloudflareR2Service;
        private readonly ServeiNotificacions _serveiNotificacions;
        /// <summary>
        /// Explicació: inicialitza el servei de sales amb accés a dades, emmagatzematge R2 i notificacions.
        /// Precondicions: el context, el servei R2 i el servei de notificacions han d'estar registrats a la injecció de dependències.
        /// Postcondicions: el servei queda preparat per gestionar links, imatges, permisos i estadístiques de sales.
        /// </summary>
        public ServeiSala(
            PicPoolDbContext context,
            CloudflareR2Service cloudflareR2Service, 
            ServeiNotificacions serveiNotificacions)
        {
            _context = context;
            _cloudflareR2Service = cloudflareR2Service;
            _serveiNotificacions = serveiNotificacions;
        }

        /// <summary>
        /// Explicació: crea un link compartit per a una sala amb un conjunt de permisos i metadades.
        /// Precondicions: la sala ha d'existir i estar activa; l'usuari creador ha de formar part de la sala i poder gestionar-la.
        /// Postcondicions: el link compartit queda persistit i es retorna l'entitat creada.
        /// </summary>
        public async Task<SalaLinkCompartit> CrearLinkCompartitSalaAsync(
            string salaPK,
            string usuariCreadorPK,
            string nom,
            string rol,
            bool potVeure,
            bool potPujar,
            bool potDescarregar,
            bool potEliminarPropies,
            bool potEliminarQualsevol,
            bool potGestionarSala,
            DateTime? dataExpiracio = null,
            int? limitUsos = null)
        {
            var sala = await _context.Sales
                .FirstOrDefaultAsync(s => s.SalaPK == salaPK && s.Activa);

            if (sala == null)
                throw new Exception("La sala no existeix o no està activa.");

            var usuariSala = await _context.SalaUsuaris
                .FirstOrDefaultAsync(su =>
                    su.SalaPK == salaPK &&
                    su.UsuariPK == usuariCreadorPK);

            if (usuariSala == null || !usuariSala.PotGestionarSala)
                throw new Exception("No tens permisos per crear links d'aquesta sala.");

            var link = new SalaLinkCompartit
            {
                SalaLinkCompartitPK = Guid.NewGuid().ToString(),
                SalaPK = salaPK,
                Token = Guid.NewGuid().ToString("N"),
                Nom = nom,
                Rol = rol,
                PotVeure = potVeure,
                PotPujar = potPujar,
                PotDescarregar = potDescarregar,
                PotEliminarPropies = potEliminarPropies,
                PotEliminarQualsevol = potEliminarQualsevol,
                PotGestionarSala = potGestionarSala,
                Actiu = true,
                DataCreacio = DateTime.UtcNow,
                DataExpiracio = dataExpiracio,
                LimitUsos = limitUsos,
                UsosActuals = 0,
                UsuariCreadorPK = usuariCreadorPK
            };

            _context.SalaLinksCompartits.Add(link);
            await _context.SaveChangesAsync();

            return link;
        }

        /// <summary>
        /// Explicació: accepta un link compartit i afegeix l'usuari a la sala o amplia els permisos existents sense reduir-los.
        /// Precondicions: el token ha de correspondre a un link existent, actiu i vàlid; l'usuari ha d'estar identificat.
        /// Postcondicions: l'usuari queda vinculat a la sala o amb permisos actualitzats, i es retorna l'identificador de la sala.
        /// </summary>
        public async Task<string> AcceptarLinkCompartitSalaAsync(string token, string usuariActualPK)
        {
            var link = await _context.SalaLinksCompartits
                .Include(l => l.Sala)
                .FirstOrDefaultAsync(l => l.Token == token);

            if (link == null)
                throw new Exception("L'enllaç no existeix.");

            ValidarLinkCompartit(link);

            var salaUsuari = await _context.SalaUsuaris
                .FirstOrDefaultAsync(su =>
                    su.SalaPK == link.SalaPK &&
                    su.UsuariPK == usuariActualPK);

            var usuariNouAfegit = false;

            if (salaUsuari == null)
            {
                salaUsuari = new SalaUsuari
                {
                    SalaUsuariPK = Guid.NewGuid().ToString(),
                    SalaPK = link.SalaPK,
                    UsuariPK = usuariActualPK,
                    Rol = link.Rol,
                    PotVeure = link.PotVeure,
                    PotPujar = link.PotPujar,
                    PotDescarregar = link.PotDescarregar,
                    PotEliminarPropies = link.PotEliminarPropies,
                    PotEliminarQualsevol = link.PotEliminarQualsevol,
                    PotGestionarSala = link.PotGestionarSala,
                    DataUnio = DateTime.UtcNow
                };

                _context.SalaUsuaris.Add(salaUsuari);
                usuariNouAfegit = true;
            }
            else
            {
                AplicarPermisosSenseReduir(salaUsuari, link);
            }

            if (usuariNouAfegit)
            {
                link.UsosActuals++;
            }

            await _context.SaveChangesAsync();

            return link.SalaPK;
        }

        /// <summary>
        /// Explicació: desactiva un link compartit perquè ja no es pugui utilitzar.
        /// Precondicions: el link ha d'existir i l'usuari ha de tenir permisos de gestió sobre la sala del link.
        /// Postcondicions: el camp d'activació del link queda marcat com a fals i es desa a la base de dades.
        /// </summary>
        public async Task<bool> DesactivarLinkCompartitSalaAsync(string salaLinkCompartitPK, string usuariPK)
        {
            var link = await _context.SalaLinksCompartits
                .FirstOrDefaultAsync(l => l.SalaLinkCompartitPK == salaLinkCompartitPK);

            if (link == null)
            {
                throw new InvalidOperationException("El link no existeix.");
            }

            var usuariSala = await _context.SalaUsuaris
                .FirstOrDefaultAsync(su =>
                    su.SalaPK == link.SalaPK &&
                    su.UsuariPK == usuariPK);

            if (usuariSala == null || !usuariSala.PotGestionarSala)
            {
                throw new UnauthorizedAccessException("No tens permisos per desactivar aquest link.");
            }

            link.Actiu = false;

            await _context.SaveChangesAsync();

            return true;
        }

        /// <summary>
        /// Explicació: genera un token nou per a un link compartit i el reactiva.
        /// Precondicions: el link ha d'existir i l'usuari ha de tenir permisos de gestió sobre la sala del link.
        /// Postcondicions: el link queda actiu, amb token nou i comptador d'usos reiniciat.
        /// </summary>
        public async Task<SalaLinkCompartit> RegenerarTokenLinkCompartitSalaAsync(string salaLinkCompartitPK, string usuariPK)
        {
            var link = await _context.SalaLinksCompartits
                .FirstOrDefaultAsync(l => l.SalaLinkCompartitPK == salaLinkCompartitPK);

            if (link == null)
            {
                throw new InvalidOperationException("El link no existeix.");
            }

            var usuariSala = await _context.SalaUsuaris
                .FirstOrDefaultAsync(su =>
                    su.SalaPK == link.SalaPK &&
                    su.UsuariPK == usuariPK);

            if (usuariSala == null || !usuariSala.PotGestionarSala)
            {
                throw new UnauthorizedAccessException("No tens permisos per regenerar aquest link.");
            }

            link.Token = Guid.NewGuid().ToString("N");
            link.UsosActuals = 0;
            link.Actiu = true;

            await _context.SaveChangesAsync();

            return link;
        }

        /// <summary>
        /// Explicació: comprova si un usuari està vinculat a una sala.
        /// Precondicions: els identificadors de sala i usuari han d'estar informats.
        /// Postcondicions: retorna cert si existeix la relació sala-usuari; altrament retorna fals.
        /// </summary>
        public async Task<bool> UsuariFormaPartSalaAsync(string salaPK, string usuariPK)
        {
            return await _context.SalaUsuaris.AnyAsync(su =>
                su.SalaPK == salaPK &&
                su.UsuariPK == usuariPK);
        }

        /// <summary>
        /// Explicació: obté els links compartits d'una sala per ordre de creació descendent.
        /// Precondicions: l'usuari ha de formar part de la sala i tenir permisos per gestionar-la.
        /// Postcondicions: retorna els links de la sala o propaga una excepció si l'usuari no té permisos.
        /// </summary>
        public async Task<SalaLinkCompartit[]> ObtenirLinksCompartitsSalaAsync( string salaPK, string usuariPK)
        {
            var usuariSala = await _context.SalaUsuaris
                .FirstOrDefaultAsync(su =>
                    su.SalaPK == salaPK &&
                    su.UsuariPK == usuariPK);

            if (usuariSala == null || !usuariSala.PotGestionarSala)
            {
                throw new UnauthorizedAccessException("No tens permisos per veure els links d'aquesta sala.");
            }

            return await _context.SalaLinksCompartits
                .Where(l => l.SalaPK == salaPK)
                .OrderByDescending(l => l.DataCreacio)
                .ToArrayAsync();
        }

        /// <summary>
        /// Explicació: recupera la informació d'un link compartit a partir del token.
        /// Precondicions: el token ha d'existir i el link ha de superar les validacions d'estat, expiració i usos.
        /// Postcondicions: retorna el link amb la sala carregada o propaga una excepció funcional si no és vàlid.
        /// </summary>
        public async Task<SalaLinkCompartit> ObtenirInfoLinkCompartitAsync(string token)
        {
            var link = await _context.SalaLinksCompartits
               .Include(l => l.Sala)
               .FirstOrDefaultAsync(l => l.Token == token);

            if (link == null)
            {
                throw new InvalidOperationException("L'enllaç no existeix.");
            }

            ValidarLinkCompartit(link);

            return link;
        }
        /// <summary>
        /// Explicació: compta quantes imatges hi ha associades a una sala.
        /// Precondicions: l'identificador de sala ha d'estar informat.
        /// Postcondicions: retorna el nombre de relacions sala-imatge existents per a la sala.
        /// </summary>
        public async Task<int> ComptarImatgesSalaAsync(string salaPK)
        {
            return await _context.SalaImatges
                .CountAsync(si => si.SalaPK == salaPK);
        }

        /// <summary>
        /// Explicació: obté els identificadors de les imatges associades a una sala.
        /// Precondicions: l'identificador de sala ha d'estar informat.
        /// Postcondicions: retorna la llista d'identificadors d'imatge existents dins de la sala.
        /// </summary>
        public async Task<string[]> ObtenirImatgePksExistentsSalaAsync(string salaPK)
        {
            return await _context.SalaImatges
                .Where(si => si.SalaPK == salaPK)
                .Select(si => si.ImatgePK)
                .ToArrayAsync();
        }

        /// <summary>
        /// Explicació: valida si un lot d'imatges es pot afegir a una sala segons el pla del creador de la sala.
        /// Precondicions: la sala ha d'existir i estar activa; les mides han de venir dels fitxers rebuts pel backend.
        /// Postcondicions: si el lot supera el pla del creador, es propaga una excepció abans de pujar cap fitxer.
        /// </summary>
        public async Task ValidarPujadaImatgesSalaAsync(string salaPK, long midaTotalBytes, int totalImatges)
        {
            var sala = await _context.Sales
                .FirstOrDefaultAsync(s => s.SalaPK == salaPK && s.Activa);

            if (sala == null)
            {
                throw new InvalidOperationException("La sala no existeix o no està activa.");
            }

            await ValidarQuotaPlaCreadorSalaAsync(sala, midaTotalBytes, totalImatges);
        }

        /// <summary>
        /// Explicació: puja una imatge a R2, crea la seva entitat i l'associa a una sala.
        /// Precondicions: la sala ha d'existir i estar activa; el propietari ha de correspondre a un usuari existent; l'stream i metadades han de ser vàlids.
        /// Postcondicions: la imatge queda persistida, associada a la sala, amb estadístiques de sala actualitzades i notificació enviada si correspon.
        /// </summary>
        public async Task<Imatge> PujarImatge(string salaPK, Stream stream, string fileName, string contentType, long midaBytes, decimal resolucio, string? descripcio, string? propietari)
        {
            var sala = await _context.Sales
                .Include(s => s.Usuari)
                .FirstOrDefaultAsync(s => s.SalaPK == salaPK && s.Activa);

            if (sala == null)
            {
                throw new InvalidOperationException("La sala no existeix o no està activa.");
            }

            var usuariPujador = await _context.Usuaris
                .FirstOrDefaultAsync(u => u.UsuariPK == propietari);

            if (usuariPujador == null)
            {
                throw new InvalidOperationException("L'usuari que puja la imatge no existeix.");
            }

            var quota = await ValidarQuotaPlaCreadorSalaAsync(sala, midaBytes, 1);

            var imatgePK = Guid.NewGuid().ToString("N");
            var imatgeSalaPK = Guid.NewGuid().ToString("N");

            var extension = Path.GetExtension(fileName);
            var objectKey = $"sales/{salaPK}/{imatgePK}{extension}";

            var sourceUrl = await _cloudflareR2Service.PujarFitxerAsync(
                stream,
                objectKey,
                contentType
            );

            var imatge = new Imatge
            {
                ImatgePK = imatgePK,
                NomOriginal = fileName,
                RutaStorage = objectKey,
                SourceUrl = sourceUrl,
                MidaBytes = midaBytes,
                TipusMime = contentType,
                UsuariPujadorPK = usuariPujador.UsuariPK,
                Usuari = usuariPujador,
                DataPujada = DateTime.UtcNow
            };

            var salaImatge = new SalaImatge
            {
                ImatgeSalaPK = imatgeSalaPK,
                SalaPK = salaPK,
                ImatgePK = imatgePK
            };

            _context.Imatges.Add(imatge);
            _context.SalaImatges.Add(salaImatge);

            sala.PesTotal += midaBytes;
            sala.TotalImatges += 1;
            quota.UsuariPla.EspaiConsumitBytes = quota.EspaiConsumitBytesActual + midaBytes;
            quota.UsuariPla.ImatgesPujades = quota.ImatgesPujadesActuals + 1;
            quota.UsuariPla.SalesCreades = quota.SalesCreadesActuals;

            await _context.SaveChangesAsync();

            if (sala.UsuariCreadorPK != usuariPujador.UsuariPK)
            {
                await EnviarCorreuImatgePujadaAsync(
                    sala,
                    usuariPujador,
                    imatge
                );
            }

            return imatge;
        }

        /// <summary>
        /// Explicació: obté les imatges d'una sala aplicant ordenació i paginació.
        /// Precondicions: els identificadors de sala i usuari han d'estar informats; els paràmetres de consulta han de ser coherents.
        /// Postcondicions: retorna les imatges de la pàgina sol·licitada amb l'usuari carregat.
        /// </summary>
        public async Task<Imatge[]> ObtenirImatgesSala(string salaPK, string usuariPK, int pagina = 1, int quantitat = 20, string ordre = "data", bool descendent = false) 
        {
            var consultaImatgesSala = _context.SalaImatges
                .Where(w => w.SalaPK == salaPK)
                .Include(si => si.Imatge)
                .ThenInclude(i => i.Usuari)
                .Select(s => s.Imatge);
            


            consultaImatgesSala = ordre switch
            {
                "nom" => descendent
                    ? consultaImatgesSala.OrderByDescending(i => i.NomOriginal)
                    : consultaImatgesSala.OrderBy(i => i.NomOriginal),

                "data" => descendent
                    ? consultaImatgesSala.OrderByDescending(i => i.DataPujada)
                    : consultaImatgesSala.OrderBy(i => i.DataPujada),

                "mida" => descendent
                    ? consultaImatgesSala.OrderByDescending(i => i.MidaBytes)
                    : consultaImatgesSala.OrderBy(i => i.MidaBytes),

                _ => consultaImatgesSala.OrderByDescending(i => i.DataPujada)
            };

            return await consultaImatgesSala
                                .Skip((pagina - 1) * quantitat)
                                .Take(quantitat)
                                .ToArrayAsync();
            
        }
        /// <summary>
        /// Explicació: obté les imatges que s'han de descarregar d'una sala.
        /// Precondicions: la sala ha d'estar informada; si no es descarreguen totes, la llista d'imatges ha de contenir identificadors.
        /// Postcondicions: retorna totes les imatges de la sala o només les seleccionades segons el paràmetre indicat.
        /// </summary>
        public async Task<Imatge[]> ObtenirImatgesDescarregarSala(string salaPK, string[] imatgePks, bool totes = false)
        {
            var consultaImatgesSala = _context.SalaImatges
                .Where(w => w.SalaPK == salaPK)
                .Include(si => si.Imatge)
                .ThenInclude(i => i.Usuari);
                

            if (totes)
            {
                return await consultaImatgesSala.Select(s => s.Imatge).ToArrayAsync();
            }
            else
            {
                return await consultaImatgesSala.Where(w=> imatgePks.Contains(w.ImatgePK)).Select(s => s.Imatge).ToArrayAsync();
            }
        }

        /// <summary>
        /// Explicació: elimina imatges d'una sala, esborra els fitxers remots quan és possible i recalcula estadístiques.
        /// Precondicions: la sala i l'usuari han d'existir; la llista d'imatges ha de contenir identificadors associats a la sala.
        /// Postcondicions: les imatges trobades queden eliminades de la base de dades, s'intenta eliminar el fitxer remot i es retornen les estadístiques actualitzades.
        /// </summary>
        public async Task<bool> EliminarImatgesSala(string salaPk, string usuariPK, string[] imatgePks)
        {
            var sala = await _context.Sales
                .Include(s => s.Usuari)
                .FirstOrDefaultAsync(s => s.SalaPK == salaPk);

            if (sala == null)
            {
                throw new InvalidOperationException("La sala no existeix.");
            }

            var usuariEliminador = await _context.Usuaris
                .FirstOrDefaultAsync(u => u.UsuariPK == usuariPK);

            if (usuariEliminador == null)
            {
                throw new InvalidOperationException("L'usuari no existeix.");
            }

            var salaImatgesABorrar = await _context.SalaImatges
                .Where(w => w.SalaPK == salaPk && imatgePks.Contains(w.ImatgePK))
                .ToListAsync();

            if (salaImatgesABorrar.Count == 0)
            {
                return false;
            }

            var imatgePksReals = salaImatgesABorrar
                .Select(si => si.ImatgePK)
                .ToArray();

            var imatgesABorrar = await _context.Imatges
                .Where(w => imatgePksReals.Contains(w.ImatgePK))
                .ToListAsync();

            var nomsImatgesEliminades = imatgesABorrar
                .Select(i => i.NomOriginal)
                .ToArray();

            foreach (var imatge in imatgesABorrar)
            {
                try
                {
                    await _cloudflareR2Service.EliminarFitxerAsync(imatge.RutaStorage);
                }
                catch
                {
                    // No fem fallar tota l'eliminació si falla l'esborrat del bucket.
                    // La imatge igualment s'eliminarà de la base de dades.
                }
            }

            _context.SalaImatges.RemoveRange(salaImatgesABorrar);
            _context.Imatges.RemoveRange(imatgesABorrar);

            await _context.SaveChangesAsync();

            await RecalcularEstadistiquesSalaAsync(salaPk);
            await SincronitzarPlaCreadorSalaAsync(sala.UsuariCreadorPK);

            if (sala.UsuariCreadorPK != usuariEliminador.UsuariPK)
            {
                await EnviarCorreuImatgesEliminadesAsync(
                    sala,
                    usuariEliminador,
                    nomsImatgesEliminades
                );
            }

            return true;
        }

        /// <summary>
        /// Explicació: crea un ZIP temporal amb els fitxers remots corresponents a una llista d'imatges.
        /// Precondicions: les imatges han de contenir rutes d'emmagatzematge vàlides quan es vulguin incloure al ZIP.
        /// Postcondicions: retorna la ruta del fitxer ZIP temporal creat.
        /// </summary>
        public async Task<string> CrearZipTemporalImatgesSalaAsync(Imatge[] imatges, CancellationToken cancellationToken = default)
        {
            var carpetaTemporal = Path.Combine(
                Path.GetTempPath(),
                "PicPool",
                "Downloads"
            );

            Directory.CreateDirectory(carpetaTemporal);

            var zipPath = Path.Combine(
                carpetaTemporal,
                $"imatges-{Guid.NewGuid():N}.zip"
            );

            await using var fileStream = new FileStream(
                zipPath,
                FileMode.CreateNew,
                FileAccess.ReadWrite,
                FileShare.None,
                bufferSize: 1024 * 128,
                options: FileOptions.Asynchronous | FileOptions.SequentialScan
            );

            using (var zipArchive = new ZipArchive(
                fileStream,
                ZipArchiveMode.Create,
                leaveOpen: true))
            {
                foreach (var imatge in imatges)
                {
                    cancellationToken.ThrowIfCancellationRequested();

                    if (string.IsNullOrWhiteSpace(imatge.RutaStorage))
                    {
                        continue;
                    }

                    var nomFitxer = string.IsNullOrWhiteSpace(imatge.NomOriginal)
                        ? $"{imatge.ImatgePK}{Path.GetExtension(imatge.RutaStorage)}"
                        : imatge.NomOriginal;

                    var zipEntry = zipArchive.CreateEntry(
                        nomFitxer,
                        CompressionLevel.NoCompression
                    );

                    await using var zipEntryStream = zipEntry.Open();

                    await _cloudflareR2Service.CopiarFitxerAStreamAsync(
                        imatge.RutaStorage,
                        zipEntryStream,
                        cancellationToken
                    );
                }
            }

            return zipPath;
        }


        //MetodesPrivats
        /// <summary>
        /// Explicació: envia una notificació al creador de la sala quan un altre usuari puja una imatge.
        /// Precondicions: la sala ha de tenir usuari creador i email disponibles; la imatge i l'usuari pujador han d'estar informats.
        /// Postcondicions: s'intenta enviar el correu; si falla, la fallada queda absorbida per no interrompre el flux principal.
        /// </summary>
        private async Task EnviarCorreuImatgePujadaAsync(Sala sala, Usuari usuariPujador,Imatge imatge)
        {
            if (sala.Usuari == null || string.IsNullOrWhiteSpace(sala.Usuari.Email))
            {
                return;
            }

            try
            {
                await _serveiNotificacions.EnviarCorreuAsync(
                    sala.Usuari.Email,
                    "Nova imatge pujada a PicPool",
                    $"L'usuari {usuariPujador.Nom} ha pujat una imatge a la sala \"{sala.Nom}\".\n\n" +
                    $"Imatge: {imatge.NomOriginal}\n" +
                    $"Data: {imatge.DataPujada:dd/MM/yyyy HH:mm}\n\n" +
                    "Aquest és un avís automàtic de PicPool."
                );
            }
            catch
            {
                // No fem fallar la pujada de la imatge si falla el correu.
            }
        }

        /// <summary>
        /// Explicació: envia una notificació al creador de la sala quan un altre usuari elimina imatges.
        /// Precondicions: la sala ha de tenir usuari creador i email disponibles; l'usuari eliminador i els noms d'imatges han d'estar disponibles.
        /// Postcondicions: s'intenta enviar el correu; si falla, la fallada queda absorbida per no interrompre el flux principal.
        /// </summary>
        private async Task EnviarCorreuImatgesEliminadesAsync( Sala sala, Usuari usuariEliminador, string[] nomsImatges)
        {
            if (sala.Usuari == null || string.IsNullOrWhiteSpace(sala.Usuari.Email))
            {
                return;
            }

            try
            {
                var llistaImatges = nomsImatges.Length == 0
                    ? "No s'ha pogut obtenir el nom de les imatges."
                    : string.Join("\n", nomsImatges.Select(nom => $"- {nom}"));

                await _serveiNotificacions.EnviarCorreuAsync(
                    sala.Usuari.Email,
                    "Imatges eliminades a PicPool",
                    $"L'usuari {usuariEliminador.Nom} ha eliminat imatges de la sala \"{sala.Nom}\".\n\n" +
                    $"Imatges eliminades:\n{llistaImatges}\n\n" +
                    $"Data: {DateTime.UtcNow:dd/MM/yyyy HH:mm}\n\n" +
                    "Aquest és un avís automàtic de PicPool."
                );
            }
            catch
            {
                // No fem fallar l'eliminació si falla el correu.
            }
        }

        /// <summary>
        /// Explicació: valida que un link compartit es pugui utilitzar.
        /// Precondicions: el link ha d'estar carregat amb la sala associada quan calgui validar-ne l'estat.
        /// Postcondicions: si el link no és vàlid, es propaga una excepció; si és vàlid, el flux pot continuar.
        /// </summary>
        private void ValidarLinkCompartit(SalaLinkCompartit link)
        {
            if (!link.Actiu)
                throw new Exception("Aquest enllaç està desactivat.");

            if (link.DataExpiracio.HasValue && link.DataExpiracio.Value < DateTime.UtcNow)
                throw new Exception("Aquest enllaç ha caducat.");

            if (link.LimitUsos.HasValue && link.UsosActuals >= link.LimitUsos.Value)
                throw new Exception("Aquest enllaç ha arribat al límit d'usos.");

            if (link.Sala == null || !link.Sala.Activa)
                throw new Exception("La sala no està activa.");
        }

        /// <summary>
        /// Explicació: aplica els permisos d'un link a una relació sala-usuari sense retirar permisos ja existents.
        /// Precondicions: la relació sala-usuari i el link compartit han d'estar carregats.
        /// Postcondicions: els permisos de la relació queden ampliats només quan el link concedeix permisos addicionals.
        /// </summary>
        private void AplicarPermisosSenseReduir(SalaUsuari salaUsuari, SalaLinkCompartit link)
        {
            salaUsuari.PotVeure = salaUsuari.PotVeure || link.PotVeure;
            salaUsuari.PotPujar = salaUsuari.PotPujar || link.PotPujar;
            salaUsuari.PotDescarregar = salaUsuari.PotDescarregar || link.PotDescarregar;
            salaUsuari.PotEliminarPropies = salaUsuari.PotEliminarPropies || link.PotEliminarPropies;
            salaUsuari.PotEliminarQualsevol = salaUsuari.PotEliminarQualsevol || link.PotEliminarQualsevol;
            salaUsuari.PotGestionarSala = salaUsuari.PotGestionarSala || link.PotGestionarSala;

            salaUsuari.Rol = ObtenirRolMesAlt(salaUsuari.Rol, link.Rol);
        }

        /// <summary>
        /// Explicació: compara dos rols i retorna el de més prioritat segons una escala interna.
        /// Precondicions: els noms de rol han de correspondre, preferentment, a valors reconeguts per l'escala.
        /// Postcondicions: retorna el rol nou si té més pes; en cas contrari manté el rol actual.
        /// </summary>
        private string ObtenirRolMesAlt(string rolActual, string rolNou)
        {
            var pesos = new Dictionary<string, int>
            {
                { "Lector", 1 },
                { "Editor", 2 },
                { "Gestor", 3 },
                { "Admin", 4 }
            };

            var pesActual = pesos.ContainsKey(rolActual) ? pesos[rolActual] : 0;
            var pesNou = pesos.ContainsKey(rolNou) ? pesos[rolNou] : 0;

            return pesNou > pesActual ? rolNou : rolActual;
        }

        /// <summary>
        /// Explicació: valida espai i nombre d'imatges contra el pla actiu del creador de la sala.
        /// Precondicions: la sala ha d'incloure l'identificador del creador i les quantitats a afegir han de ser positives o zero.
        /// Postcondicions: retorna les dades d'ús actuals per actualitzar el comptador del pla si la pujada continua.
        /// </summary>
        private async Task<QuotaPlaSala> ValidarQuotaPlaCreadorSalaAsync(Sala sala, long midaAfegirBytes, int imatgesAfegir)
        {
            var plaActiu = await (
                from usuariPla in _context.UsuariPlans
                join pla in _context.Plans on usuariPla.PlaPK equals pla.PlaPK
                where usuariPla.UsuariPK == sala.UsuariCreadorPK && usuariPla.Actiu && pla.Actiu
                orderby usuariPla.DataInici descending
                select new { UsuariPla = usuariPla, Pla = pla }
            ).FirstOrDefaultAsync();

            if (plaActiu == null)
            {
                throw new InvalidOperationException("El creador de la sala ha de tenir un pla actiu per poder pujar imatges.");
            }

            var usage = await CalcularUsageCreadorSalaAsync(sala.UsuariCreadorPK);
            var espaiResultant = usage.EspaiConsumitBytes + midaAfegirBytes;
            var imatgesResultants = usage.ImatgesPujades + imatgesAfegir;

            if (espaiResultant > plaActiu.Pla.LimitEmmagatzematgeBytes)
            {
                var espaiDisponible = Math.Max(0, plaActiu.Pla.LimitEmmagatzematgeBytes - usage.EspaiConsumitBytes);
                throw new InvalidOperationException($"La pujada supera l'espai disponible del creador de la sala. Queden {FormatBytes(espaiDisponible)}.");
            }

            if (imatgesResultants > plaActiu.Pla.LimitImatges)
            {
                var imatgesDisponibles = Math.Max(0, plaActiu.Pla.LimitImatges - usage.ImatgesPujades);
                throw new InvalidOperationException($"La pujada supera el límit d'imatges del creador de la sala. Queden {imatgesDisponibles} imatge(s).");
            }

            return new QuotaPlaSala
            {
                UsuariPla = plaActiu.UsuariPla,
                EspaiConsumitBytesActual = usage.EspaiConsumitBytes,
                SalesCreadesActuals = usage.SalesCreades,
                ImatgesPujadesActuals = usage.ImatgesPujades
            };
        }

        /// <summary>
        /// Explicació: calcula l'ús total imputable al creador d'una sala segons totes les seves sales actives.
        /// Precondicions: l'identificador del creador ha d'estar informat.
        /// Postcondicions: retorna sales actives creades, bytes consumits i imatges associades a aquestes sales.
        /// </summary>
        private async Task<UsagePlaSala> CalcularUsageCreadorSalaAsync(string usuariCreadorPK)
        {
            var consultaImatges = _context.SalaImatges
                .Where(salaImatge =>
                    salaImatge.Sala.UsuariCreadorPK == usuariCreadorPK &&
                    salaImatge.Sala.Activa)
                .Select(salaImatge => salaImatge.Imatge);

            return new UsagePlaSala
            {
                SalesCreades = await _context.Sales
                    .CountAsync(sala => sala.UsuariCreadorPK == usuariCreadorPK && sala.Activa),
                EspaiConsumitBytes = await consultaImatges.SumAsync(imatge => (long?)imatge.MidaBytes) ?? 0,
                ImatgesPujades = await consultaImatges.CountAsync()
            };
        }

        /// <summary>
        /// Explicació: recalcula i desa els comptadors del pla actiu del creador d'una sala.
        /// Precondicions: l'identificador del creador ha d'estar informat; pot no existir cap pla actiu.
        /// Postcondicions: si hi ha pla actiu, els comptadors de sales, espai i imatges queden sincronitzats.
        /// </summary>
        private async Task SincronitzarPlaCreadorSalaAsync(string usuariCreadorPK)
        {
            var usuariPla = await _context.UsuariPlans
                .Where(up => up.UsuariPK == usuariCreadorPK && up.Actiu)
                .OrderByDescending(up => up.DataInici)
                .FirstOrDefaultAsync();

            if (usuariPla == null)
            {
                return;
            }

            var usage = await CalcularUsageCreadorSalaAsync(usuariCreadorPK);
            usuariPla.SalesCreades = usage.SalesCreades;
            usuariPla.EspaiConsumitBytes = usage.EspaiConsumitBytes;
            usuariPla.ImatgesPujades = usage.ImatgesPujades;

            await _context.SaveChangesAsync();
        }

        /// <summary>
        /// Explicació: formata una quantitat de bytes en MB o GB per mostrar-la en missatges funcionals.
        /// Precondicions: el valor de bytes ha de representar una mida no negativa.
        /// Postcondicions: retorna una cadena llegible amb dues xifres decimals com a màxim.
        /// </summary>
        private static string FormatBytes(long bytes)
        {
            var gigabytes = bytes / 1024d / 1024d / 1024d;

            if (gigabytes >= 1)
            {
                return $"{gigabytes:0.##} GB";
            }

            var megabytes = bytes / 1024d / 1024d;
            return $"{megabytes:0.##} MB";
        }


        /// <summary>
        /// Explicació: recalcula el nombre total d'imatges i el pes total d'una sala.
        /// Precondicions: l'identificador de sala ha d'estar informat; si la sala no existeix, no s'apliquen canvis.
        /// Postcondicions: si la sala existeix, les estadístiques queden sincronitzades amb les imatges associades.
        /// </summary>
        private async Task RecalcularEstadistiquesSalaAsync(string salaPK)
        {
            var sala = await _context.Sales
                .FirstOrDefaultAsync(s => s.SalaPK == salaPK);

            if (sala == null)
            {
                return;
            }

            var consultaImatges = _context.SalaImatges
                .Where(si => si.SalaPK == salaPK)
                .Select(si => si.Imatge);

            sala.TotalImatges = await consultaImatges.CountAsync();
            sala.PesTotal = await consultaImatges.SumAsync(i => (decimal?)i.MidaBytes) ?? 0;

            await _context.SaveChangesAsync();
        }
    }

    internal class QuotaPlaSala
    {
        public UsuariPla UsuariPla { get; set; }

        public long EspaiConsumitBytesActual { get; set; }

        public int SalesCreadesActuals { get; set; }

        public int ImatgesPujadesActuals { get; set; }
    }

    internal class UsagePlaSala
    {
        public long EspaiConsumitBytes { get; set; }

        public int SalesCreades { get; set; }

        public int ImatgesPujades { get; set; }
    }
}

