using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.SignalR;
using Microsoft.Net.Http.Headers;
using PicPool.Api.Hubs;
using PicPool.Api.Sala.DTOs;
using PicPool.Api.Usuari.DTOs;
using PicPool.Domain.Entities;
using PicPool.Infrastructure.Services;
using System.Threading;
using static System.Net.Mime.MediaTypeNames;

namespace PicPool.Api.Controllers
{
    [ApiController]
    [Route("api/sala")]
    public class SalaController : ControllerBase
    {
        private readonly ServeiSala _serveiSala;
        private readonly SalaOperacioCua _salaOperacioCua;
        private readonly IHubContext<SalaHub> _salaHub;
        /// <summary>
        /// Explicació: inicialitza el controlador de sales amb el servei de domini, la cua d'operacions i el hub SignalR.
        /// Precondicions: la injecció de dependències ha de proporcionar instàncies vàlides dels tres serveis.
        /// Postcondicions: el controlador pot gestionar imatges, links compartits i notificacions en temps real.
        /// </summary>
        public SalaController(ServeiSala serveiSala, SalaOperacioCua salaOperacioCua, IHubContext<SalaHub> salaHub)
        {
            _serveiSala = serveiSala;
            _salaOperacioCua = salaOperacioCua;
            _salaHub = salaHub;
        }

        /// <summary>
        /// Explicació: puja una o més imatges a una sala i notifica els clients connectats quan la sala s'actualitza.
        /// Precondicions: la petició ha d'incloure sala, usuari i com a mínim una imatge; la sala i l'usuari han de ser vàlids.
        /// Postcondicions: les imatges queden persistides si l'operació és correcta i s'envia un esdeveniment SignalR al grup de la sala.
        /// </summary>
        [HttpPost("pujarImatges")]
        public async Task<IActionResult> PujarImatges([FromForm] PujarImatgesRequestDto dto, CancellationToken cancellationToken)
        {
            if (string.IsNullOrWhiteSpace(dto.SalaPk))
            {
                return BadRequest("La sala és obligatòria.");
            }

            if (string.IsNullOrWhiteSpace(dto.UserPk))
            {
                return BadRequest("L'usuari és obligatori.");
            }

            if (dto.Imatges == null || dto.Imatges.Length == 0)
            {
                return BadRequest("No s'ha rebut cap imatge.");
            }

            try
            {
                var resposta = await _salaOperacioCua.ExecutarEnCuaAsync(
                    dto.SalaPk,
                    async () =>
                    {
                        foreach (var imatgeDto in dto.Imatges)
                        {
                            await using var stream = imatgeDto.File.OpenReadStream();

                            await _serveiSala.PujarImatge(
                                salaPK: dto.SalaPk,
                                stream: stream,
                                fileName: imatgeDto.File.FileName,
                                contentType: imatgeDto.File.ContentType,
                                midaBytes: imatgeDto.File.Length,
                                resolucio: imatgeDto.Resolucio,
                                descripcio: imatgeDto.Descripcio,
                                propietari: dto.UserPk
                            );
                        }

                        await _salaHub.Clients
                            .Group($"sala-{dto.SalaPk}")
                            .SendAsync("ImatgesSalaActualitzades", new
                            {
                                salaPk = dto.SalaPk,
                                usuariPk = dto.UserPk,
                                operacio = "pujada",
                                dataUtc = DateTime.UtcNow
                            }, cancellationToken);

                        return new PujarImatgesRespostaDto
                        {
                            Correcte = true,
                            Missatge = "Imatges pujades correctament."
                        };
                    },
                    async () =>
                    {
                        if (!string.IsNullOrWhiteSpace(dto.SignalRConnectionId))
                        {
                            await _salaHub.Clients
                                .Client(dto.SignalRConnectionId)
                                .SendAsync("OperacioSalaEnCua", new
                                {
                                    salaPk = dto.SalaPk,
                                    missatge = "Un altre usuari està pujant o eliminant imatges en aquest moment. Quan acabi, les teves accions es realitzaran."
                                }, cancellationToken);
                        }
                    },
                    cancellationToken
                );

                return Ok(resposta);
            }
            catch (InvalidOperationException ex)
            {
                return Ok(new PujarImatgesRespostaDto
                {
                    Correcte = false,
                    Missatge = ex.Message
                });
            }
        }

