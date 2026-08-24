using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Identity;
using PicPool.Domain.Entities;
using PicPool.Infrastructure.Data;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using static Microsoft.EntityFrameworkCore.DbLoggerCategory;

namespace PicPool.Infrastructure.Services
{
    public class ServeiUsuaris
    {
        private readonly PicPoolDbContext _context;
        private readonly ServeiNotificacions _serveiNotificacions;
        /// <summary>
        /// Explicació: inicialitza el servei d'usuaris amb el context de dades i el servei de notificacions.
        /// Precondicions: el context i el servei de notificacions han d'estar registrats a la injecció de dependències.
        /// Postcondicions: el servei pot crear usuaris, validar credencials i gestionar sales vinculades a usuaris.
        /// </summary>
        public ServeiUsuaris(PicPoolDbContext context, ServeiNotificacions serveiNotificacions)
        {
            _context = context;
            _serveiNotificacions = serveiNotificacions;

        }

        /// <summary>
        /// Explicació: crea un usuari nou amb contrasenya hashejada i li assigna el pla gratuït actiu per defecte.
        /// Precondicions: el nom, email i password han de ser valors vàlids, no hauria d'existir un usuari amb el mateix nom o email i ha d'existir un pla gratuït actiu.
        /// Postcondicions: si no hi ha duplicats, l'usuari i el seu pla inicial queden guardats; si ja existeix, retorna un usuari marcador sense clau.
        /// </summary>
        public Usuari CrearUsuari(string nom, string email, string password)
        {

            //Primer mirem si ja existeix un usuari amb aquest nom o email.
            var existeixUsuario = _context.Usuaris.Where(s => s.Nom == nom || s.Email == email).FirstOrDefault();

            if(existeixUsuario != null)
            {
                return new Usuari()
                {
                    UsuariPK = "",
                    Nom = existeixUsuario.Nom,
                    Email = existeixUsuario.Email
                };
            }

            var hasher = new PasswordHasher<Usuari>();

            var usuariPk_ = Guid.NewGuid().ToString("N");
            var usuari = new Usuari
            {
                UsuariPK = usuariPk_,
                Nom = nom,
                Email = email,
                PasswordHash = ""
            };

            usuari.PasswordHash = hasher.HashPassword(usuari, password);

            var plaGratuit = ObtenirPlaGratuitActiu();
            if (plaGratuit == null)
            {
                throw new InvalidOperationException("No hi ha cap pla gratuït actiu configurat.");
            }

            var usuariPla = new UsuariPla
            {
                UsuariPlaPK = Guid.NewGuid().ToString("N"),
                UsuariPK = usuariPk_,
                PlaPK = plaGratuit.PlaPK,
                DataInici = DateTime.UtcNow,
                DataFi = null,
                Actiu = true,
                EspaiConsumitBytes = 0,
                SalesCreades = 0,
                ImatgesPujades = 0
            };

            _context.Usuaris.Add(usuari);
            _context.UsuariPlans.Add(usuariPla);
            _context.SaveChanges();


            var resposta = new Usuari()
            {
                UsuariPK = usuariPk_,
                Nom = nom,
                Email = email,
            };

            return resposta;
        }


        /// <summary>
        /// Explicació: valida el nom d'usuari i la contrasenya contra el hash desat.
        /// Precondicions: el nom d'usuari i la contrasenya han d'arribar informats.
        /// Postcondicions: retorna una resposta de login correcta amb dades bàsiques o una resposta d'error funcional.
        /// </summary>
        public object Login(string username, string password)
        {
            var usuari = _context.Usuaris
                .FirstOrDefault(w => w.Nom == username);

            if (usuari == null)
            {
                return new LoginResponseDto
                {
                    Correcte = false,
                    Missatge = "Usuari no trobat"
                };
            }

            var hasher = new PasswordHasher<Usuari>();

            var resultat = hasher.VerifyHashedPassword(
                usuari,
                usuari.PasswordHash,
                password);

            if (resultat == PasswordVerificationResult.Failed)
            {
                return new LoginResponseDto
                {
                    Correcte = false,
                    Missatge = "Contrasenya incorrecta"
                };
            }

            return new LoginResponseDto
            {
                Correcte = true,
                Missatge = "Login correcte",
                UsuariPK = usuari.UsuariPK,
                Nom = usuari.Nom,
                Email = usuari.Email,
                Pla = ObtenirResumPlaUsuari(usuari.UsuariPK)
            };
        }

