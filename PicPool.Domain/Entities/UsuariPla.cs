using System;
using System.Collections.Generic;
using System.Text;

namespace PicPool.Domain.Entities
{
    public class UsuariPla
    {
        public string UsuariPlaPK { get; set; } 

        public string UsuariPK { get; set; }

        public string PlaPK { get; set; }

        public DateTime DataInici { get; set; } = DateTime.UtcNow;

        public DateTime? DataFi { get; set; }

        public bool Actiu { get; set; } = true;

        public long EspaiConsumitBytes { get; set; }

        public int SalesCreades { get; set; }

        public int ImatgesPujades { get; set; }
    }
}
