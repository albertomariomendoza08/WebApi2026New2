using System.ComponentModel.DataAnnotations;   // atributos de validación como [Required]

namespace WebApi2026New.DTOs
{
    // Datos que llegan cuando alguien se registra
    public class RegistroDTO
    {
        [Required]                                                  // obligatorio
        public string nombre { get; set; } = string.Empty;

        [Required]                                                  // obligatorio
        public string apellido { get; set; } = string.Empty;

        public int edad { get; set; }

        [Required, EmailAddress]                                    // obligatorio y con formato de correo
        public string correo { get; set; } = string.Empty;

        [Required, MinLength(8)]                                    // obligatorio, mínimo 8 caracteres
        public string password { get; set; } = string.Empty;        // llega en texto plano solo en la petición
    }

    // Datos que llegan cuando alguien inicia sesión
    public class LoginDTO
    {
        [Required, EmailAddress]
        public string correo { get; set; } = string.Empty;

        [Required]
        public string password { get; set; } = string.Empty;
    }

    // Lo que devolvemos después de un login correcto
    public class LoginRespuestaDTO
    {
        public string token { get; set; } = string.Empty;           // el JWT firmado
        public DateTime expira { get; set; }                        // cuándo vence
        public int idPersona { get; set; }
        public string nombre { get; set; } = string.Empty;
    }
}