        /// <summary>
        /// Explicació: obté les imatges d'una sala amb paginació i ordenació, juntament amb metadades de recompte.
        /// Precondicions: el DTO ha d'incloure identificador de sala i identificador d'usuari.
        /// Postcondicions: retorna les imatges de la sala en format DTO i la informació auxiliar necessària per a la UI.
        /// </summary>
        [HttpPost("obtenirImatgesSala")]
        public async Task<IActionResult> ObtenirImatgesSala([FromBody] ObtenirImatgesSalaRequestDto dto)
        {
            if (string.IsNullOrWhiteSpace(dto.SalaPK))
            {
                return BadRequest("La sala és obligatòria.");
            }

            if (string.IsNullOrWhiteSpace(dto.UsuariPK))
            {
                return BadRequest("L'usuari és obligatori.");
            }

            var imatges = await _serveiSala.ObtenirImatgesSala(
                salaPK: dto.SalaPK,
                usuariPK: dto.UsuariPK,
                pagina: dto.Pagina,
                quantitat: dto.Quantitat,
                ordre: dto.Ordre,
                descendent: dto.Descendent
            );

            var totalImatges = await _serveiSala.ComptarImatgesSalaAsync(dto.SalaPK);
            var imatgePksExistents = await _serveiSala.ObtenirImatgePksExistentsSalaAsync(dto.SalaPK);

            var resposta = new ObtenirImatgesSalaResponseDto
            {
                Correcte = true,
                TotalImatges = totalImatges,
                ImatgePksExistents = imatgePksExistents,
                Imatges = imatges.Select(i => new ImatgeSalaDto
                {
                    ImatgePK = i.ImatgePK,
                    Nom = i.NomOriginal,
                    SourceUrl = i.SourceUrl,
                    MidaBytes = i.MidaBytes,
                    TipusMime = i.TipusMime,
                    UsuariCreador = new UsuariDto
                    {
                        UsuariPK = i.Usuari.UsuariPK,
                        Nom = i.Usuari.Nom,
                        Email = i.Usuari.Email
                    },
                    DataPujada = i.DataPujada
                }).ToArray()
            };

            return Ok(resposta);
        }

        /// <summary>
        /// Explicació: elimina imatges d'una sala i notifica els clients connectats quan hi ha canvis.
        /// Precondicions: el DTO ha d'incloure sala, usuari i com a mínim una imatge a eliminar.
        /// Postcondicions: si l'eliminació és correcta, les imatges deixen d'estar associades a la sala i s'emet una notificació SignalR.
        /// </summary>
        [HttpPost("eliminarImatgesSala")]
        public async Task<IActionResult> EliminarImatgesSala([FromBody] EliminarImatgesSalaRequestDto dto, CancellationToken cancellationToken)
        {
            if (string.IsNullOrEmpty(dto.SalaPk))
            {
                return BadRequest("La sala és obligatòria.");
            }

            if (string.IsNullOrEmpty(dto.UsuariPK))
            {
                return BadRequest("L'usuari és obligatori.");
            }

            if (dto.ImatgePks == null || dto.ImatgePks.Length == 0)
            {
                return BadRequest("No hi ha imatges a eliminar.");
            }

            try
            {
                var resposta = await _salaOperacioCua.ExecutarEnCuaAsync(
                    dto.SalaPk,
                    async () =>
                    {
                        var eliminat = await _serveiSala.EliminarImatgesSala(
                            dto.SalaPk,
                            dto.UsuariPK,
                            dto.ImatgePks
                        );

                        if (eliminat)
                        {
                            await _salaHub.Clients
                                .Group($"sala-{dto.SalaPk}")
                                .SendAsync("ImatgesSalaActualitzades", new
                                {
                                    salaPk = dto.SalaPk,
                                    usuariPk = dto.UsuariPK,
                                    operacio = "eliminacio",
                                    dataUtc = DateTime.UtcNow
                                }, cancellationToken);
                        }

                        return new RespostaBaseDto
                        {
                            Correcte = eliminat,
                            Missatge = eliminat
                                ? "Imatges eliminades correctament."
                                : "No s'han pogut eliminar les imatges."
                        };
                    },
                    async () =>
                    {
                        if (!string.IsNullOrWhiteSpace(dto.SignalRConnectionId))
                        {
                            await _salaHub.Clients
                                .Client(dto.SignalRConnectionId)
                                .SendAsync("OperacioSalaEnCua", new
                                {
                                    salaPk = dto.SalaPk,
                                    missatge = "Un altre usuari està pujant o eliminant imatges en aquest moment. Quan acabi, les teves accions es realitzaran."
                                }, cancellationToken);
                        }
                    },
                    cancellationToken
                );

                return Ok(resposta);
            }
            catch (InvalidOperationException ex)
            {
                return Ok(new RespostaBaseDto
                {
                    Correcte = false,
                    Missatge = ex.Message
                });
            }
        }

