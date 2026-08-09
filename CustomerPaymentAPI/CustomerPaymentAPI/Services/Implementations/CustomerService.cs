using CustomerPaymentAPI.DTOs.Customer;
using CustomerPaymentAPI.Entities;
using CustomerPaymentAPI.Repositories.Interfaces;
using CustomerPaymentAPI.Services.Interfaces;

namespace CustomerPaymentAPI.Services.Implementations
{
    // =====================================================================
    // TUTOR IA: IMPLEMENTACIÓN DEL SERVICIO DE CUSTOMER
    // =====================================================================
    // Esta clase contiene la LÓGICA DE NEGOCIO para la entidad Customer.
    // Su responsabilidad principal es:
    //
    // 1. RECIBIR DTOs del Controller (nunca Entidades)
    // 2. VALIDAR reglas de negocio (¿existe el cliente a actualizar?)
    // 3. MAPEAR DTOs ↔ Entidades (conversión manual entre capas)
    // 4. DELEGAR al repositorio (nunca accede directamente a la BD)
    // 5. RETORNAR DTOs al Controller (nunca Entidades)
    //
    // ¿Por qué mapeamos manualmente y no usamos AutoMapper?
    // Para un sistema de este tamaño, el mapeo manual es más transparente
    // y educativo. AutoMapper agrega complejidad y puede ocultar errores.
    // En sistemas grandes con muchos DTOs, AutoMapper sí vale la pena.
    //
    // PRINCIPIO DE RESPONSABILIDAD ÚNICA (SRP):
    // El Service NO sabe cómo se ejecutan los SPs (eso es del Repository).
    // El Service NO sabe qué HTTP status devolver (eso es del Controller).
    // El Service SOLO sabe las reglas de negocio.
    // =====================================================================
    public class CustomerService : ICustomerService
    {
        // TUTOR IA: Inyectamos la INTERFAZ del repositorio, no la implementación.
        // Esto permite cambiar la implementación sin tocar este código.
        private readonly ICustomerRepository _customerRepository;
        private readonly IPaymentRepository _paymentRepository;

        public CustomerService(ICustomerRepository customerRepository, IPaymentRepository paymentRepository)
        {
            _customerRepository = customerRepository;
            _paymentRepository = paymentRepository;
        }

        // =====================================================================
        // MÉTODO: GetAllAsync — Obtener todos los clientes como DTOs
        // =====================================================================
        // TUTOR IA: Flujo completo de una petición GET /api/customers:
        // 1. Controller llama a _customerService.GetAllAsync()
        // 2. Este método llama a _customerRepository.GetAllAsync()
        // 3. El Repository ejecuta el SP sp_Customer_GetAll
        // 4. MySQL retorna las filas → Repository las convierte en List<Customer>
        // 5. Este método mapea cada Customer a CustomerResponseDto
        // 6. Controller recibe los DTOs y los devuelve como JSON (HTTP 200)
        // =====================================================================
        public async Task<IEnumerable<CustomerResponseDto>> GetAllAsync()
        {
            var customers = await _customerRepository.GetAllAsync();

            // TUTOR IA: .Select() aplica la función MapToResponseDto a cada elemento.
            // Es equivalente a un foreach que crea una lista de DTOs.
            return customers.Select(MapToResponseDto);
        }

        // =====================================================================
        // MÉTODO: GetByIdAsync — Obtener un cliente específico como DTO
        // =====================================================================
        // TUTOR IA: Retorna null si el cliente no existe. El Controller
        // convertirá ese null en una respuesta HTTP 404 (Not Found).
        // =====================================================================
        public async Task<CustomerResponseDto?> GetByIdAsync(int id)
        {
            var customer = await _customerRepository.GetByIdAsync(id);

            // TUTOR IA: Operador ternario: si customer no es null, mapeamos.
            // Si es null, retornamos null directamente.
            return customer != null ? MapToResponseDto(customer) : null;
        }

