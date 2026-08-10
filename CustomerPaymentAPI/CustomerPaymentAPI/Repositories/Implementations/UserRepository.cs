using Microsoft.EntityFrameworkCore;
using MySqlConnector;
using System.Data;
using CustomerPaymentAPI.Data;
using CustomerPaymentAPI.Entities;
using CustomerPaymentAPI.Repositories.Interfaces;

namespace CustomerPaymentAPI.Repositories.Implementations
{
    // =====================================================================
    // IMPLEMENTACIÓN DEL REPOSITORIO DE USER
    // =====================================================================
    // Este repositorio soporta las operaciones necesarias para autenticación:
    // - GetByUsernameAsync: Buscar usuario para login (FromSqlRaw)
    // - CreateAsync: Registrar nuevo usuario (ADO.NET)
    //
    // Sigue el mismo patrón híbrido que CustomerRepository:
    // - Lectura simple → FromSqlRaw (la entidad User mapea directo)
    // - Escritura con retorno escalar → ADO.NET
    //
    // NOTA DE SEGURIDAD: Este repositorio NUNCA debe manejar contraseñas
    // en texto plano. El campo PasswordHash ya debe venir hasheado desde
    // la capa de servicio (AuthService) usando BCrypt.
    // =====================================================================
    public class UserRepository : IUserRepository
    {
        private readonly AppDbContext _context;

        public UserRepository(AppDbContext context)
        {
            _context = context;
        }

        // =====================================================================
        // MÉTODO: GetByUsernameAsync — Buscar usuario por nombre de usuario
        // =====================================================================
        // Este método se invoca durante el proceso de login.
        // El SP sp_User_GetByUsername busca por Username Y verifica que
        // el usuario esté activo (Activo = TRUE).
        //
        // El AuthService comparará la contraseña ingresada por el usuario
        // contra el PasswordHash retornado usando BCrypt.Verify().
        // El repositorio NO hace esa comparación — solo trae los datos.
        // Eso es responsabilidad de la capa de negocio (Service).
        // =====================================================================
        public async Task<User?> GetByUsernameAsync(string username)
        {
            var resultado = await _context.Users
                .FromSqlRaw("CALL sp_User_GetByUsername({0})", username)
                .AsNoTracking()
                .ToListAsync();

            return resultado.FirstOrDefault();
        }

        // =====================================================================
        // MÉTODO: CreateAsync — Registrar un nuevo usuario
        // =====================================================================
        // El SP sp_User_Create inserta el usuario y retorna
        // LAST_INSERT_ID() con el Id generado.
        //
        // IMPORTANTE: user.PasswordHash DEBE contener el hash BCrypt
        // de la contraseña, NO la contraseña en texto plano.
        // La responsabilidad de hashear está en AuthService.
        //
        // Ejemplo del flujo completo:
        // 1. Usuario envía: { "Username": "juan", "Password": "MiClave123" }
        // 2. AuthService hashea: BCrypt.HashPassword("MiClave123") → "$2a$11$..."
        // 3. AuthService crea User con PasswordHash = "$2a$11$..."
        // 4. Este método recibe el User con el hash y lo guarda en BD
        // =====================================================================
        public async Task<int> CreateAsync(User user)
        {
            var connection = _context.Database.GetDbConnection();

            try
            {
                await _context.Database.OpenConnectionAsync();

                using var command = connection.CreateCommand();
                command.CommandText = "sp_User_Create";
                command.CommandType = CommandType.StoredProcedure;

                command.Parameters.Add(new MySqlParameter("@p_Username", user.Username));
                command.Parameters.Add(new MySqlParameter("@p_PasswordHash", user.PasswordHash));
                command.Parameters.Add(new MySqlParameter("@p_Rol", user.Rol));

                var resultado = await command.ExecuteScalarAsync();
                return Convert.ToInt32(resultado);
            }
            finally
            {
                await _context.Database.CloseConnectionAsync();
            }
        }
    }
}
