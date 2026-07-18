using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PicPool.Domain.Entities
{
    public class SalaLinkCompartit
    {
        public string SalaLinkCompartitPK { get; set; }

        public string SalaPK { get; set; }

        public string Token { get; set; } = string.Empty;

        public string Nom { get; set; } = string.Empty;
        // Exemple: "Només lectura", "Convidats", "Poden pujar imatges"

        public string Rol { get; set; } = "Lector";

        public bool PotVeure { get; set; } = true;

        public bool PotPujar { get; set; }

        public bool PotDescarregar { get; set; } = true;

        public bool PotEliminarPropies { get; set; }

        public bool PotEliminarQualsevol { get; set; }

        public bool PotGestionarSala { get; set; }

        public bool Actiu { get; set; } = true;

        public DateTime DataCreacio { get; set; } = DateTime.UtcNow;

        public DateTime? DataExpiracio { get; set; }

        public int? LimitUsos { get; set; }

        public int UsosActuals { get; set; }

        public string UsuariCreadorPK { get; set; }

        public Sala Sala { get; set; }

        public Usuari UsuariCreador { get; set; }
    }
}