        /// <summary>
        /// Explicació: crea una sala nova per a un usuari i l'afegeix com a creador amb tots els permisos.
        /// Precondicions: l'usuari ha d'existir i no ha de tenir ja una sala amb el mateix nom.
        /// Postcondicions: la sala i la relació usuari-sala queden persistides; si falla la notificació per correu, la sala igualment es manté creada.
        /// </summary>
        public async Task<Sala> CrearSala(string UsuariPK, string NomSala)
        {
            var existeixUsuari = await _context.Usuaris
                .FirstOrDefaultAsync(w => w.UsuariPK == UsuariPK);

            if (existeixUsuari == null)
            {
                return new Sala()
                {
                    SalaPK = "",
                    Nom = "L'usuari no existeix"
                };
            }

            var existeixSala = await _context.Sales
                .FirstOrDefaultAsync(s => s.UsuariCreadorPK == UsuariPK && s.Nom == NomSala);

            if (existeixSala != null)
            {
                return new Sala()
                {
                    SalaPK = "",
                    Nom = NomSala,
                };
            }

            var plaActiu = await ObtenirPlaActiuAmbLimitsUsuariAsync(UsuariPK);
            if (plaActiu == null)
            {
                throw new InvalidOperationException("Has de seleccionar un pla abans de crear sales.");
            }

            var salesCreadesActuals = await ComptarSalesCreadesUsuariAsync(UsuariPK);
            if (salesCreadesActuals >= plaActiu.Value.Pla.LimitSales)
            {
                throw new InvalidOperationException($"Has arribat al límit de {plaActiu.Value.Pla.LimitSales} sala(es) del teu pla.");
            }

            var salaPk = Guid.NewGuid().ToString("N");

            var sala = new Sala
            {
                SalaPK = salaPk,
                Nom = NomSala,
                UsuariCreadorPK = UsuariPK,
                TokenAcces = Guid.NewGuid().ToString("N")[..12],
                DataCreacio = DateTime.UtcNow,
                DataExpiracio = DateTime.UtcNow.AddDays(3),
                Activa = true,
                DataAvisExpiracioEnviat = null,
            };

            var salaUsuari = new SalaUsuari
            {
                SalaUsuariPK = Guid.NewGuid().ToString("N"),
                SalaPK = salaPk,
                UsuariPK = UsuariPK,
                Rol = "Creador",
                PotVeure = true,
                PotPujar = true,
                PotDescarregar = true,
                PotEliminarPropies = true,
                PotEliminarQualsevol = true,
                PotGestionarSala = true,
                DataUnio = DateTime.UtcNow
            };

            _context.Sales.Add(sala);
            _context.SalaUsuaris.Add(salaUsuari);
            plaActiu.Value.UsuariPla.SalesCreades = salesCreadesActuals + 1;

            await _context.SaveChangesAsync();

            try
            {
                await _serveiNotificacions.EnviarCorreuAsync(
                    existeixUsuari.Email,
                    "Sala creada",
                    $"La sala {NomSala} ha estat creada."
                );
            }
            catch
            {
                // No fem fallar la creació de la sala si falla el correu.
            }

            return sala;
        }

        /// <summary>
        /// Explicació: obté les sales associades a un usuari amb paginació i ordenació.
        /// Precondicions: l'identificador d'usuari ha d'estar informat; els paràmetres de pàgina i quantitat han de representar una consulta vàlida.
        /// Postcondicions: retorna les relacions sala-usuari corresponents a la pàgina demanada.
        /// </summary>
        public async Task<SalaUsuari[]> obtenirSalesUsuari(string usuariPK, int pagina = 1, int quantitat = 20, string ordre = "data", bool descendent = false)
        {

            var salasUsuari =  _context.SalaUsuaris.Where( w=> w.UsuariPK == usuariPK ).Include( i=> i.Sala).ThenInclude( ti => ti.Usuari).AsQueryable();
            ;

            salasUsuari = ordre switch
            {
                "nom" => descendent
                    ? salasUsuari.OrderByDescending(su => su.Sala.Nom)
                    : salasUsuari.OrderBy(su => su.Sala.Nom),

                "data" => descendent
                    ? salasUsuari.OrderByDescending(su => su.Sala.DataCreacio)
                    : salasUsuari.OrderBy(su => su.Sala.DataCreacio),

                _ => descendent
                    ? salasUsuari.OrderByDescending(su => su.Sala.DataCreacio)
                    : salasUsuari.OrderBy(su => su.Sala.DataCreacio)
            };

            return await salasUsuari.Skip((pagina - 1 ) * quantitat).Take(quantitat).ToArrayAsync();

        }