        /// <summary>
        /// Explicació: prepara i retorna un fitxer ZIP amb totes les imatges d'una sala o només amb una selecció.
        /// Precondicions: el DTO ha d'incloure la sala; si no es descarreguen totes, ha d'incloure imatges seleccionades.
        /// Postcondicions: retorna un ZIP descarregable o una resposta d'error si no hi ha imatges disponibles.
        /// </summary>
        [HttpPost("descarregarImatgesSala")]
        public async Task<IActionResult> DescarregarImatgesSala([FromBody] DescarregarImatgesSalaRequestDto dto, CancellationToken cancellationToken)
        {
            if (string.IsNullOrWhiteSpace(dto.SalaPk))
            {
                return BadRequest("La sala és obligatòria.");
            }

            if (!dto.Totes && (dto.ImatgePks == null || dto.ImatgePks.Length == 0))
            {
                return BadRequest("No hi ha imatges a descarregar.");
            }

            var imatges = await _serveiSala.ObtenirImatgesDescarregarSala(
                dto.SalaPk,
                dto.ImatgePks,
                dto.Totes
            );

            if (imatges.Length == 0)
            {
                return NotFound("No s'han trobat imatges per descarregar.");
            }

            var zipPath = await _serveiSala.CrearZipTemporalImatgesSalaAsync(
                imatges,
                cancellationToken
            );

            var nomFitxer = dto.Totes
                ? $"sala-{dto.SalaPk}-imatges.zip"
                : $"sala-{dto.SalaPk}-imatges-seleccionades.zip";

            var fileStream = new FileStream(
                zipPath,
                FileMode.Open,
                FileAccess.Read,
                FileShare.Read,
                bufferSize: 1024 * 128,
                options: FileOptions.Asynchronous | FileOptions.SequentialScan | FileOptions.DeleteOnClose
            );

            return File(
                fileStream,
                "application/zip",
                nomFitxer
            );
        }

        /// <summary>
        /// Explicació: crea un link compartit per donar accés a una sala amb permisos concrets.
        /// Precondicions: el DTO ha d'incloure sala, usuari creador, nom del link i permisos; l'usuari ha de poder gestionar la sala.
        /// Postcondicions: retorna el link compartit creat o una resposta amb missatge d'error si no es pot crear.
        /// </summary>
        [HttpPost("crearLinkCompartit")]
        public async Task<IActionResult> CrearLinkCompartit([FromBody] CrearLinkCompartitSalaRequestDto dto)
        {
            if (string.IsNullOrWhiteSpace(dto.SalaPK))
            {
                return BadRequest("La sala és obligatòria.");
            }

            if (string.IsNullOrWhiteSpace(dto.UsuariCreadorPK))
            {
                return BadRequest("L'usuari creador és obligatori.");
            }

            if (string.IsNullOrWhiteSpace(dto.Nom))
            {
                return BadRequest("El nom del link és obligatori.");
            }

            try
            {
                var link = await _serveiSala.CrearLinkCompartitSalaAsync(
                    salaPK: dto.SalaPK,
                    usuariCreadorPK: dto.UsuariCreadorPK,
                    nom: dto.Nom,
                    rol: dto.Rol,
                    potVeure: dto.PotVeure,
                    potPujar: dto.PotPujar,
                    potDescarregar: dto.PotDescarregar,
                    potEliminarPropies: dto.PotEliminarPropies,
                    potEliminarQualsevol: dto.PotEliminarQualsevol,
                    potGestionarSala: dto.PotGestionarSala,
                    dataExpiracio: dto.DataExpiracio,
                    limitUsos: dto.LimitUsos
                );

                return Ok(new CrearLinkCompartitSalaResponseDto
                {
                    Correcte = true,
                    Link = MapSalaLinkCompartitDto(link)
                });
            }
            catch (UnauthorizedAccessException ex)
            {
                return Ok(new CrearLinkCompartitSalaResponseDto
                {
                    Correcte = false,
                    Missatge = ex.Message
                });
            }
            catch (InvalidOperationException ex)
            {
                return Ok(new CrearLinkCompartitSalaResponseDto
                {
                    Correcte = false,
                    Missatge = ex.Message
                });
            }
        }

