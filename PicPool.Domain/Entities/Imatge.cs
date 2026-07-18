using System;

namespace PicPool.Domain.Entities
{
    public class Imatge
    {
        public string ImatgePK { get; set; }

        public string NomOriginal { get; set; } = string.Empty;

        // Ruta interna dins Cloudflare R2
        public string RutaStorage { get; set; } = string.Empty;

        // URL pública completa
        public string SourceUrl { get; set; } = string.Empty;

        public long MidaBytes { get; set; }

        public string TipusMime { get; set; } = string.Empty;

        public string UsuariPujadorPK { get; set; }

        public Usuari Usuari { get; set; }

        public DateTime DataPujada { get; set; } = DateTime.UtcNow;
    }
}