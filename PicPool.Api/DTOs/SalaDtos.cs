using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using PicPool.Api.Usuari.DTOs;

namespace PicPool.Api.Sala.DTOs
{

    public class ObtenirImatgesSalaRequestDto
    {
        public string SalaPK { get; set; }

        public string UsuariPK { get; set; }

        public int Pagina { get; set; } = 1;

        public int Quantitat { get; set; } = 20;

        public string Ordre { get; set; } = "data";

        public bool Descendent { get; set; } = true;
    }

    public class ObtenirImatgesSalaResponseDto
    {
        public bool Correcte { get; set; }

        public int TotalImatges { get; set; }

        public string Missatge { get; set; } = string.Empty;

        public ImatgeSalaDto[] Imatges { get; set; } = [];

        public string[] ImatgePksExistents { get; set; } = [];

    }

    public class ImatgeSalaDto
    {
        public string ImatgePK { get; set; } = string.Empty;

        public string Nom { get; set; } = string.Empty;

        public string Resolucio { get; set; }
        public string SourceUrl { get; set; } = string.Empty;

        public long MidaBytes { get; set; }

        public string TipusMime { get; set; } = string.Empty;

        public UsuariDto UsuariCreador { get; set; } = new UsuariDto();

        public DateTime DataPujada { get; set; }
    }

    public class PujarImatgeRequestDto
    {
        public IFormFile File { get; set; }
        public decimal Mida { get; set; }
        public decimal Resolucio { get; set; }

        public string? Nom { get; set; } = string.Empty;

        public string? Descripcio { get; set; } = string.Empty;

        public string Propietari { get; set; }
    }

    public class PujarImatgesRequestDto
    {
        public PujarImatgeRequestDto[] Imatges { get; set; }

        public string SalaPk { get; set; }

        public string UserPk { get; set; }

        public string? SignalRConnectionId { get; set; }


    }


    public class PujarImatgesRespostaDto
    {

        public bool Correcte { get; set; }

        public string Missatge { get; set; }

    }

    public class PujarImatgeRespostaDto
    {
        public bool Correcte { get; set; }

        public string Missatge { get; set; } = string.Empty;

        public string? UsuariPK { get; set; }

        public string ImatgePK { get; set; } = string.Empty;

        public string SalaPK { get; set; } = string.Empty;

        public string SourceUrl { get; set; } = string.Empty;
    }

    public class DescarregarImatgesSalaRequestDto
    {
        public string SalaPk { get; set; } = string.Empty;

        public string[] ImatgePks { get; set; } = [];

        public bool Totes { get; set; } = false;
    }

    public class EliminarImatgesSalaRequestDto
    {
        public string SalaPk { get; set; } = string.Empty;

        public string UsuariPK { get; set; } = string.Empty;

        public string[] ImatgePks { get; set; } = [];

        public string? SignalRConnectionId { get; set; }

    }
    public class CrearLinkCompartitSalaRequestDto
    {
        public string SalaPK { get; set; }

        public string UsuariCreadorPK { get; set; }

        public string Nom { get; set; } = string.Empty;

        public string Rol { get; set; } = "Lector";

        public bool PotVeure { get; set; } = true;

        public bool PotPujar { get; set; }

        public bool PotDescarregar { get; set; } = true;

        public bool PotEliminarPropies { get; set; }

        public bool PotEliminarQualsevol { get; set; }

        public bool PotGestionarSala { get; set; }

        public DateTime? DataExpiracio { get; set; }

        public int? LimitUsos { get; set; }
    }
    public class CrearLinkCompartitSalaResponseDto
    {
        public bool Correcte { get; set; }

        public string? Missatge { get; set; }

        public SalaLinkCompartitDto? Link { get; set; }
    }
    public class SalaLinkCompartitDto
    {
        public string SalaLinkCompartitPK { get; set; }

        public string SalaPK { get; set; }

        public string Nom { get; set; }

        public string Token { get; set; }

        public string Url { get; set; }

        public string Rol { get; set; }

        public bool PotVeure { get; set; }

        public bool PotPujar { get; set; }

        public bool PotDescarregar { get; set; }

        public bool PotEliminarPropies { get; set; }

        public bool PotEliminarQualsevol { get; set; }

        public bool PotGestionarSala { get; set; }

        public bool Actiu { get; set; }

        public DateTime DataCreacio { get; set; }

        public DateTime? DataExpiracio { get; set; }

        public int? LimitUsos { get; set; }

        public int UsosActuals { get; set; }
    }

    public class ObtenirLinksCompartitsSalaRequestDto
    {
        public string SalaPK { get; set; }

        public string UsuariPK { get; set; }
    }

    public class ObtenirLinksCompartitsSalaResponseDto
    {
        public bool Correcte { get; set; }

        public string? Missatge { get; set; }

        public SalaLinkCompartitDto[] Links { get; set; } = Array.Empty<SalaLinkCompartitDto>();
    }

    public class ObtenirInfoLinkCompartitSalaRequestDto
    {
        public string Token { get; set; }

        public string? UsuariPK { get; set; }
    }

    public class ObtenirInfoLinkCompartitSalaResponseDto
    {
        public bool Correcte { get; set; }

        public string? Missatge { get; set; }

        public InfoLinkCompartitSalaDto? Info { get; set; }
    }

    public class InfoLinkCompartitSalaDto
    {
        public string SalaPK { get; set; }

        public string NomSala { get; set; }

        public string NomLink { get; set; }

        public string Rol { get; set; }

        public bool PotVeure { get; set; }

        public bool PotPujar { get; set; }

        public bool PotDescarregar { get; set; }

        public bool PotEliminarPropies { get; set; }

        public bool PotEliminarQualsevol { get; set; }

        public bool PotGestionarSala { get; set; }

        public bool JaFormaPart { get; set; }
    }

    public class AcceptarLinkCompartitSalaRequestDto
    {
        public string Token { get; set; }

        public string UsuariPK { get; set; }
    }

    public class AcceptarLinkCompartitSalaResponseDto
    {
        public bool Correcte { get; set; }

        public string? Missatge { get; set; }

        public string? SalaPK { get; set; }
    }

    public class DesactivarLinkCompartitSalaRequestDto
    {
        public string SalaLinkCompartitPK { get; set; }

        public string UsuariPK { get; set; }
    }

    public class RegenerarLinkCompartitSalaRequestDto
    {
        public string SalaLinkCompartitPK { get; set; }

        public string UsuariPK { get; set; }
    }

    public class RegenerarLinkCompartitSalaResponseDto
    {
        public bool Correcte { get; set; }

        public string? Missatge { get; set; }

        public SalaLinkCompartitDto? Link { get; set; }
    }

    public class RespostaBaseDto
    {
        public bool Correcte { get; set; }

        public string? Missatge { get; set; }
    }
}
