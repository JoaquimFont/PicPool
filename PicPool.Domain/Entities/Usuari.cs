using System;
using System.Collections.Generic;
using System.Text;

namespace PicPool.Domain.Entities
{
    public class Usuari
    {
        public string UsuariPK { get; set; }

        public string Nom { get; set; } = string.Empty;

        public string Email { get; set; } = string.Empty;

        public string PasswordHash { get; set; } = string.Empty;

        public DateTime DataAlta { get; set; } = DateTime.UtcNow;

        public bool Actiu { get; set; } = true;

        public ICollection<UsuariPla> UsuariPlans { get; set; } = new List<UsuariPla>();
        public ICollection<Sala> SalesCreades { get; set; } = new List<Sala>();
        public ICollection<SalaUsuari> SalesUsuari { get; set; } = new List<SalaUsuari>();
        public ICollection<Imatge> ImatgesPujades { get; set; } = new List<Imatge>();

    }
}
