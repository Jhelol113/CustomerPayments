using CustomerPaymentAPI.DTOs.Customer;

namespace CustomerPaymentAPI.Services.Interfaces
{
    // =====================================================================
    // INTERFAZ DEL SERVICIO DE CUSTOMER
    // =====================================================================
    // La capa de Servicio es la responsable de:
    // 1. VALIDAR reglas de negocio (¿existe el cliente? ¿datos válidos?)
    // 2. MAPEAR entre DTOs y Entidades (el Controller nunca ve Entidades)
    // 3. ORQUESTAR llamadas al repositorio
    //
    // ¿Por qué los métodos reciben/retornan DTOs y no Entidades?
    // Porque el Controller (que consume este servicio) solo debe trabajar
    // con DTOs. Las Entidades son objetos internos de la capa de datos.
    // Esto crea una BARRERA DE ABSTRACCIÓN: si cambias la tabla en la BD,
    // solo necesitas ajustar el mapeo en el Service, no el Controller.
    //
    // Flujo: Controller → IService.Method(DTO) → Service → IRepository → SP
    //        Controller ← IService retorna DTO ← Service mapea Entity→DTO
    // =====================================================================
    public interface ICustomerService
    {
        /// <summary>
        /// Obtiene todos los clientes activos como DTOs de respuesta.
        /// </summary>
        Task<IEnumerable<CustomerResponseDto>> GetAllAsync();

        /// <summary>
        /// Obtiene un cliente por Id. Retorna null si no existe.
        /// </summary>
        Task<CustomerResponseDto?> GetByIdAsync(int id);

        /// <summary>
        /// Crea un nuevo cliente a partir del DTO de request.
        /// Retorna el DTO de respuesta con el Id generado y FechaCreacion.
        /// </summary>
        Task<CustomerResponseDto> CreateAsync(CustomerRequestDto dto);

        /// <summary>
        /// Actualiza un cliente existente. Retorna false si el cliente no existe.
        /// </summary>
        Task<bool> UpdateAsync(int id, CustomerRequestDto dto);

        /// <summary>
        /// Desactiva un cliente (soft delete). Retorna false si no existe.
        /// </summary>
        Task<bool> DeleteAsync(int id);
    }
}
