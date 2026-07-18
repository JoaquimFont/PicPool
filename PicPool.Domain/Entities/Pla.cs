using System;
using System.Collections.Generic;
using System.Text;

namespace PicPool.Domain.Entities
{
    public class Pla
    {
        public string PlaPK { get; set; } 

        public string Nom { get; set; } = string.Empty;

        public long LimitEmmagatzematgeBytes { get; set; }

        public int LimitSales { get; set; }

        public int LimitImatges { get; set; }

        public decimal Preu { get; set; }

        public bool Actiu { get; set; } = true;
    }
}
