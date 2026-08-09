using CustomerPaymentAPI.DTOs.Auth;

namespace CustomerPaymentAPI.Services.Interfaces
{
    // =====================================================================
    // INTERFAZ DEL SERVICIO DE AUTENTICACIÓN
    // =====================================================================
    // Este servicio maneja dos flujos fundamentales de seguridad:
    //
    // 1. LOGIN: Recibe credenciales → Verifica con BCrypt → Genera JWT
    //    - Si las credenciales son válidas, retorna un LoginResponseDto
    //      con el token JWT, username, rol y fecha de expiración.
    //    - Si son inválidas, retorna null (el Controller devuelve 401).
    //
    // 2. REGISTRO: Recibe datos del nuevo usuario → Hashea contraseña → Guarda
    //    - Retorna true si el registro fue exitoso.
    //    - Retorna false si el username ya existe.
    //
    // ¿Por qué el Login retorna un DTO completo y no solo el token?
    // Porque el frontend necesita saber el Username, Rol y Expiración
    // para configurar la interfaz (mostrar nombre, ocultar/mostrar opciones
    // según el rol, y programar el refresh del token antes de que expire).
    // =====================================================================
    public interface IAuthService
    {
        /// <summary>
        /// Autentica un usuario con username y password.
        /// Retorna el token JWT y datos del usuario, o null si las credenciales son inválidas.
        /// </summary>
        Task<LoginResponseDto?> LoginAsync(LoginRequestDto dto);

        /// <summary>
        /// Registra un nuevo usuario en el sistema.
        /// Retorna true si fue exitoso, false si el username ya existe.
        /// </summary>
        Task<bool> RegisterAsync(string username, string password, string rol = "User");
    }
}
