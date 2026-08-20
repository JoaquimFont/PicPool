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

        /// <summary>
        /// Explicació: inicialitza el controlador d'usuaris amb el servei que concentra la lògica d'usuaris i sales personals.
        /// Precondicions: el contenidor d'injecció de dependències ha de proporcionar una instància vàlida de <see cref="ServeiUsuaris"/>.
        /// Postcondicions: el controlador queda preparat per delegar les operacions d'usuari al servei corresponent.
        /// </summary>
        public UsuariController(ServeiUsuaris serveiUsuaris)
        {
            _serveiUsuaris = serveiUsuaris;
        }

        /// <summary>
        /// Explicació: crea un usuari nou a partir de les dades rebudes pel cos de la petició i retorna el pla assignat.
        /// Precondicions: el DTO ha de contenir nom, email i contrasenya; el nom i l'email no haurien d'existir prèviament i ha d'existir un pla gratuït actiu.
        /// Postcondicions: retorna una resposta correcta amb l'usuari creat i el seu pla gratuït o una resposta d'error si no es pot crear.
        /// </summary>
        [HttpPost]
        public IActionResult CrearUsuari([FromBody] CrearUsuariDto dto)
        {
            PicPool.Domain.Entities.Usuari usuari;

            try
            {
                usuari = _serveiUsuaris.CrearUsuari(dto.Nom, dto.Email, dto.Password);
            }
            catch (InvalidOperationException ex)
            {
                return BadRequest(new
                {
                    correcte = false,
                    missatge = ex.Message
                });
            }

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
                usuari,
                pla = _serveiUsuaris.ObtenirResumPlaUsuari(usuari.UsuariPK)
            });
        }

        /// <summary>
        /// Explicació: valida les credencials d'un usuari i retorna el resultat de l'inici de sessió.
        /// Precondicions: el DTO ha d'incloure nom d'usuari i contrasenya.
        /// Postcondicions: retorna la resposta del servei indicant si el login és correcte i, si escau, les dades bàsiques de l'usuari.
        /// </summary>
        [HttpPost("login")]
        public IActionResult Login([FromBody] LoginRequestDto dto)
        {
            var resposta = _serveiUsuaris.Login(dto.Username, dto.Password);

            return Ok(resposta);
        }


        /// <summary>
        /// Explicació: crea una sala associada a un usuari existent.
        /// Precondicions: el DTO ha d'incloure l'identificador de l'usuari creador i el nom de la sala.
        /// Postcondicions: retorna la sala creada o una resposta d'error si falta l'usuari o la sala ja existeix/no es pot crear.
        /// </summary>
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

            PicPool.Domain.Entities.Sala sala;

            try
            {
                sala = await _serveiUsuaris.CrearSala(dto.UsuariPK, dto.NomSala);
            }
            catch (InvalidOperationException ex)
            {
                return BadRequest(new
                {
                    correcte = false,
                    missatge = ex.Message
                });
            }

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
                },
                pla = await _serveiUsuaris.ObtenirResumPlaUsuariAsync(dto.UsuariPK)
            });
        }

        /// <summary>
        /// Explicació: obté les sales vinculades a un usuari aplicant paginació i ordenació.
        /// Precondicions: el DTO ha d'incloure l'identificador d'usuari i els paràmetres de consulta esperats.
        /// Postcondicions: retorna una llista de sales transformada a DTOs de resposta.
        /// </summary>
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

        /// <summary>
        /// Explicació: elimina una o més sales indicades per un usuari.
        /// Precondicions: el DTO ha d'incloure l'identificador de l'usuari i com a mínim una sala a eliminar.
        /// Postcondicions: retorna el resultat de l'eliminació o una resposta d'error si falten dades obligatòries.
        /// </summary>
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

        /// <summary>
        /// Explicació: obté els plans actius i el pla actual de l'usuari indicat.
        /// Precondicions: el DTO ha d'incloure l'identificador de l'usuari.
        /// Postcondicions: retorna la llista de plans disponibles i, si existeix, el pla actiu de l'usuari.
        /// </summary>
        [HttpPost("obtenirPlansUsuari")]
        public async Task<IActionResult> ObtenirPlansUsuari([FromBody] ObtenirPlansUsuariRequestDto dto)
        {
            if (string.IsNullOrWhiteSpace(dto.UsuariPK))
            {
                return BadRequest(new ObtenirPlansUsuariResponseDto
                {
                    Correcte = false,
                    Missatge = "L'usuari és obligatori.",
                    Plans = []
                });
            }

            var plans = await _serveiUsuaris.ObtenirPlansActius();
            var plaActual = await _serveiUsuaris.ObtenirPlaActiuUsuari(dto.UsuariPK);

            return Ok(new ObtenirPlansUsuariResponseDto
            {
                Correcte = true,
                Missatge = "Plans obtinguts correctament.",
                PlaActualPK = plaActual?.PlaPK,
                PlaActual = await _serveiUsuaris.ObtenirResumPlaUsuariAsync(dto.UsuariPK),
                Plans = plans.Select(pla => new PlaDto
                {
                    PlaPK = pla.PlaPK,
                    Nom = pla.Nom,
                    LimitEmmagatzematgeBytes = pla.LimitEmmagatzematgeBytes,
                    LimitSales = pla.LimitSales,
                    LimitImatges = pla.LimitImatges,
                    Preu = pla.Preu,
                    Actiu = pla.Actiu
                }).ToArray()
            });
        }

        /// <summary>
        /// Explicació: selecciona el pla actual per a l'usuari indicat.
        /// Precondicions: el DTO ha d'incloure identificador d'usuari i identificador de pla.
        /// Postcondicions: l'usuari queda vinculat al pla seleccionat com a pla actiu.
        /// </summary>
        [HttpPost("seleccionarPlaUsuari")]
        public async Task<IActionResult> SeleccionarPlaUsuari([FromBody] SeleccionarPlaUsuariRequestDto dto)
        {
            if (string.IsNullOrWhiteSpace(dto.UsuariPK) || string.IsNullOrWhiteSpace(dto.PlaPK))
            {
                return BadRequest(new SeleccionarPlaUsuariResponseDto
                {
                    Correcte = false,
                    Missatge = "L'usuari i el pla són obligatoris."
                });
            }

            var plaSeleccionat = await _serveiUsuaris.SeleccionarPlaUsuari(dto.UsuariPK, dto.PlaPK);

            if (plaSeleccionat == null)
            {
                return BadRequest(new SeleccionarPlaUsuariResponseDto
                {
                    Correcte = false,
                    Missatge = "No s'ha pogut seleccionar el pla."
                });
            }

            return Ok(new SeleccionarPlaUsuariResponseDto
            {
                Correcte = true,
                Missatge = "Pla seleccionat correctament.",
                PlaActualPK = plaSeleccionat.PlaPK,
                PlaActual = await _serveiUsuaris.ObtenirResumPlaUsuariAsync(dto.UsuariPK)
            });
        }


    }
}
