using CustomerPaymentAPI.Entities;

namespace CustomerPaymentAPI.Repositories.Interfaces
{
    // =====================================================================
    // TUTOR IA: INTERFAZ DEL REPOSITORIO DE CUSTOMER
    // =====================================================================
    // ¿Por qué usamos una interfaz?
    // 1. DESACOPLAMIENTO: La capa de negocio (Service) NO conoce la implementación
    //    concreta. Solo sabe que existe un contrato con estos métodos.
    // 2. INYECCIÓN DE DEPENDENCIAS: En Program.cs registraremos:
    //    builder.Services.AddScoped<ICustomerRepository, CustomerRepository>();
    //    Así .NET inyecta automáticamente la implementación correcta.
    // 3. TESTABILIDAD: Podemos crear "mocks" de esta interfaz para pruebas unitarias
    //    sin necesidad de una base de datos real.
    //
    // Flujo: Controller → IService → Service → IRepository → Repository → SP MySQL
    // =====================================================================
    public interface ICustomerRepository
    {
        /// <summary>
        /// Obtiene todos los clientes activos desde el SP sp_Customer_GetAll.
        /// </summary>
        Task<IEnumerable<Customer>> GetAllAsync();

        /// <summary>
        /// Obtiene un cliente específico por su Id desde el SP sp_Customer_GetById.
        /// Retorna null si no se encuentra.
        /// </summary>
        Task<Customer?> GetByIdAsync(int id);

        /// <summary>
        /// Crea un nuevo cliente usando el SP sp_Customer_Create.
        /// Retorna el Id generado por la base de datos (LAST_INSERT_ID).
        /// </summary>
        Task<int> CreateAsync(Customer customer);

        /// <summary>
        /// Actualiza un cliente existente usando el SP sp_Customer_Update.
        /// Retorna true si se actualizó al menos una fila.
        /// </summary>
        Task<bool> UpdateAsync(Customer customer);

        /// <summary>
        /// Realiza un soft delete usando el SP sp_Customer_Delete.
        /// Retorna true si se desactivó al menos una fila.
        /// </summary>
        Task<bool> DeleteAsync(int id);
    }
}
