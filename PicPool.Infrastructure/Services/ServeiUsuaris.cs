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
        /// Explicació: crea un usuari nou amb contrasenya hashejada.
        /// Precondicions: el nom, email i password han de ser valors vàlids i no hauria d'existir un usuari amb el mateix nom o email.
        /// Postcondicions: si no hi ha duplicats, l'usuari queda guardat; si ja existeix, retorna un usuari marcador sense clau.
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

            _context.Usuaris.Add(usuari);
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
                Email = usuari.Email
            };
        }

        /// <summary>
        /// Explicació: crea una sala nova per a un usuari i l'afegeix com a creador amb tots els permisos.
        /// Precondicions: l'usuari ha d'existir i no ha de tenir ja una sala amb el mateix nom.
        /// Postcondicions: la sala i la relació usuari-sala queden persistides; si falla la notificació per correu, la sala igualment es manté creada.
        /// </summary>
        public async Task<Sala> CrearSala(string UsuariPK, string NomSala)
        {
            var existeixSala = await _context.Sales
                .FirstOrDefaultAsync(s => s.Usuari.UsuariPK == UsuariPK && s.Nom == NomSala);

            if (existeixSala != null)
            {
                return new Sala()
                {
                    SalaPK = "",
                    Nom = existeixSala.SalaPK,
                };
            }

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

            var usuariPlaNou = new UsuariPla
            {
                UsuariPlaPK = Guid.NewGuid().ToString("N"),
                UsuariPK = usuariPK,
                PlaPK = plaPK,
                DataInici = ara,
                DataFi = null,
                Actiu = true,
                EspaiConsumitBytes = 0,
                SalesCreades = 0,
                ImatgesPujades = 0
            };

            _context.UsuariPlans.Add(usuariPlaNou);

            await _context.SaveChangesAsync();

            return usuariPlaNou;
        }
    }

}
public class LoginResponseDto
{
    public bool Correcte { get; set; }

    public string Missatge { get; set; }

    public string UsuariPK { get; set; }

    public string Nom { get; set; }

    public string Email { get; set; }
}