        // =====================================================================
        // MÉTODO: CreateAsync — Crear un nuevo cliente
        // =====================================================================
        // TUTOR IA: Flujo de creación:
        // 1. Convertimos el DTO de request a una entidad Customer
        // 2. El Repository ejecuta sp_Customer_Create y retorna el nuevo Id
        // 3. Buscamos el cliente recién creado con GetByIdAsync para obtener
        //    todos sus datos (incluyendo FechaCreacion generada por MySQL)
        // 4. Mapeamos la entidad completa a un DTO de respuesta
        //
        // ¿Por qué no simplemente retornar el DTO con el Id?
        // Porque necesitamos campos que la BD genera automáticamente:
        // FechaCreacion (CURRENT_TIMESTAMP) y Activo (TRUE por defecto).
        // Solo el SP nos da esos valores reales.
        // =====================================================================
        public async Task<CustomerResponseDto> CreateAsync(CustomerRequestDto dto)
        {
            var entity = MapToEntity(dto);
            var nuevoId = await _customerRepository.CreateAsync(entity);

            // TUTOR IA: Obtenemos el registro completo recién creado.
            // El operador '!' indica que confiamos en que el registro existe
            // (acabamos de crearlo, sería un error crítico si no existiera).
            var creado = await _customerRepository.GetByIdAsync(nuevoId);
            return MapToResponseDto(creado!);
        }

        // =====================================================================
        // MÉTODO: UpdateAsync — Actualizar un cliente existente
        // =====================================================================
        // TUTOR IA: VALIDACIÓN DE NEGOCIO — Verificamos que el cliente exista
        // antes de intentar actualizarlo. Esto evita que el SP haga un UPDATE
        // que no afecta ninguna fila sin que el usuario sepa por qué.
        //
        // Parámetros:
        // - id: viene de la URL (ej: PUT /api/customers/5)
        // - dto: viene del body de la petición con los nuevos datos
        // =====================================================================
        public async Task<bool> UpdateAsync(int id, CustomerRequestDto dto)
        {
            // TUTOR IA: Primero verificamos existencia.
            // Si el cliente no existe, retornamos false.
            // El Controller convertirá false en HTTP 404.
            var existente = await _customerRepository.GetByIdAsync(id);
            if (existente == null)
                return false;

            // TUTOR IA: Mapeamos el DTO a entidad y asignamos el Id de la URL.
            // El Id viene de la ruta, no del body, por seguridad:
            // así evitamos que el cliente manipule el Id en el JSON.
            var entity = MapToEntity(dto);
            entity.Id = id;

            return await _customerRepository.UpdateAsync(entity);
        }

        // =====================================================================
        // MÉTODO: DeleteAsync — Desactivar un cliente (soft delete)
        // =====================================================================
        // TUTOR IA: Verificamos existencia antes de intentar el soft delete.
        // El SP sp_Customer_Delete solo marca Activo = FALSE.
        // =====================================================================
        public async Task<bool> DeleteAsync(int id)
        {
            // Verificar que el cliente exista
            var existente = await _customerRepository.GetByIdAsync(id);
            if (existente == null)
                return false;

            // REGLA DE NEGOCIO: No permitir eliminar si tiene pagos pendientes
            var pagos = await _paymentRepository.GetAllAsync(id);
            var pagosPendientes = pagos.Where(p => p.Estado == "Pendiente").ToList();
            if (pagosPendientes.Any())
            {
                throw new InvalidOperationException(
                    $"No se puede eliminar el cliente '{existente.Nombre}' porque tiene {pagosPendientes.Count} pago(s) en estado 'Pendiente'. Debe completar o eliminar los pagos primero.");
            }

            return await _customerRepository.DeleteAsync(id);
        }

        // =====================================================================
        // MÉTODOS PRIVADOS DE MAPEO
        // =====================================================================
        // TUTOR IA: Estos métodos convierten entre las dos representaciones:
        //
        // Entity → ResponseDto: Para ENVIAR datos al frontend.
        //   Solo incluye los campos que el frontend necesita ver.
        //
        // RequestDto → Entity: Para RECIBIR datos del frontend.
        //   Solo toma los campos que el usuario puede ingresar.
        //   Campos como Id, FechaCreacion, Activo los maneja la BD.
        // =====================================================================

        private static CustomerResponseDto MapToResponseDto(Customer entity)
        {
            return new CustomerResponseDto
            {
                Id = entity.Id,
                Nombre = entity.Nombre,
                Email = entity.Email,
                Telefono = entity.Telefono,
                Direccion = entity.Direccion,
                FechaCreacion = entity.FechaCreacion,
                Activo = entity.Activo
            };
        }

        private static Customer MapToEntity(CustomerRequestDto dto)
        {
            return new Customer
            {
                Nombre = dto.Nombre,
                Email = dto.Email,
                Telefono = dto.Telefono,
                Direccion = dto.Direccion
            };
        }
    }
}
