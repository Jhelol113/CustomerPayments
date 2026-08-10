using CustomerPaymentAPI.DTOs.Auth;
using CustomerPaymentAPI.Entities;
using CustomerPaymentAPI.Repositories.Interfaces;
using CustomerPaymentAPI.Security;
using CustomerPaymentAPI.Services.Interfaces;

namespace CustomerPaymentAPI.Services.Implementations
{
    // =====================================================================
    //  IMPLEMENTACIÓN DEL SERVICIO DE AUTENTICACIÓN
    // =====================================================================
    // Este servicio es el corazón de la seguridad del sistema.
    // Coordina tres componentes:
    //
    // 1. IUserRepository: Acceso a datos de usuarios (SPs de MySQL)
    // 2. BCrypt: Biblioteca para hashear y verificar contraseñas
    // 3. JwtHelper: Generador de tokens JWT
    //
    //
    // FLUJO DE REGISTRO:
    // 1. Verificar que el username no exista
    // 2. Hashear la contraseña con BCrypt
    // 3. Guardar el usuario con el hash (NUNCA la contraseña en texto plano)
    // =====================================================================
    public class AuthService : IAuthService
    {
        private readonly IUserRepository _userRepository;
        private readonly JwtHelper _jwtHelper;

        // Inyectamos IUserRepository y JwtHelper.
        // Ambos se registran en Program.cs con Inyección de Dependencias.
        public AuthService(IUserRepository userRepository, JwtHelper jwtHelper)
        {
            _userRepository = userRepository;
            _jwtHelper = jwtHelper;
        }

        // =====================================================================
        // MÉTODO: LoginAsync — Autenticar un usuario y generar JWT
        // =====================================================================
        //  Este método implementa el flujo estándar de autenticación:
        //
        // Paso 1: Buscar el usuario por su Username en la BD.
        //   - Si no existe → retornar null (credenciales inválidas).
        //   - Nótese que NO decimos "usuario no encontrado" por seguridad.
        //     Revelar si un username existe ayuda a atacantes a enumerar usuarios.
        //
        // Paso 2: Verificar la contraseña con BCrypt.Verify().
        //   BCrypt.Verify hace lo siguiente internamente:
        //   a) Extrae el salt del hash almacenado
        //   b) Hashea la contraseña ingresada con ese mismo salt
        //   c) Compara los dos hashes
        //   - Si no coinciden → retornar null (credenciales inválidas).
        //
        // Paso 3: Generar el token JWT con los datos del usuario.
        //   El JwtHelper crea un token firmado con los Claims del usuario.
        //
        // Paso 4: Armar y retornar el LoginResponseDto.
        //   El frontend guardará el token y lo usará en peticiones futuras.
        // =====================================================================
        public async Task<LoginResponseDto?> LoginAsync(LoginRequestDto dto)
        {
            // Paso 1: Buscar usuario por username
            var user = await _userRepository.GetByUsernameAsync(dto.Username);
            if (user == null)
            {
                //Retornamos null sin especificar el motivo.
                // El Controller devolverá un genérico "Credenciales inválidas".
                return null;
            }

            // Paso 2: Verificar contraseña con BCrypt
            //BCrypt.Verify compara la contraseña en texto plano
            // contra el hash almacenado. Internamente:
            // - Extrae el salt del hash (los primeros 29 caracteres)
            // - Re-hashea la contraseña con ese salt
            // - Compara byte a byte los resultados
            // Esto es resistente a ataques de timing porque usa comparación
            // de tiempo constante.
            bool passwordValido = BCrypt.Net.BCrypt.Verify(dto.Password, user.PasswordHash);
            if (!passwordValido)
            {
                return null;
            }

            // Paso 3: Generar token JWT
            // JwtHelper retorna una tupla (token, expiración).
            // Usamos desestructuración de tupla para obtener ambos valores.
            var (token, expiracion) = _jwtHelper.GenerateToken(user);

            // Paso 4: Armar respuesta
            return new LoginResponseDto
            {
                Token = token,
                Username = user.Username,
                Rol = user.Rol,
                Expiracion = expiracion
            };
        }

        // =====================================================================
        // MÉTODO: RegisterAsync — Registrar un nuevo usuario
        // =====================================================================
        // El registro sigue estos pasos:
        //
        // 1. Verificar que el username no exista ya.
        //    - Si existe → retornar false (username duplicado).
        //
        // 2. Hashear la contraseña con BCrypt.HashPassword().
        //    BCrypt genera automáticamente:
        //    - Un SALT aleatorio (protege contra rainbow tables)
        //    - Un HASH con work factor 11 (2^11 = 2048 iteraciones)
        //    El resultado es un string como: "$2a$11$K3g4gJ0Z..."
        //    donde '$2a$' es la versión, '11' es el work factor,
        //    y el resto es salt + hash combinados.
        //
        // 3. Crear el usuario con el hash en la BD vía el SP.
        //
        // ¿Por qué el parámetro 'rol' tiene valor por defecto "User"?
        // Porque la mayoría de registros serán usuarios normales.
        // Solo un admin podría crear otro admin pasando rol = "Admin".
        // =====================================================================
        public async Task<bool> RegisterAsync(string username, string password, string rol = "User")
        {
            // Paso 1: Verificar que el username no exista
            var existente = await _userRepository.GetByUsernameAsync(username);
            if (existente != null)
            {
                // Si el usuario ya existe, retornamos false.
                // El Controller devolverá HTTP 409 (Conflict).
                return false;
            }

            // Paso 2: Hashear la contraseña
            // NUNCA almacenes contraseñas en texto plano.
            // BCrypt agrega un salt aleatorio automáticamente, por lo que
            // dos usuarios con la misma contraseña tendrán hashes DIFERENTES.
            var passwordHash = BCrypt.Net.BCrypt.HashPassword(password);

            // Paso 3: Crear el usuario
            var user = new User
            {
                Username = username,
                PasswordHash = passwordHash,
                Rol = rol
            };

            await _userRepository.CreateAsync(user);
            return true;
        }
    }
}
