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
        public SalaController(ServeiSala serveiSala, SalaOperacioCua salaOperacioCua, IHubContext<SalaHub> salaHub)
        {
            _serveiSala = serveiSala;
            _salaOperacioCua = salaOperacioCua;
            _salaHub = salaHub;
        }

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