        /// <summary>
        /// Explicació: obté els links compartits existents d'una sala.
        /// Precondicions: el DTO ha d'incloure sala i usuari; l'usuari ha de tenir permisos per gestionar o consultar els links.
        /// Postcondicions: retorna la llista de links compartits o una resposta buida amb missatge si no hi ha autorització.
        /// </summary>
        [HttpPost("obtenirLinksCompartits")]
        public async Task<IActionResult> ObtenirLinksCompartits([FromBody] ObtenirLinksCompartitsSalaRequestDto dto)
        {
            if (string.IsNullOrWhiteSpace(dto.SalaPK))
            {
                return BadRequest("La sala és obligatòria.");
            }

            if (string.IsNullOrWhiteSpace(dto.UsuariPK))
            {
                return BadRequest("L'usuari és obligatori.");
            }

            try
            {
                var links = await _serveiSala.ObtenirLinksCompartitsSalaAsync(
                    dto.SalaPK,
                    dto.UsuariPK
                );

                return Ok(new ObtenirLinksCompartitsSalaResponseDto
                {
                    Correcte = true,
                    Links = links.Select(MapSalaLinkCompartitDto).ToArray()
                });
            }
            catch (UnauthorizedAccessException ex)
            {
                return Ok(new ObtenirLinksCompartitsSalaResponseDto
                {
                    Correcte = false,
                    Missatge = ex.Message,
                    Links = Array.Empty<SalaLinkCompartitDto>()
                });
            }
            catch (InvalidOperationException ex)
            {
                return Ok(new ObtenirLinksCompartitsSalaResponseDto
                {
                    Correcte = false,
                    Missatge = ex.Message,
                    Links = Array.Empty<SalaLinkCompartitDto>()
                });
            }
        }

        /// <summary>
        /// Explicació: consulta la informació pública i de permisos associada a un token de link compartit.
        /// Precondicions: el DTO ha d'incloure el token; opcionalment pot incloure usuari per saber si ja forma part de la sala.
        /// Postcondicions: retorna la informació del link i l'estat de pertinença de l'usuari quan es pot calcular.
        /// </summary>
        [HttpPost("obtenirInfoLinkCompartit")]
        public async Task<IActionResult> ObtenirInfoLinkCompartit([FromBody] ObtenirInfoLinkCompartitSalaRequestDto dto)
        {
            if (string.IsNullOrWhiteSpace(dto.Token))
            {
                return BadRequest("El token és obligatori.");
            }

            try
            {
                var link = await _serveiSala.ObtenirInfoLinkCompartitAsync(dto.Token);

                var jaFormaPart = false;

                if (!string.IsNullOrWhiteSpace(dto.UsuariPK))
                {
                    jaFormaPart = await _serveiSala.UsuariFormaPartSalaAsync(
                        link.SalaPK,
                        dto.UsuariPK
                    );
                }

                return Ok(new ObtenirInfoLinkCompartitSalaResponseDto
                {
                    Correcte = true,
                    Info = new InfoLinkCompartitSalaDto
                    {
                        SalaPK = link.SalaPK,
                        NomSala = link.Sala.Nom,
                        NomLink = link.Nom,
                        Rol = link.Rol,
                        PotVeure = link.PotVeure,
                        PotPujar = link.PotPujar,
                        PotDescarregar = link.PotDescarregar,
                        PotEliminarPropies = link.PotEliminarPropies,
                        PotEliminarQualsevol = link.PotEliminarQualsevol,
                        PotGestionarSala = link.PotGestionarSala,
                        JaFormaPart = jaFormaPart
                    }
                });
            }
            catch (InvalidOperationException ex)
            {
                return Ok(new ObtenirInfoLinkCompartitSalaResponseDto
                {
                    Correcte = false,
                    Missatge = ex.Message
                });
            }
        }
        /// <summary>
        /// Explicació: accepta un link compartit i afegeix o actualitza l'usuari dins de la sala.
        /// Precondicions: el DTO ha d'incloure token i identificador d'usuari; el link ha de ser vàlid i actiu.
        /// Postcondicions: retorna la sala associada al link si l'acceptació és correcta.
        /// </summary>
        [HttpPost("acceptarLinkCompartit")]
        public async Task<IActionResult> AcceptarLinkCompartit(
            [FromBody] AcceptarLinkCompartitSalaRequestDto dto)
        {
            if (string.IsNullOrWhiteSpace(dto.Token))
            {
                return BadRequest("El token és obligatori.");
            }

            if (string.IsNullOrWhiteSpace(dto.UsuariPK))
            {
                return BadRequest("L'usuari és obligatori.");
            }

            try
            {
                var salaPK = await _serveiSala.AcceptarLinkCompartitSalaAsync(
                    dto.Token,
                    dto.UsuariPK
                );

                return Ok(new AcceptarLinkCompartitSalaResponseDto
                {
                    Correcte = true,
                    SalaPK = salaPK
                });
            }
            catch (InvalidOperationException ex)
            {
                return Ok(new AcceptarLinkCompartitSalaResponseDto
                {
                    Correcte = false,
                    Missatge = ex.Message
                });
            }
            catch (UnauthorizedAccessException ex)
            {
                return Ok(new AcceptarLinkCompartitSalaResponseDto
                {
                    Correcte = false,
                    Missatge = ex.Message
                });
            }
        }

