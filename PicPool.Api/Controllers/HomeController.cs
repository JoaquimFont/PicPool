using Microsoft.AspNetCore.Mvc;
using PicPool.Api.Usuari.DTOs;
using PicPool.Infrastructure.Services;
using PicPool.Domain.Entities;
using System.Threading.Tasks;
using PicPool.Api.Sala.DTOs;

namespace PicPool.Api.Controllers
{
    [ApiController]
    [Route("api/usuari")]
    public class UsuariController : ControllerBase
    {
        private readonly ServeiUsuaris _serveiUsuaris;

        public UsuariController(ServeiUsuaris serveiUsuaris)
        {
            _serveiUsuaris = serveiUsuaris;
        }

        [HttpPost]
        public IActionResult CrearUsuari([FromBody] CrearUsuariDto dto)
        {
            var usuari = _serveiUsuaris.CrearUsuari(dto.Nom, dto.Email, dto.Password);

            if (string.IsNullOrEmpty(usuari?.UsuariPK))
            {
                var motiu = usuari?.Nom == dto.Nom ? "Ja existeix un usuari amb aquest nom" : usuari?.Email == dto.Email ? "Ja existeix un usuari amb aquest email" : " No es pot crear l'usuari";
                return BadRequest(new
                {
                    correcte = false,
                    missatge = motiu
                });
            }

            return Ok(new
            {
                correcte = true,
                missatge = "Usuari creat correctament.",
                usuari
            });
        }

        [HttpPost("login")]
        public IActionResult Login([FromBody] LoginRequestDto dto)
        {
            var resposta = _serveiUsuaris.Login(dto.Username, dto.Password);

            return Ok(resposta);
        }


        [HttpPost("crearSala")]
        public async Task<IActionResult> CrearSala([FromBody] CrearSalaRequestDto dto)
        {

            if (string.IsNullOrEmpty(dto.UsuariPK))
            {
                var motiu = "Falta l'identificador de l'usuari per crear la sala";
                return BadRequest(new
                {
                    correcte = false,
                    missatge = motiu
                });
            }

           var sala = await _serveiUsuaris.CrearSala(dto.UsuariPK, dto.NomSala);

            if (string.IsNullOrEmpty(sala.SalaPK))
            {
                var motiu = sala.Nom == dto.NomSala ? "Ja existeix una sala amb aquest nom a la teva area personal" : "La sala no es pot crear";
                return BadRequest(new
                {
                    correcte = false,
                    missatge = motiu
                });
            }

            var resposta = new CrearSalaResponseDto()
            {
                Correcte = sala != null,
                SalaPK = sala?.SalaPK?? "",
                NomSala = dto.NomSala
            };

            return Ok(new
            {
                correcte = true,
                missatge = "Sala creada correctament",
                sala = new obtenirSalaDto()
                {
                    SalaPK = sala?.SalaPK,
                    Nom = sala?.Nom,
                    TokenAcces = sala?.TokenAcces,
                    UsuariCreador = new usuarioDto
                    {
                        Nom = sala.Usuari.UsuariPK,
                        UsuariPK = "",
                        Email = ""
                    },
                    DataCreacio = sala.DataCreacio,
                    DataExpiracio = sala.DataExpiracio,
                    Activa = sala.Activa
                }
            });
        }

        [HttpPost("obtenirSalesUsuari")]
        public async Task<IActionResult> ObtenirSalesUsuari([FromBody] ObtenirSalesUsuariRequestDto dto)
        {
            var llistaSales = await _serveiUsuaris.obtenirSalesUsuari(dto.UsuariPK, dto.Pagina, dto.Quantitat, dto.Ordre, dto.Descendent);

            var resposta = new ObtenirSalesUsuariResponseDto()
            {
                Correcte = true,
                Salas = llistaSales.Select(i => new obtenirSalaDto
                {
                    SalaPK = i.Sala.SalaPK,
                    Nom = i.Sala.Nom,
                    TokenAcces = i.Sala.TokenAcces,
                    UsuariCreador = new usuarioDto
                    {
                        Nom = i.Sala.Usuari.Nom,
                        UsuariPK = i.Sala.Usuari.UsuariPK,
                        Email = i.Sala.Usuari.Email
                    },
                    DataCreacio = i.Sala.DataCreacio,
                    DataExpiracio = i.Sala.DataExpiracio,
                    Activa = i.Sala.Activa,
                    TotalImatges = i.Sala.TotalImatges,
                    PesTotal = i.Sala.PesTotal
                }).ToArray()
            };

            return Ok(resposta);
            
        }

        [HttpPost("eliminarSalesUsuari")]
        public async Task<IActionResult> EliminarSalesUsuari([FromBody] EliminarSalesUsuariRequestDto dto)
        {

            if (string.IsNullOrEmpty(dto.UsuariPK))
            {
                return BadRequest("L'usuari és obligatori.");

            }

            if (dto.SalaPks.Length == 0)
            {
                return BadRequest("No hi ha sales a eliminar");
            }

            var eliminarSales = await _serveiUsuaris.eliminarSalas(dto.UsuariPK, dto.SalaPks);
            return Ok(eliminarSales);

        }


    }
}
