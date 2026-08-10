namespace CustomerPaymentAPI.DTOs.Auth
{
    //Esta es la estructura estándar de respuesta cuando generamos un JWT (JSON Web Token).
    // El frontend guardará el 'Token' (usualmente en LocalStorage o Cookies) y lo enviará en las siguientes peticiones.
    public class LoginResponseDto
    {
        public string Token { get; set; }
        public string Username { get; set; }
        public string Rol { get; set; }
        public DateTime Expiracion { get; set; }
    }
}
