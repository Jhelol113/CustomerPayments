using CustomerPaymentAPI.Entities;

namespace CustomerPaymentAPI.Repositories.Interfaces
{
    // =====================================================================
    // INTERFAZ DEL REPOSITORIO DE PAYMENT
    // =====================================================================
    // Esta interfaz sigue exactamente el mismo patrón que ICustomerRepository.
    // La diferencia principal está en GetAllAsync, que recibe un parámetro
    // opcional 'customerId' para filtrar pagos por cliente.
    //
    // ¿Por qué customerId es nullable (int?)?
    // Porque el SP sp_Payment_GetAll tiene un parámetro p_CustomerId que:
    // - Si es NULL → retorna TODOS los pagos
    // - Si tiene valor → filtra solo los pagos de ese cliente
    // Esto nos permite reutilizar el mismo SP para dos casos de uso distintos.
    // =====================================================================
    public interface IPaymentRepository
    {
        /// <summary>
        /// Obtiene pagos. Si customerId es null, trae todos.
        /// Si tiene valor, filtra por ese cliente.
        /// Usa el SP sp_Payment_GetAll con JOIN para incluir CustomerNombre.
        /// </summary>
        Task<IEnumerable<Payment>> GetAllAsync(int? customerId = null);

        /// <summary>
        /// Obtiene un pago específico por Id con JOIN para CustomerNombre.
        /// Usa el SP sp_Payment_GetById.
        /// </summary>
        Task<Payment?> GetByIdAsync(int id);

        /// <summary>
        /// Crea un nuevo pago usando el SP sp_Payment_Create.
        /// Retorna el Id generado (LAST_INSERT_ID).
        /// </summary>
        Task<int> CreateAsync(Payment payment);

        /// <summary>
        /// Actualiza un pago existente usando el SP sp_Payment_Update.
        /// Retorna true si se actualizó al menos una fila.
        /// </summary>
        Task<bool> UpdateAsync(Payment payment);

        /// <summary>
        /// Elimina un pago (hard delete) usando el SP sp_Payment_Delete.
        /// Retorna true si se eliminó al menos una fila.
        /// </summary>
        Task<bool> DeleteAsync(int id);

        Task<bool> UpdateStatusAsync(int id, string estado);
    }
}
