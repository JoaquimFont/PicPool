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
        public ServeiUsuaris(PicPoolDbContext context, ServeiNotificacions serveiNotificacions)
        {
            _context = context;
            _serveiNotificacions = serveiNotificacions;

        }

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

        public async Task<Boolean> eliminarSalas(string usuariPK, string[] salaPks)
        {

            var llistaSalesEliminar = await _context.Sales.Where(w => salaPks.Contains(w.SalaPK)).ToListAsync();

            var llistaSalesUsuariEliminar = await _context.SalaUsuaris.Where(w => salaPks.Contains(w.SalaPK)).ToListAsync();

            _context.Sales.RemoveRange(llistaSalesEliminar);
            _context.SalaUsuaris.RemoveRange(llistaSalesUsuariEliminar);

            _context.SaveChanges();

            return true;

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