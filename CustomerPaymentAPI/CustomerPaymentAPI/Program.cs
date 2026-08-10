using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;
using System.Text;
using CustomerPaymentAPI.Data;
using CustomerPaymentAPI.Repositories.Interfaces;
using CustomerPaymentAPI.Repositories.Implementations;
using CustomerPaymentAPI.Services.Interfaces;
using CustomerPaymentAPI.Services.Implementations;
using CustomerPaymentAPI.Security;
using FluentValidation;
using FluentValidation.AspNetCore;

// =====================================================================
// PROGRAM.CS — PUNTO DE ENTRADA DE LA APLICACIÓN
// =====================================================================
// Este archivo configura TODA la aplicación. Aquí se registran:
// 1. Servicios (Inyección de Dependencias)
// 2. Autenticación JWT
// 3. CORS (Cross-Origin Resource Sharing)
// 4. Swagger (documentación de la API)
// 5. Pipeline de middlewares (orden de procesamiento de peticiones)
//
// El orden de los middlewares es CRÍTICO:
// UseCors → UseAuthentication → UseAuthorization → MapControllers
// Si se altera este orden, la autenticación puede fallar silenciosamente.
// =====================================================================

var builder = WebApplication.CreateBuilder(args);

// =====================================================================
// 1. REGISTRO DEL DBCONTEXT (MySQL con Pomelo)
// =====================================================================
// Registramos el AppDbContext para que EF Core pueda
// gestionar la conexión a MySQL. ServerVersion.AutoDetect detecta
// automáticamente la versión de MySQL instalada en el servidor.
// =====================================================================
var connectionString = builder.Configuration.GetConnectionString("DefaultConnection");
builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseMySql(connectionString, ServerVersion.AutoDetect(connectionString)));

// =====================================================================
// 2. INYECCIÓN DE DEPENDENCIAS — REPOSITORIOS
// =====================================================================
// AddScoped crea UNA instancia por cada petición HTTP.
// Esto es ideal para repositorios porque cada petición debe tener
// su propia conexión a la base de datos.
//
// Registramos la INTERFAZ como tipo de servicio y la IMPLEMENTACIÓN
// como tipo de implementación. Cuando un constructor pide ICustomerRepository,
// .NET automáticamente crea un CustomerRepository y lo inyecta.
//
// Alternativas de ciclo de vida:
// - AddSingleton: Una sola instancia para toda la aplicación (no apto para DbContext)
// - AddTransient: Nueva instancia cada vez que se solicita (innecesariamente costoso)
// - AddScoped: Una instancia por petición HTTP (equilibrio perfecto)
// =====================================================================
builder.Services.AddScoped<ICustomerRepository, CustomerRepository>();
builder.Services.AddScoped<IPaymentRepository, PaymentRepository>();
builder.Services.AddScoped<IUserRepository, UserRepository>();

// =====================================================================
// 3. INYECCIÓN DE DEPENDENCIAS — SERVICIOS
// =====================================================================
// Los servicios también son Scoped porque dependen de
// los repositorios (que son Scoped). Un servicio Singleton NO puede
// depender de un servicio Scoped (error en tiempo de ejecución).
// =====================================================================
builder.Services.AddScoped<ICustomerService, CustomerService>();
builder.Services.AddScoped<IPaymentService, PaymentService>();
builder.Services.AddScoped<IAuthService, AuthService>();

// =====================================================================
// 4. INYECCIÓN DE DEPENDENCIAS — SEGURIDAD
// =====================================================================
// No tiene estado mutable, así que una sola
// instancia compartida entre todas las peticiones es eficiente y seguro.
// =====================================================================
builder.Services.AddSingleton<JwtHelper>();

// =====================================================================
// 5. CONFIGURACIÓN DE AUTENTICACIÓN JWT
// =====================================================================
// Aquí le decimos a .NET CÓMO validar los tokens JWT que
// llegan en el header "Authorization: Bearer <token>".
//
// TokenValidationParameters define las reglas de validación:
// - ValidateIssuer: Verifica que el token fue emitido por NUESTRA API
// - ValidateAudience: Verifica que el token es para NUESTRA aplicación
// - ValidateLifetime: Verifica que el token no ha expirado
// - ValidateIssuerSigningKey: Verifica la firma digital del token
// - ClockSkew = TimeSpan.Zero: Sin margen de tolerancia para expiración
//   (por defecto son 5 minutos, lo reducimos a 0 para mayor seguridad)
//
// Si CUALQUIERA de estas validaciones falla, .NET automáticamente
// devuelve HTTP 401 (Unauthorized) sin que nuestro código lo maneje.
// =====================================================================
var jwtSettings = builder.Configuration.GetSection("JwtSettings");
var secretKey = jwtSettings["SecretKey"]!;

builder.Services.AddAuthentication(options =>
{
    options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
    options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
})
.AddJwtBearer(options =>
{
    options.TokenValidationParameters = new TokenValidationParameters
    {
        ValidateIssuer = true,
        ValidateAudience = true,
        ValidateLifetime = true,
        ValidateIssuerSigningKey = true,
        ValidIssuer = jwtSettings["Issuer"],
        ValidAudience = jwtSettings["Audience"],
        IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(secretKey)),
        ClockSkew = TimeSpan.Zero
    };
});

