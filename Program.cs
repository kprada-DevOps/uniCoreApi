using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Mvc;
using Microsoft.IdentityModel.Tokens;
using UniCore.Api.Configuration;
using UniCore.Api.Database;
using UniCore.Api.Extensions;
using UniCore.Api.Helpers;
using UniCore.Api.Middleware;
using UniCore.Api.Modules.Auth.Managers;
using UniCore.Api.Modules.Personas.Managers;
using UniCore.Api.Modules.Seguridad.Managers;
using UniCore.Api.Modules.Seguridad.Authorization;
using UniCore.Api.Modules.EstructuraAcademica.Managers;
using UniCore.Api.Modules.PlanEstudios.Managers;

var builder = WebApplication.CreateBuilder(args);

// ---------- Configuración ----------
builder.Services.Configure<JwtOptions>(builder.Configuration.GetSection(JwtOptions.SectionName));
builder.Services.Configure<CorsOptions>(builder.Configuration.GetSection(CorsOptions.SectionName));

// ---------- Base de datos ----------
builder.Services.AddDatabaseInfrastructure(builder.Configuration);

// ---------- Emisión de tokens ----------
builder.Services.AddSingleton<JwtHelper>();

// ---------- Controladores y JSON ----------
builder.Services
    .AddControllers()
    .AddJsonOptions(options =>
    {
        options.JsonSerializerOptions.PropertyNamingPolicy = JsonNamingPolicy.CamelCase;
        options.JsonSerializerOptions.DictionaryKeyPolicy = JsonNamingPolicy.CamelCase;
        options.JsonSerializerOptions.Converters.Add(new JsonStringEnumConverter());
        options.JsonSerializerOptions.DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull;
    })
    .ConfigureApiBehaviorOptions(options =>
    {
        // El filtro automatico de [ApiController] devuelve ProblemDetails antes de que
        // actue el filtro global, asi que se desactiva para que controlemos la respuesta.
        options.SuppressModelStateInvalidFilter = true;
    })
    .AddMvcOptions(options =>
    {
        // Uniformiza los errores de validacion con el formato de Respuesta.
        options.Filters.Add<ValidationProblemDetailsFilter>();
    });

// ---------- Autenticación ----------
var jwtSection = builder.Configuration.GetSection(JwtOptions.SectionName);
var jwtOptions = jwtSection.Get<JwtOptions>() ?? new JwtOptions();

builder.Services
    .AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        options.RequireHttpsMetadata = !builder.Environment.IsDevelopment();
        options.SaveToken = true;
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidateAudience = true,
            ValidateLifetime = true,
            ValidateIssuerSigningKey = true,
            ClockSkew = TimeSpan.Zero,
            ValidIssuer = jwtOptions.Issuer,
            ValidAudience = string.IsNullOrWhiteSpace(jwtOptions.Audience) ? jwtOptions.Issuer : jwtOptions.Audience,
            IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtOptions.TokenSecret)),
        };
    });

builder.Services.AddHttpContextAccessor();
builder.Services.AddSingleton<IAuthorizationPolicyProvider, PermisoPolicyProvider>();
builder.Services.AddScoped<IAuthorizationHandler, PermisoAuthorizationHandler>();
builder.Services.AddAuthorization();

// ---------- CORS ----------
var corsOptions = builder.Configuration.GetSection(CorsOptions.SectionName).Get<CorsOptions>()
    ?? new CorsOptions();

const string CorsPolicyName = "CorsPolicy";

builder.Services.AddCors(options => options.AddPolicy(CorsPolicyName, policy =>
{
    if (corsOptions.AllowedOrigins.Length == 0)
    {
        // Sin orígenes configurados no se permite CORS en absoluto.
        policy.WithOrigins(Array.Empty<string>());
    }
    else
    {
        policy.WithOrigins(corsOptions.AllowedOrigins)
            .AllowAnyMethod()
            .AllowAnyHeader()
            .AllowCredentials();
    }
}));

