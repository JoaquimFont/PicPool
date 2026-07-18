using System;
using System.Collections.Generic;
using System.Text;

namespace PicPool.Domain.Entities
{
    public class SalaUsuari
    {
        public string SalaUsuariPK { get; set; }

        public string SalaPK { get; set; }

        public string UsuariPK { get; set; }

        public string Rol { get; set; } = "Lector";

        public bool PotVeure { get; set; } = true;

        public bool PotPujar { get; set; }

        public bool PotDescarregar { get; set; } = true;

        public bool PotEliminarPropies { get; set; }

        public bool PotEliminarQualsevol { get; set; }

        public bool PotGestionarSala { get; set; }

        public DateTime DataUnio { get; set; } = DateTime.UtcNow;

        public Sala Sala { get; set; }

        public Usuari Usuari { get; set; }

    }
}
