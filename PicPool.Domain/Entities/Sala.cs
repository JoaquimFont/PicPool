using System;
using System.Collections.Generic;
using System.Text;

namespace PicPool.Domain.Entities
{
    public class Sala
    {
        public string SalaPK { get; set; }

        public string Nom { get; set; } = string.Empty;

        public string TokenAcces { get; set; } = string.Empty;

        public string UsuariCreadorPK { get; set; }

        public DateTime DataCreacio { get; set; } = DateTime.UtcNow;

        public DateTime? DataExpiracio { get; set; }

        public bool Activa { get; set; } = true;

        public decimal PesTotal { get; set; } = 0;

        public int TotalImatges { get; set; }

        public DateTime? DataAvisExpiracioEnviat { get; set; }
        public Usuari Usuari { get; set; }

        public ICollection<SalaUsuari> UsuarisSala { get; set; } = new List<SalaUsuari>();

        public ICollection<SalaImatge> ImatgesSala { get; set; } = new List<SalaImatge>();

        public ICollection<SalaLinkCompartit> LinksCompartits { get; set; } = new List<SalaLinkCompartit>();

    }
}