// ---------- Swagger ----------
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(options =>
{
    options.SwaggerDoc("v1", new Microsoft.OpenApi.Models.OpenApiInfo
    {
        Title = "UniCore.Api",
        Version = "v1",
        Description = "API base de la plataforma universitaria UniCore.",
    });

    options.AddSecurityDefinition("Bearer", new Microsoft.OpenApi.Models.OpenApiSecurityScheme
    {
        Name = "Authorization",
        Type = Microsoft.OpenApi.Models.SecuritySchemeType.Http,
        Scheme = "bearer",
        BearerFormat = "JWT",
        In = Microsoft.OpenApi.Models.ParameterLocation.Header,
        Description = "Introduzca el token JWT con el formato: Bearer {token}",
    });

    options.AddSecurityRequirement(new Microsoft.OpenApi.Models.OpenApiSecurityRequirement
    {
        {
            new Microsoft.OpenApi.Models.OpenApiSecurityScheme
            {
                Reference = new Microsoft.OpenApi.Models.OpenApiReference
                {
                    Type = Microsoft.OpenApi.Models.ReferenceType.SecurityScheme,
                    Id = "Bearer",
                },
            },
            Array.Empty<string>()
        },
    });

    var xmlFile = Path.Combine(AppContext.BaseDirectory,
        $"{System.Reflection.Assembly.GetExecutingAssembly().GetName().Name}.xml");
    if (File.Exists(xmlFile))
        options.IncludeXmlComments(xmlFile);
});

// ---------- Modulos ----------
// Cada modulo vive en Modules/<Modulo>/ con Controllers, Dto, Request, Response y Managers.
// Sus managers se registran aqui, por ambito de peticion.
builder.Services.AddScoped<AuthManager>();
builder.Services.AddScoped<PersonaManager>();
builder.Services.AddScoped<EstudianteManager>();
builder.Services.AddScoped<DocenteManager>();
builder.Services.AddScoped<TipoDocumentoManager>();
builder.Services.AddScoped<UsuarioSeguridadManager>();
builder.Services.AddScoped<RolesManager>();
builder.Services.AddScoped<PermisosManager>();
builder.Services.AddScoped<ModulosManager>();
builder.Services.AddScoped<MenuManager>();
builder.Services.AddScoped<AuditoriaManager>();
builder.Services.AddScoped<SedesManager>();
builder.Services.AddScoped<FacultadesManager>();
builder.Services.AddScoped<DepartamentosManager>();
builder.Services.AddScoped<ProgramasManager>();
builder.Services.AddScoped<AulasManager>();
builder.Services.AddScoped<ProgramaNivelManager>();
builder.Services.AddScoped<ProgramaModalidadManager>();
builder.Services.AddScoped<TipoPeriodoManager>();
builder.Services.AddScoped<PlanEstudiosCatalogosManager>();
builder.Services.AddScoped<CatalogosRelacionadosManager>();
builder.Services.AddScoped<AsignaturasManager>();
builder.Services.AddScoped<PlanesEstudioManager>();
builder.Services.AddScoped<ComponentesManager>();
builder.Services.AddScoped<RequisitosCocurricularesManager>();
builder.Services.AddScoped<CumplimientoEstudianteManager>();

var app = builder.Build();

// ---------- Pipeline ----------

// Manejo global de excepciones: lo primero del pipeline, para capturar cualquier
// fallo de lo que se registre despues.
app.UseMiddleware<GlobalExceptionMiddleware>();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI(options => options.SwaggerEndpoint("/swagger/v1/swagger.json", "UniCore.Api v1"));
}

// En desarrollo la API se sirve por HTTP para no depender del certificado local,
// que no siempre es de confianza y el navegador lo reporta como error de red.
// En produccion todo el trafico se redirige a HTTPS.
if (!app.Environment.IsDevelopment())
{
    app.UseHttpsRedirection();
}

// UseRouting debe preceder a UseCors: el middleware de CORS necesita la metadata
// del endpoint para resolver la politica. Colocarlo antes impide que el preflight
// OPTIONS reciba las cabeceras Access-Control-Allow-* y el navegador bloquea.
app.UseRouting();

app.UseCors(CorsPolicyName);

// Resuelve la conexión (tenant) a partir de la ruta.
app.UseMiddleware<CadenaConnectMiddleware>();

app.UseAuthentication();
app.UseAuthorization();

// Contrasta la conexión de la ruta con la del token.
app.UseMiddleware<PeticionColegioMiddleware>();

app.MapControllers();

// Endpoint de verificacion de salud de la base de datos.
app.MapGet("/health", ([FromServices] PersistenceHealth health) => Results.Ok(new
{
    status = "ok",
    conexiones = DatabaseHealth.GetConfiguredConnections(),
    baseDeDatos = health.GetDatabaseStatus(),
}));

app.Run();

/// <summary>
/// Marcador usado por las pruebas de integracion para localizar el punto de entrada.
/// </summary>
public partial class Program;
