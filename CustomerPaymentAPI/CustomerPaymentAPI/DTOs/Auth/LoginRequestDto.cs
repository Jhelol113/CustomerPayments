using System.ComponentModel.DataAnnotations;

namespace CustomerPaymentAPI.DTOs.Auth
{
    // TUTOR IA: Este DTO NUNCA se persiste en la base de datos. 
    // Solo se utiliza transitoriamente durante el flujo de autenticación para capturar las credenciales del usuario.
    public class LoginRequestDto
    {
        [Required(ErrorMessage = "El usuario es obligatorio")]
        public string Username { get; set; }

        [Required(ErrorMessage = "La contraseña es obligatoria")]
        public string Password { get; set; }
    }
}