        /// <summary>
        /// Explicació: elimina sales i les seves relacions amb usuaris a partir d'una llista d'identificadors.
        /// Precondicions: l'identificador d'usuari ha d'arribar informat i la llista de sales no hauria d'estar buida.
        /// Postcondicions: les sales trobades i les relacions associades queden marcades per eliminar i es desa el canvi a la base de dades.
        /// </summary>
        public async Task<Boolean> eliminarSalas(string usuariPK, string[] salaPks)
        {

            var llistaSalesEliminar = await _context.Sales.Where(w => salaPks.Contains(w.SalaPK)).ToListAsync();

            var llistaSalesUsuariEliminar = await _context.SalaUsuaris.Where(w => salaPks.Contains(w.SalaPK)).ToListAsync();

            _context.Sales.RemoveRange(llistaSalesEliminar);
            _context.SalaUsuaris.RemoveRange(llistaSalesUsuariEliminar);

            _context.SaveChanges();

            await SincronitzarUsPlaDespresCanvisAsync(llistaSalesEliminar.Select(s => s.UsuariCreadorPK).Distinct().ToArray());

            return true;

        }

        /// <summary>
        /// Explicació: obté tots els plans actius disponibles per als usuaris.
        /// Precondicions: la taula de plans ha d'estar creada i poblada.
        /// Postcondicions: retorna els plans actius ordenats per preu i límit de sales.
        /// </summary>
        public async Task<Pla[]> ObtenirPlansActius()
        {
            return await _context.Plans
                .Where(pla => pla.Actiu)
                .OrderBy(pla => pla.Preu)
                .ThenBy(pla => pla.LimitSales)
                .ToArrayAsync();
        }

        /// <summary>
        /// Explicació: obté el pla actiu associat a un usuari.
        /// Precondicions: l'identificador d'usuari ha d'arribar informat.
        /// Postcondicions: retorna la relació activa usuari-pla o null si l'usuari encara no té pla.
        /// </summary>
        public async Task<UsuariPla?> ObtenirPlaActiuUsuari(string usuariPK)
        {
            return await _context.UsuariPlans
                .Where(usuariPla => usuariPla.UsuariPK == usuariPK && usuariPla.Actiu)
                .OrderByDescending(usuariPla => usuariPla.DataInici)
                .FirstOrDefaultAsync();
        }

        /// <summary>
        /// Explicació: selecciona un pla per a l'usuari, tancant qualsevol pla actiu anterior.
        /// Precondicions: l'usuari i el pla han d'existir i el pla ha d'estar actiu.
        /// Postcondicions: l'usuari queda amb una única relació de pla activa.
        /// </summary>
        public async Task<UsuariPla?> SeleccionarPlaUsuari(string usuariPK, string plaPK)
        {
            var usuariExisteix = await _context.Usuaris
                .AnyAsync(usuari => usuari.UsuariPK == usuariPK && usuari.Actiu);

            if (!usuariExisteix)
            {
                return null;
            }

            var plaExisteix = await _context.Plans
                .AnyAsync(pla => pla.PlaPK == plaPK && pla.Actiu);

            if (!plaExisteix)
            {
                return null;
            }

            var plaActiuActual = await ObtenirPlaActiuUsuari(usuariPK);

            if (plaActiuActual != null && plaActiuActual.PlaPK == plaPK)
            {
                return plaActiuActual;
            }

            var ara = DateTime.UtcNow;
            var plansActius = await _context.UsuariPlans
                .Where(usuariPla => usuariPla.UsuariPK == usuariPK && usuariPla.Actiu)
                .ToListAsync();

            foreach (var plaActiu in plansActius)
            {
                plaActiu.Actiu = false;
                plaActiu.DataFi = ara;
            }

            var resumUsage = await CalcularUsageUsuariAsync(usuariPK);

            var usuariPlaNou = new UsuariPla
            {
                UsuariPlaPK = Guid.NewGuid().ToString("N"),
                UsuariPK = usuariPK,
                PlaPK = plaPK,
                DataInici = ara,
                DataFi = null,
                Actiu = true,
                EspaiConsumitBytes = resumUsage.EspaiConsumitBytes,
                SalesCreades = resumUsage.SalesCreades,
                ImatgesPujades = resumUsage.ImatgesPujades
            };

            _context.UsuariPlans.Add(usuariPlaNou);

            await _context.SaveChangesAsync();

            return usuariPlaNou;
        }