        /// <summary>
        /// Explicació: desactiva un link compartit existent.
        /// Precondicions: el DTO ha d'incloure el link i l'usuari; l'usuari ha de tenir permisos per gestionar el link.
        /// Postcondicions: el link queda desactivat si l'operació és correcta.
        /// </summary>
        [HttpPost("desactivarLinkCompartit")]
        public async Task<IActionResult> DesactivarLinkCompartit([FromBody] DesactivarLinkCompartitSalaRequestDto dto)
        {
            if (string.IsNullOrWhiteSpace(dto.SalaLinkCompartitPK))
            {
                return BadRequest("El link és obligatori.");
            }

            if (string.IsNullOrWhiteSpace(dto.UsuariPK))
            {
                return BadRequest("L'usuari és obligatori.");
            }

            try
            {
                await _serveiSala.DesactivarLinkCompartitSalaAsync(
                    dto.SalaLinkCompartitPK,
                    dto.UsuariPK
                );

                return Ok(new RespostaBaseDto
                {
                    Correcte = true
                });
            }
            catch (UnauthorizedAccessException ex)
            {
                return Ok(new RespostaBaseDto
                {
                    Correcte = false,
                    Missatge = ex.Message
                });
            }
            catch (InvalidOperationException ex)
            {
                return Ok(new RespostaBaseDto
                {
                    Correcte = false,
                    Missatge = ex.Message
                });
            }
        }

        /// <summary>
        /// Explicació: regenera el token d'un link compartit i reinicia el seu estat d'ús.
        /// Precondicions: el DTO ha d'incloure el link i l'usuari; l'usuari ha de tenir permisos per gestionar el link.
        /// Postcondicions: retorna el link actualitzat amb el token regenerat si l'operació és correcta.
        /// </summary>
        [HttpPost("regenerarLinkCompartit")]
        public async Task<IActionResult> RegenerarLinkCompartit([FromBody] RegenerarLinkCompartitSalaRequestDto dto)
        {
            if (string.IsNullOrWhiteSpace(dto.SalaLinkCompartitPK))
            {
                return BadRequest("El link és obligatori.");
            }

            if (string.IsNullOrWhiteSpace(dto.UsuariPK))
            {
                return BadRequest("L'usuari és obligatori.");
            }

            try
            {
                var link = await _serveiSala.RegenerarTokenLinkCompartitSalaAsync(
                    dto.SalaLinkCompartitPK,
                    dto.UsuariPK
                );

                return Ok(new RegenerarLinkCompartitSalaResponseDto
                {
                    Correcte = true,
                    Link = MapSalaLinkCompartitDto(link)
                });
            }
            catch (UnauthorizedAccessException ex)
            {
                return Ok(new RegenerarLinkCompartitSalaResponseDto
                {
                    Correcte = false,
                    Missatge = ex.Message
                });
            }
            catch (InvalidOperationException ex)
            {
                return Ok(new RegenerarLinkCompartitSalaResponseDto
                {
                    Correcte = false,
                    Missatge = ex.Message
                });
            }
        }
        /// <summary>
        /// Explicació: transforma una entitat de link compartit en el DTO exposat per l'API.
        /// Precondicions: <paramref name="link"/> ha de contenir les dades persistides del link compartit.
        /// Postcondicions: retorna un DTO amb permisos, estat, token i URL de consum des del frontend.
        /// </summary>
        private SalaLinkCompartitDto MapSalaLinkCompartitDto(SalaLinkCompartit link)
        {
            var frontendBaseUrl = "http://localhost:5173";

            return new SalaLinkCompartitDto
            {
                SalaLinkCompartitPK = link.SalaLinkCompartitPK,
                SalaPK = link.SalaPK,
                Nom = link.Nom,
                Token = link.Token,
                Url = $"{frontendBaseUrl}/unir-sala/{link.Token}",
                Rol = link.Rol,
                PotVeure = link.PotVeure,
                PotPujar = link.PotPujar,
                PotDescarregar = link.PotDescarregar,
                PotEliminarPropies = link.PotEliminarPropies,
                PotEliminarQualsevol = link.PotEliminarQualsevol,
                PotGestionarSala = link.PotGestionarSala,
                Actiu = link.Actiu,
                DataCreacio = link.DataCreacio,
                DataExpiracio = link.DataExpiracio,
                LimitUsos = link.LimitUsos,
                UsosActuals = link.UsosActuals
            };
        }
    }
}

