using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using WebApi2026New.Data;
using WebApi2026New.DTOs;
using WebApi2026New.Helpers;
using WebApi2026New.Models;

[Route("api/[controller]")]          // ruta base: api/Auth
[ApiController]                      // validación automática y respuestas 400
public class AuthController : ControllerBase
{
    private readonly AppDBContext _context;   // acceso a la base de datos
    private readonly JwtHelper _jwt;          // generador de tokens

    public AuthController(AppDBContext context, JwtHelper jwt)   // ASP.NET inyecta ambos
    {
        _context = context;
        _jwt = jwt;
    }

    // POST: api/Auth/registro (público)
    [HttpPost("registro")]
    public async Task<ActionResult<PersonaReadDTO>> Registro(RegistroDTO dto)
    {
        var correo = dto.correo.Trim().ToLowerInvariant();   // normaliza: sin espacios y en minúscula

        // Si ya hay alguien con ese correo, responde 409 Conflict
        if (await _context.Personas.AnyAsync(p => p.correo == correo))
            return Conflict(new { mensaje = "El correo ya está registrado." });

        var persona = new Persona
        {
            nombre = dto.nombre,
            apellido = dto.apellido,
            edad = dto.edad,
            correo = correo,
            password = BCrypt.Net.BCrypt.HashPassword(dto.password)   // se guarda el HASH, no la contraseña
        };

        _context.Personas.Add(persona);          // marca la persona para insertar
        await _context.SaveChangesAsync();       // ejecuta el INSERT en SQL Server

        return Ok(new PersonaReadDTO             // devuelve los datos SIN password
        {
            idPersona = persona.idPersona,
            nombre = persona.nombre,
            apellido = persona.apellido,
            edad = persona.edad,
            correo = persona.correo
        });
    }

    // POST: api/Auth/login (público)
    [HttpPost("login")]
    public async Task<ActionResult<LoginRespuestaDTO>> Login(LoginDTO dto)
    {
        var correo = dto.correo.Trim().ToLowerInvariant();
        var persona = await _context.Personas.FirstOrDefaultAsync(p => p.correo == correo);   // busca por correo

        // Mensaje genérico: no revela si falló el correo o la contraseña
        if (persona is null || !VerificarPassword(dto.password, persona.password))
            return Unauthorized(new { mensaje = "Credenciales inválidas." });   // 401

        var (token, expira) = _jwt.GenerarToken(persona);   // crea el JWT

        return Ok(new LoginRespuestaDTO
        {
            token = token,
            expira = expira,
            idPersona = persona.idPersona,
            nombre = $"{persona.nombre} {persona.apellido}"
        });
    }

    // GET: api/Auth/yo (requiere token) -> muestra lo que trae el token
    [HttpGet("yo")]
    [Authorize]                       // sin token válido responde 401
    public IActionResult Yo()
    {
        return Ok(new
        {
            idPersona = User.FindFirst("sub")?.Value,     // claim sub
            correo = User.FindFirst("email")?.Value,      // claim email
            nombre = User.Identity?.Name                  // claim name (por NameClaimType)
        });
    }

    // Compara la contraseña escrita con el hash guardado
    private static bool VerificarPassword(string password, string hashGuardado)
    {
        try
        {
            return BCrypt.Net.BCrypt.Verify(password, hashGuardado);   // true si coincide
        }
        catch (BCrypt.Net.SaltParseException)
        {
            // La fila tiene una contraseña antigua en texto plano (no es un hash válido)
            return false;
        }
    }
}