        /// <summary>
        /// Explicació: obté un resum sincronitzat del pla actiu i de l'ús actual d'un usuari.
        /// Precondicions: l'usuari pot tenir o no tenir un pla actiu; l'identificador ha d'estar informat.
        /// Postcondicions: retorna límits, consum i disponibilitat del pla actiu, o null si no n'hi ha cap.
        /// </summary>
        public ResumPlaUsuariDto? ObtenirResumPlaUsuari(string usuariPK)
        {
            var plaActiu = (
                from usuariPla in _context.UsuariPlans
                join pla in _context.Plans on usuariPla.PlaPK equals pla.PlaPK
                where usuariPla.UsuariPK == usuariPK && usuariPla.Actiu && pla.Actiu
                orderby usuariPla.DataInici descending
                select new { UsuariPla = usuariPla, Pla = pla }
            ).FirstOrDefault();

            if (plaActiu == null)
            {
                return null;
            }

            var salesCreades = _context.Sales
                .Count(sala => sala.UsuariCreadorPK == usuariPK && sala.Activa);

            var consultaImatges = _context.SalaImatges
                .Where(salaImatge =>
                    salaImatge.Sala.UsuariCreadorPK == usuariPK &&
                    salaImatge.Sala.Activa)
                .Select(salaImatge => salaImatge.Imatge);

            var espaiConsumitBytes = consultaImatges
                .Sum(imatge => (long?)imatge.MidaBytes) ?? 0;

            var imatgesPujades = consultaImatges.Count();

            return new ResumPlaUsuariDto
            {
                PlaPK = plaActiu.Pla.PlaPK,
                Nom = plaActiu.Pla.Nom,
                LimitEmmagatzematgeBytes = plaActiu.Pla.LimitEmmagatzematgeBytes,
                LimitSales = plaActiu.Pla.LimitSales,
                LimitImatges = plaActiu.Pla.LimitImatges,
                EspaiConsumitBytes = espaiConsumitBytes,
                SalesCreades = salesCreades,
                ImatgesPujades = imatgesPujades,
                PotCrearSala = salesCreades < plaActiu.Pla.LimitSales,
                BytesDisponibles = Math.Max(0, plaActiu.Pla.LimitEmmagatzematgeBytes - espaiConsumitBytes)
            };
        }

        /// <summary>
        /// Explicació: obté de manera asíncrona el resum sincronitzat del pla actiu i de l'ús actual d'un usuari.
        /// Precondicions: l'usuari pot tenir o no tenir un pla actiu; l'identificador ha d'estar informat.
        /// Postcondicions: retorna límits, consum i disponibilitat del pla actiu, o null si no n'hi ha cap.
        /// </summary>
        public async Task<ResumPlaUsuariDto?> ObtenirResumPlaUsuariAsync(string usuariPK)
        {
            var plaActiu = await ObtenirPlaActiuAmbLimitsUsuariAsync(usuariPK);
            if (plaActiu == null)
            {
                return null;
            }

            var usage = await CalcularUsageUsuariAsync(usuariPK);

            return new ResumPlaUsuariDto
            {
                PlaPK = plaActiu.Value.Pla.PlaPK,
                Nom = plaActiu.Value.Pla.Nom,
                LimitEmmagatzematgeBytes = plaActiu.Value.Pla.LimitEmmagatzematgeBytes,
                LimitSales = plaActiu.Value.Pla.LimitSales,
                LimitImatges = plaActiu.Value.Pla.LimitImatges,
                EspaiConsumitBytes = usage.EspaiConsumitBytes,
                SalesCreades = usage.SalesCreades,
                ImatgesPujades = usage.ImatgesPujades,
                PotCrearSala = usage.SalesCreades < plaActiu.Value.Pla.LimitSales,
                BytesDisponibles = Math.Max(0, plaActiu.Value.Pla.LimitEmmagatzematgeBytes - usage.EspaiConsumitBytes)
            };
        }

        /// <summary>
        /// Explicació: localitza el pla gratuït actiu que s'assigna per defecte als usuaris nous.
        /// Precondicions: la taula de plans ha d'estar poblada; pot no existir cap pla gratuït actiu.
        /// Postcondicions: retorna el pla gratuït amb menys límit de sales o null si no n'hi ha cap.
        /// </summary>
        private Pla? ObtenirPlaGratuitActiu()
        {
            return _context.Plans
                .Where(pla => pla.Actiu && pla.Preu == 0)
                .OrderBy(pla => pla.LimitSales)
                .FirstOrDefault();
        }

