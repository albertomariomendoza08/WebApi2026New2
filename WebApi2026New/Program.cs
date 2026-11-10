using System.Text;
using Microsoft.AspNetCore.Authentication.JwtBearer;   // JwtBearerDefaults
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;                  // TokenValidationParameters
using Microsoft.OpenApi;                               // clases del esquema de seguridad
using Scalar.AspNetCore;
using WebApi2026New.Data;
using WebApi2026New.Helpers;

var builder = WebApplication.CreateBuilder(args);      // crea el constructor de la app

// Base de datos (ya la tenías): usa la cadena DefaultConnection
builder.Services.AddDbContext<AppDBContext>(options =>
{
    options.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection"));
});

// ---------- JWT ----------
// Enlaza la sección "Jwt" con la clase JwtSettings (para inyectarla con IOptions)
builder.Services.Configure<JwtSettings>(builder.Configuration.GetSection("Jwt"));
builder.Services.AddSingleton<JwtHelper>();            // una sola instancia de JwtHelper para toda la app

// Lee la sección Jwt ahora mismo (la necesitamos para configurar la validación)
var jwt = builder.Configuration.GetSection("Jwt").Get<JwtSettings>()
          ?? throw new InvalidOperationException("Falta la sección 'Jwt' en appsettings.json.");

// Si la clave es corta o está vacía, la API NO arranca (así nunca corre insegura)
if (Encoding.UTF8.GetByteCount(jwt.Key) < 32)
    throw new InvalidOperationException("Jwt:Key debe tener al menos 32 caracteres.");

builder.Services
    .AddAuthentication(JwtBearerDefaults.AuthenticationScheme)   // esquema por defecto: Bearer
    .AddJwtBearer(options =>
    {
        options.MapInboundClaims = false;   // conserva los nombres "sub", "email", "name" tal cual
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,          // revisa que el emisor sea el nuestro
            ValidIssuer = jwt.Issuer,
            ValidateAudience = true,        // revisa que el token sea para esta API
            ValidAudience = jwt.Audience,
            ValidateLifetime = true,        // rechaza tokens vencidos
            ClockSkew = TimeSpan.FromMinutes(1),   // tolerancia de 1 min por diferencias de reloj
            ValidateIssuerSigningKey = true,       // verifica la firma
            IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwt.Key)),
            NameClaimType = "name"          // User.Identity.Name tomará el claim "name"
        };
    });

builder.Services.AddAuthorization();        // habilita [Authorize]
// -------------------------

builder.Services.AddControllers();

// OpenAPI: declara el esquema "Bearer" para que Scalar muestre dónde pegar el token
builder.Services.AddOpenApi(options =>
{
    options.AddDocumentTransformer((document, context, cancellationToken) =>
    {
        document.Components ??= new OpenApiComponents();   // crea Components si no existe
        document.Components.SecuritySchemes ??= new Dictionary<string, IOpenApiSecurityScheme>();
        document.Components.SecuritySchemes["Bearer"] = new OpenApiSecurityScheme
        {
            Type = SecuritySchemeType.Http,   // autenticación HTTP
            Scheme = "bearer",                // tipo bearer
            BearerFormat = "JWT",
            Description = "Pega aquí el token que devuelve POST /api/Auth/login"
        };
        document.Security =
        [
            new OpenApiSecurityRequirement
            {
                [new OpenApiSecuritySchemeReference("Bearer", document)] = []   // exige Bearer en la documentación
            }
        ];
        return Task.CompletedTask;
    });
});

var app = builder.Build();                  // construye la app (aquí termina la configuración de servicios)

if (app.Environment.IsDevelopment())        // solo en desarrollo
{
    app.MapOpenApi();                       // publica el documento OpenAPI
    app.MapScalarApiReference(options =>    // publica Scalar (/scalar/v1)
    {
        options.Title = "WebApi2026New";
        options.AddPreferredSecuritySchemes("Bearer");   // Scalar deja Bearer seleccionado
    });
}

app.UseHttpsRedirection();                  // redirige http a https

// El orden importa: primero identifica quién eres, luego decide si puedes pasar
app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();                       // conecta las rutas de los controladores

app.Run();                                  // arranca el servidor