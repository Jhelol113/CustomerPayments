using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using Microsoft.IdentityModel.Tokens;
using CustomerPaymentAPI.Entities;

namespace CustomerPaymentAPI.Security
{
    // =====================================================================
    // JWT HELPER — GENERADOR DE TOKENS JWT
    // =====================================================================
    // Esta clase se encarga exclusivamente de generar tokens JWT.
    // Se registra como Singleton en Program.cs porque no tiene estado
    // mutable — siempre lee la configuración y genera tokens frescos.
    // =====================================================================
    public class JwtHelper
    {
        private readonly IConfiguration _configuration;

        // Inyectamos IConfiguration para leer las claves JWT
        // desde appsettings.json (sección "JwtSettings").
        public JwtHelper(IConfiguration configuration)
        {
            _configuration = configuration;
        }

        // =====================================================================
        // MÉTODO: GenerateToken — Genera un token JWT para un usuario autenticado
        // =====================================================================
        // Este método se invoca desde AuthService después de verificar
        // las credenciales del usuario con BCrypt.
        //
        // Retorna una TUPLA (string token, DateTime expiration):
        // - token: el string JWT completo para enviar al frontend
        // - expiration: la fecha/hora de expiración para informar al frontend
        //
        // Pasos internos:
        // 1. Leer la configuración JWT (clave secreta, issuer, audience, minutos)
        // 2. Crear la clave de firma (SymmetricSecurityKey) desde la SecretKey
        // 3. Definir los Claims (datos del usuario dentro del token)
        // 4. Construir el JwtSecurityToken con todos los componentes
        // 5. Serializar el token a string con JwtSecurityTokenHandler
        // =====================================================================
        public (string token, DateTime expiracion) GenerateToken(User user)
        {
            // Leemos la sección "JwtSettings" de appsettings.json.
            // El operador '!' (null-forgiving) le dice al compilador que confiamos
            // en que estos valores existen en la configuración.
            var jwtSettings = _configuration.GetSection("JwtSettings");
            var secretKey = jwtSettings["SecretKey"]!;
            var issuer = jwtSettings["Issuer"]!;
            var audience = jwtSettings["Audience"]!;
            var expirationMinutes = int.Parse(jwtSettings["ExpirationMinutes"]!);

            // La clave simétrica se usa para FIRMAR y VERIFICAR el token.
            // Es "simétrica" porque la misma clave se usa para ambas operaciones.
            // IMPORTANTE: La SecretKey debe tener al menos 32 caracteres (256 bits)
            // para cumplir con el requisito de seguridad de HMAC-SHA256.
            var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(secretKey));
            var credentials = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

            // Los Claims son las "declaraciones" sobre el usuario
            // que se incluyen dentro del payload del JWT.
            // - NameIdentifier: Id del usuario (para identificarlo en la BD)
            // - Name: Username (para mostrar en el frontend)
            // - Role: Rol del usuario (para autorización basada en roles)
            //
            // SEGURIDAD: No incluyas datos sensibles en los claims (contraseñas,
            // tarjetas de crédito, etc.) porque el payload del JWT es decodificable
            // por cualquiera ya que está en Base64 solo FIRMADO.
            var claims = new[]
            {
                new Claim(ClaimTypes.NameIdentifier, user.Id.ToString()),
                new Claim(ClaimTypes.Name, user.Username),
                new Claim(ClaimTypes.Role, user.Rol)
            };

            // Calculamos la fecha de expiración usando UTC 
            // para evitar problemas con zonas horarias entre servidor y cliente.
            var expiracion = DateTime.UtcNow.AddMinutes(expirationMinutes);

            // Construimos el token con todos los componentes.
            // - issuer: quién emitió el token (nuestra API)
            // - audience: para quién es el token (nuestro frontend)
            // - claims: datos del usuario
            // - expires: cuándo expira
            // - signingCredentials: cómo se firma
            var token = new JwtSecurityToken(
                issuer: issuer,
                audience: audience,
                claims: claims,
                expires: expiracion,
                signingCredentials: credentials
            );

            // WriteToken serializa el JwtSecurityToken a un string
            // en formato: "eyJhbGci...eyJuYW1l...SflKxwRJ..."
            // Este string es lo que el frontend guarda y envía en cada petición.
            var tokenString = new JwtSecurityTokenHandler().WriteToken(token);

            return (tokenString, expiracion);
        }
    }
}