// =====================================================================
// 6. CONFIGURACIÓN DE CORS
// =====================================================================
// CORS (Cross-Origin Resource Sharing) es un mecanismo de
// seguridad de los navegadores que BLOQUEA peticiones HTTP entre
// diferentes orígenes (dominios/puertos).
//
// Sin esta configuración, el frontend en http://localhost:5173 (Vite)
// NO podría hacer peticiones a la API en http://localhost:5041 porque
// son orígenes diferentes (distinto puerto = distinto origen).
//
// AllowAnyHeader: Permite cualquier header (incluyendo "Authorization")
// AllowAnyMethod: Permite GET, POST, PUT, DELETE, etc.
// WithOrigins: SOLO estos orígenes pueden hacer peticiones (whitelist)
//
// SEGURIDAD: En producción, NUNCA uses AllowAnyOrigin().
// Siempre especifica los dominios exactos permitidos.
// =====================================================================
builder.Services.AddCors(options =>
{
    options.AddPolicy("PermitirFrontend", policy =>
    {
        policy.WithOrigins(
                "http://localhost:5173",  // Vite (React dev server)
                "http://localhost:3000"   // Create React App (alternativa)
            )
            .AllowAnyHeader()
            .AllowAnyMethod();
    });
});

// =====================================================================
// 7. CONTROLLERS Y SWAGGER
// =====================================================================
// Swagger genera documentación interactiva de la API.
// La configuración de seguridad permite probar endpoints protegidos
// directamente desde Swagger UI ingresando el token JWT.
//
// Para probar en Swagger:
// 1. Ejecutar POST /api/auth/login con credenciales
// 2. Copiar el token de la respuesta
// 3. Click en "Authorize" (botón con candado)
// 4. Pegar: Bearer <tu_token>
// 5. Ahora puedes probar endpoints protegidos
// =====================================================================
builder.Services.AddControllers();
builder.Services.AddFluentValidationAutoValidation();
builder.Services.AddValidatorsFromAssemblyContaining<Program>();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(options =>
{
    options.SwaggerDoc("v1", new OpenApiInfo
    {
        Title = "Customer Payment API",
        Version = "v1",
        Description = "API CRUD para gestión de Clientes y Pagos con autenticación JWT"
    });

    // Configuración de seguridad en Swagger para JWT.
    // Esto agrega el botón "Authorize" en la interfaz de Swagger UI
    // donde puedes ingresar tu token para probar endpoints protegidos.
    options.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
    {
        Name = "Authorization",
        Type = SecuritySchemeType.Http,
        Scheme = "Bearer",
        BearerFormat = "JWT",
        In = ParameterLocation.Header,
        Description = "Ingresa tu token JWT. Ejemplo: eyJhbGciOiJIUzI1NiIs..."
    });

    options.AddSecurityRequirement(new OpenApiSecurityRequirement
    {
        {
            new OpenApiSecurityScheme
            {
                Reference = new OpenApiReference
                {
                    Type = ReferenceType.SecurityScheme,
                    Id = "Bearer"
                }
            },
            Array.Empty<string>()
        }
    });
});

var app = builder.Build();

// =====================================================================
// 8. SEED — CREAR USUARIO ADMINISTRADOR INICIAL
// =====================================================================
// Este bloque se ejecuta UNA VEZ al iniciar la aplicación.
// Crea el usuario "admin" con contraseña "Admin123!" y rol "Admin".
//
// ¿Por qué usamos CreateScope?
// Porque los servicios Scoped (como IAuthService) no están disponibles
// directamente en el scope raíz de la aplicación. CreateScope simula
// una petición HTTP para poder resolver estos servicios.
//
// Si el admin ya existe, RegisterAsync retorna false silenciosamente.
// No se crea un duplicado gracias a la validación en AuthService.
// =====================================================================
using (var scope = app.Services.CreateScope())
{
    var authService = scope.ServiceProvider.GetRequiredService<IAuthService>();
    await authService.RegisterAsync("admin", "Admin123!", "Admin");
}

// =====================================================================
// 9. PIPELINE DE MIDDLEWARES
// =====================================================================
// El orden de los middlewares es CRUCIAL.
// Cada petición HTTP pasa por estos middlewares en ESTE ORDEN:
//
// Petición entrante:
//   → UseCors (¿el origen está permitido?)
//   → UseAuthentication (¿tiene un token JWT válido?)
//   → UseAuthorization (¿el usuario tiene permiso para este endpoint?)
//   → MapControllers (ejecuta el Controller correspondiente)
//
// Si inviertes UseAuthentication y UseAuthorization, los atributos
// [Authorize] no funcionarán correctamente.
// =====================================================================
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseMiddleware<SecurityHeadersMiddleware>();
app.UseCors("PermitirFrontend");
app.UseAuthentication();
app.UseAuthorization();
app.MapControllers();

app.Run();
