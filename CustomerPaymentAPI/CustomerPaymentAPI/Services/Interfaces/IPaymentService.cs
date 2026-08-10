using CustomerPaymentAPI.DTOs.Payment;

namespace CustomerPaymentAPI.Services.Interfaces
{
    // =====================================================================
    // INTERFAZ DEL SERVICIO DE PAYMENT
    // =====================================================================
    // Sigue el mismo patrón que ICustomerService pero con una diferencia:
    //
    // - GetAllAsync recibe un 'customerId' opcional para filtrar pagos
    //   por cliente. Esto se traduce directamente al parámetro p_CustomerId
    //   del SP sp_Payment_GetAll.
    //
    // - CreateAsync incluye una validación de negocio CRUZADA: antes de
    //   crear un pago, el Service debe verificar que el CustomerId
    //   referenciado exista en la tabla Customers. Esta validación
    //   NO está en la BD (la FK solo previene inserciones inválidas
    //   con un error, pero nosotros queremos un mensaje amigable).
    // =====================================================================
    public interface IPaymentService
    {
        /// <summary>
        /// Obtiene pagos. Si customerId es null, trae todos.
        /// Si tiene valor, filtra por ese cliente.
        /// </summary>
        Task<IEnumerable<PaymentResponseDto>> GetAllAsync(int? customerId = null);

        /// <summary>
        /// Obtiene un pago por Id con nombre del cliente incluido.
        /// </summary>
        Task<PaymentResponseDto?> GetByIdAsync(int id);

        /// <summary>
        /// Crea un nuevo pago. Valida que el cliente exista primero.
        /// Lanza ArgumentException si el cliente no existe.
        /// </summary>
        Task<PaymentResponseDto> CreateAsync(PaymentRequestDto dto);

        /// <summary>
        /// Actualiza un pago existente. Retorna false si no existe.
        /// </summary>
        Task<bool> UpdateAsync(int id, PaymentRequestDto dto);

        /// <summary>
        /// Elimina un pago (hard delete). Retorna false si no existe.
        /// </summary>
        Task<bool> DeleteAsync(int id);

        Task<bool> UpdateStatusAsync(int id, string estado);
    }
}
