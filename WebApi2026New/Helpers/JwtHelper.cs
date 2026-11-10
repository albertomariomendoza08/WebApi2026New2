using System.Security.Claims;                    // Claim, ClaimsIdentity
using System.Text;                               // Encoding.UTF8
using Microsoft.Extensions.Options;              // IOptions<T>
using Microsoft.IdentityModel.JsonWebTokens;     // JsonWebTokenHandler, JwtRegisteredClaimNames
using Microsoft.IdentityModel.Tokens;            // SymmetricSecurityKey, SigningCredentials
using WebApi2026New.Models;                      // Persona

namespace WebApi2026New.Helpers
{
    // Refleja la sección "Jwt" de appsettings.json (los nombres deben coincidir)
    public class JwtSettings
    {
        public string Key { get; set; } = string.Empty;
        public string Issuer { get; set; } = string.Empty;
        public string Audience { get; set; } = string.Empty;
        public int ExpiresMinutes { get; set; } = 60;
    }

    public class JwtHelper
    {
        private readonly JwtSettings _settings;   // configuración leída

        public JwtHelper(IOptions<JwtSettings> options)   // ASP.NET inyecta la configuración aquí
        {
            _settings = options.Value;                    // .Value = el objeto JwtSettings ya lleno
        }

        // Recibe una persona y devuelve el token y su fecha de vencimiento
        public (string token, DateTime expira) GenerarToken(Persona persona)
        {
            var expira = DateTime.UtcNow.AddMinutes(_settings.ExpiresMinutes);   // ahora + 60 min (en UTC)

            // Claims = datos que viajan dentro del token. Mínimos, y NUNCA la contraseña
            var claims = new List<Claim>
            {
                new(JwtRegisteredClaimNames.Sub, persona.idPersona.ToString()),              // sub = id del usuario
                new(JwtRegisteredClaimNames.Email, persona.correo),                          // correo
                new(JwtRegisteredClaimNames.Name, $"{persona.nombre} {persona.apellido}")    // nombre completo
            };

            // Convierte el texto de la clave en bytes para poder firmar
            var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_settings.Key));

            // Descriptor = la "receta" del token
            var descriptor = new SecurityTokenDescriptor
            {
                Subject = new ClaimsIdentity(claims),        // los claims de arriba
                Expires = expira,                            // vencimiento
                Issuer = _settings.Issuer,                   // emisor
                Audience = _settings.Audience,               // audiencia
                SigningCredentials = new SigningCredentials(key, SecurityAlgorithms.HmacSha256)   // firma con HMAC-SHA256
            };

            var token = new JsonWebTokenHandler().CreateToken(descriptor);   // arma y firma el texto "xxx.yyy.zzz"
            return (token, expira);                                           // devuelve ambos
        }
    }
}