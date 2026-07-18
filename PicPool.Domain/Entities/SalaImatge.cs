using System;
using System.Collections.Generic;
using System.Text;

namespace PicPool.Domain.Entities
{
    public class SalaImatge
    {
        public string ImatgeSalaPK { get; set; }

        public string SalaPK { get; set; }

        public string ImatgePK { get; set; }

        public Sala? Sala { get; set; }

        public Imatge? Imatge { get; set; }
    }
}

