using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using PicPool.Domain.Entities;

namespace PicPool.Api.Usuari.DTOs
{
   
        public class LoginRequestDto
        {
            public string Username { get; set; }

            public string Password { get; set; }
        }

        public class LoginResponseDto
        {
            public bool Correcte { get; set; }

            public string Missatge { get; set; }

            public string UsuariPK { get; set; }

            public string Nom { get; set; }

            public string Email { get; set; }
        }
    
        public class ObtenirSalesUsuariRequestDto
        {
            public string UsuariPK { get; set; }

            public int Pagina { get; set; } = 1;

            public int Quantitat { get; set; } = 20;

            public string Ordre { get; set; } = "data";

            public bool Descendent { get; set; } = true;
    }

         public class obtenirSalaDto
        {
            public string SalaPK { get; set; }

            public string Nom { get; set; }

            public string TokenAcces { get; set; }

            public usuarioDto UsuariCreador { get; set; }

            public DateTime DataCreacio { get; set; }

            public DateTime? DataExpiracio { get; set; }

            public bool Activa { get; set; } = true;

            public int TotalImatges { get; set; } = 0;

            public decimal PesTotal { get; set; } = 0;
        }

        public class usuarioDto
        {
            public string UsuariPK { get; set; }
            public string Nom { get; set; }

            public string Email { get; set; }
        }

        public class ObtenirSalesUsuariResponseDto
        {
            public bool Correcte { get; set; }

            public string Missatge { get; set; }

            public obtenirSalaDto[] Salas { get; set; }
        }


        public class CrearSalaRequestDto
        {
            public string UsuariPK { get; set; }

            public string NomSala { get; set; }    

        }

        public class CrearSalaResponseDto
        {
            public bool Correcte { get; set; }
            public string SalaPK { get; set; }
            public string NomSala { get; set; }

        }

        public class CrearUsuariDto
        {
            public string Nom { get; set; }

            public string Email { get; set; }

            public string Password { get; set; }
        }

        public class CrearUsuariResponseDto
        {
            public bool Correcte { get; set; }

            public string Missatge { get; set; }

            public string UsuariPK { get; set; }

            public string Nom { get; set; }

            public string Email { get; set; }
        }

        public class UsuariDto
        {
            public string UsuariPK { get; set; }

            public string Nom { get; set; }

            public string Email { get; set; }
        }

    public class EliminarSalesUsuariRequestDto { 
        public string UsuariPK { get; set; }
        
        public string[] SalaPks { get; set; }

    }


}