        /// <summary>
        /// Explicació: obté conjuntament la relació de pla activa d'un usuari i els límits del pla associat.
        /// Precondicions: l'identificador d'usuari ha d'estar informat.
        /// Postcondicions: retorna la parella usuari-pla i pla si existeix un pla actiu, o null en cas contrari.
        /// </summary>
        private async Task<(UsuariPla UsuariPla, Pla Pla)?> ObtenirPlaActiuAmbLimitsUsuariAsync(string usuariPK)
        {
            var plaActiu = await (
                from usuariPla in _context.UsuariPlans
                join pla in _context.Plans on usuariPla.PlaPK equals pla.PlaPK
                where usuariPla.UsuariPK == usuariPK && usuariPla.Actiu && pla.Actiu
                orderby usuariPla.DataInici descending
                select new { UsuariPla = usuariPla, Pla = pla }
            ).FirstOrDefaultAsync();

            return plaActiu == null
                ? null
                : (plaActiu.UsuariPla, plaActiu.Pla);
        }

        /// <summary>
        /// Explicació: compta les sales actives creades per un usuari.
        /// Precondicions: l'identificador d'usuari ha d'estar informat.
        /// Postcondicions: retorna el nombre de sales actives on l'usuari és creador.
        /// </summary>
        private async Task<int> ComptarSalesCreadesUsuariAsync(string usuariPK)
        {
            return await _context.Sales
                .CountAsync(sala => sala.UsuariCreadorPK == usuariPK && sala.Activa);
        }

        /// <summary>
        /// Explicació: calcula l'ús actual que consumeix el pla d'un usuari segons les seves sales actives.
        /// Precondicions: l'identificador d'usuari ha d'estar informat.
        /// Postcondicions: retorna sales creades, bytes consumits i imatges associades a les sales actives creades per l'usuari.
        /// </summary>
        private async Task<UsagePlaUsuari> CalcularUsageUsuariAsync(string usuariPK)
        {
            var consultaImatges = _context.SalaImatges
                .Where(salaImatge =>
                    salaImatge.Sala.UsuariCreadorPK == usuariPK &&
                    salaImatge.Sala.Activa)
                .Select(salaImatge => salaImatge.Imatge);

            return new UsagePlaUsuari
            {
                SalesCreades = await ComptarSalesCreadesUsuariAsync(usuariPK),
                EspaiConsumitBytes = await consultaImatges.SumAsync(imatge => (long?)imatge.MidaBytes) ?? 0,
                ImatgesPujades = await consultaImatges.CountAsync()
            };
        }

        /// <summary>
        /// Explicació: sincronitza els comptadors del pla actiu després de canvis en sales o imatges.
        /// Precondicions: la llista d'usuaris pot estar buida; cada usuari pot tenir o no tenir pla actiu.
        /// Postcondicions: els plans actius existents queden actualitzats amb l'ús recalculat.
        /// </summary>
        private async Task SincronitzarUsPlaDespresCanvisAsync(string[] usuariPks)
        {
            foreach (var usuariPK in usuariPks)
            {
                var plaActiu = await ObtenirPlaActiuUsuari(usuariPK);
                if (plaActiu == null)
                {
                    continue;
                }

                var usage = await CalcularUsageUsuariAsync(usuariPK);
                plaActiu.SalesCreades = usage.SalesCreades;
                plaActiu.EspaiConsumitBytes = usage.EspaiConsumitBytes;
                plaActiu.ImatgesPujades = usage.ImatgesPujades;
            }

            await _context.SaveChangesAsync();
        }
    }

}
public class ResumPlaUsuariDto
{
    public string PlaPK { get; set; }

    public string Nom { get; set; }

    public long LimitEmmagatzematgeBytes { get; set; }

    public int LimitSales { get; set; }

    public int LimitImatges { get; set; }

    public long EspaiConsumitBytes { get; set; }

    public int SalesCreades { get; set; }

    public int ImatgesPujades { get; set; }

    public bool PotCrearSala { get; set; }

    public long BytesDisponibles { get; set; }
}

public class UsagePlaUsuari
{
    public long EspaiConsumitBytes { get; set; }

    public int SalesCreades { get; set; }

    public int ImatgesPujades { get; set; }
}

public class LoginResponseDto
{
    public bool Correcte { get; set; }

    public string Missatge { get; set; }

    public string UsuariPK { get; set; }

    public string Nom { get; set; }

    public string Email { get; set; }

    public ResumPlaUsuariDto? Pla { get; set; }
}
