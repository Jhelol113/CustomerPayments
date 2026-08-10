using CustomerPaymentAPI.Entities;

namespace CustomerPaymentAPI.Repositories.Interfaces
{
    // =====================================================================
    //  INTERFAZ DEL REPOSITORIO DE USER
    // =====================================================================
    // A diferencia de Customer y Payment, el repositorio de User NO tiene
    // CRUD completo. Solo necesitamos dos operaciones para autenticación:
    //
    // 1. GetByUsernameAsync: Buscar un usuario por su nombre de usuario
    //    durante el proceso de login. El Service comparará el password
    //    ingresado contra el PasswordHash almacenado usando BCrypt.
    //
    // 2. CreateAsync: Crear nuevos usuarios (registro). El Service se
    //    encargará de hashear la contraseña ANTES de llamar a este método.
    //
    // ¿Por qué no hay Update ni Delete?
    // Para este sistema, la gestión completa de usuarios no es un requisito.
    // Se puede agregar en el futuro extendiendo esta interfaz sin romper
    // el código existente (Principio Open/Closed de SOLID).
    // =====================================================================
    public interface IUserRepository
    {
        /// <summary>
        /// Busca un usuario activo por su Username.
        /// Usa el SP sp_User_GetByUsername.
        /// Retorna null si el usuario no existe o está inactivo.
        /// </summary>
        Task<User?> GetByUsernameAsync(string username);

        /// <summary>
        /// Crea un nuevo usuario en el sistema.
        /// Usa el SP sp_User_Create.
        /// Retorna el Id generado.
        /// IMPORTANTE: El PasswordHash debe estar ya encriptado con BCrypt.
        /// </summary>
        Task<int> CreateAsync(User user);
    }
}
