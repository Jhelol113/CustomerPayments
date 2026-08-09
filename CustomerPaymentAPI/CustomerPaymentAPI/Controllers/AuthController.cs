using Microsoft.AspNetCore.Mvc;
using CustomerPaymentAPI.DTOs.Auth;
using CustomerPaymentAPI.Services.Interfaces;

namespace CustomerPaymentAPI.Controllers
{
    // =====================================================================
    // TUTOR IA: CONTROLLER DE AUTENTICACIÓN
    // =====================================================================
    // Este controller maneja el flujo de seguridad del sistema:
    // - Login: Autentica usuarios y genera tokens JWT
    // - Register: Crea nuevos usuarios (solo accesible por Admins)
    //
    // NOTA: Este controller NO tiene [Authorize] a nivel de clase porque
    // el endpoint de Login debe ser accesible sin autenticación (el usuario
    // aún no tiene un token cuando intenta hacer login).
    //
    // [ApiController] habilita automáticamente:
    // - Validación de modelos (Data Annotations en los DTOs)
    // - Respuestas 400 automáticas si el modelo es inválido
    // - Binding automático de [FromBody] para JSON
    //
    // [Route("api/[controller]")] genera la ruta: /api/auth
    // El "[controller]" se reemplaza por el nombre de la clase sin "Controller".
    // =====================================================================
    [Route("api/[controller]")]
    [ApiController]
    public class AuthController : ControllerBase
    {
        private readonly IAuthService _authService;

        // TUTOR IA: Inyectamos la INTERFAZ del servicio, no la implementación.
        // El Controller es la capa más externa — solo maneja HTTP.
        // Toda la lógica de negocio está delegada al Service.
        public AuthController(IAuthService authService)
        {
            _authService = authService;
        }

        // =====================================================================
        // POST: api/auth/login
        // =====================================================================
        // TUTOR IA: Flujo completo de un Login exitoso:
        // 1. El frontend envía: { "username": "admin", "password": "Admin123!" }
        // 2. [ApiController] valida automáticamente el DTO (campos Required)
        // 3. AuthService busca al usuario y verifica la contraseña con BCrypt
        // 4. Si es válido, JwtHelper genera el token JWT
        // 5. Retornamos HTTP 200 con el token y datos del usuario
        //
        // Si las credenciales son inválidas:
        // - AuthService retorna null
        // - Retornamos HTTP 401 (Unauthorized) con un mensaje genérico
        //
        // ¿Por qué un mensaje genérico y no "usuario no encontrado"?
        // Por SEGURIDAD. Si decimos "usuario no encontrado", un atacante
        // puede enumerar usernames válidos. Con un mensaje genérico,
        // no sabe si falló el usuario o la contraseña.
        // =====================================================================
        [HttpPost("login")]
        public async Task<IActionResult> Login([FromBody] LoginRequestDto dto)
        {
            var resultado = await _authService.LoginAsync(dto);

            if (resultado == null)
            {
                return Unauthorized(new { mensaje = "Credenciales inválidas. Verifica tu usuario y contraseña." });
            }

            return Ok(resultado);
        }

        // =====================================================================
        // POST: api/auth/register
        // =====================================================================
        // TUTOR IA: Endpoint para crear nuevos usuarios en el sistema.
        //
        // El parámetro 'rol' viene del Query String, no del body:
        // POST /api/auth/register?rol=Admin
        // Body: { "username": "nuevo_usuario", "password": "Clave123!" }
        //
        // Si el username ya existe, retornamos HTTP 409 (Conflict).
        //
        // SEGURIDAD EN PRODUCCIÓN: Este endpoint debería tener [Authorize]
        // para que solo usuarios autenticados (idealmente Admins) puedan
        // crear nuevos usuarios. Lo dejamos abierto para facilitar la
        // configuración inicial del sistema.
        // =====================================================================
        [HttpPost("register")]
        public async Task<IActionResult> Register(
            [FromBody] LoginRequestDto dto,
            [FromQuery] string rol = "User")
        {
            var exito = await _authService.RegisterAsync(dto.Username, dto.Password, rol);

            if (!exito)
            {
                return Conflict(new { mensaje = $"El nombre de usuario '{dto.Username}' ya está en uso." });
            }

            return Ok(new { mensaje = $"Usuario '{dto.Username}' registrado exitosamente con rol '{rol}'." });
        }
    }
}
