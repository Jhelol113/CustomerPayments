using CustomerPaymentAPI.DTOs.Payment;
using CustomerPaymentAPI.Entities;
using CustomerPaymentAPI.Repositories.Interfaces;
using CustomerPaymentAPI.Services.Interfaces;

namespace CustomerPaymentAPI.Services.Implementations
{
    // =====================================================================
    // IMPLEMENTACIÓN DEL SERVICIO DE PAYMENT
    // =====================================================================
    // Este servicio tiene una particularidad que lo diferencia de CustomerService:
    // VALIDACIÓN CRUZADA entre entidades.
    //
    // Cuando se crea o actualiza un pago, debemos verificar que el Customer
    // referenciado exista. Esto es una regla de NEGOCIO, no de base de datos:
    // - La BD tiene una FK que PREVIENE inserciones inválidas (con un error SQL)
    // - El Service ANTICIPA el problema y devuelve un mensaje amigable
    //
    // Por eso inyectamos DOS repositorios: IPaymentRepository e ICustomerRepository.
    // El Service es la capa ideal para orquestar validaciones entre entidades.
    // =====================================================================
    public class PaymentService : IPaymentService
    {
        private readonly IPaymentRepository _paymentRepository;
        private readonly ICustomerRepository _customerRepository;

        // Inyectamos ambos repositorios.
        // El Service es la ÚNICA capa que puede coordinar múltiples repositorios.
        // El Repository solo conoce su propia entidad.
        public PaymentService(
            IPaymentRepository paymentRepository,
            ICustomerRepository customerRepository)
        {
            _paymentRepository = paymentRepository;
            _customerRepository = customerRepository;
        }

        // =====================================================================
        // MÉTODO: GetAllAsync — Obtener pagos (todos o filtrados por cliente)
        // =====================================================================
        // El parámetro customerId es optional (nullable int).
        // - Si es null → el Repository pasa NULL al SP → trae todos los pagos
        // - Si tiene valor → filtra por ese CustomerId
        //
        // El ResponseDto incluye CustomerNombre (viene del JOIN en el SP),
        // así el frontend muestra "Pago de Juan Pérez" sin consultas adicionales.
        // =====================================================================
        public async Task<IEnumerable<PaymentResponseDto>> GetAllAsync(int? customerId = null)
        {
            var payments = await _paymentRepository.GetAllAsync(customerId);
            return payments.Select(MapToResponseDto);
        }

        // =====================================================================
        // MÉTODO: GetByIdAsync — Obtener un pago específico como DTO
        // =====================================================================
        public async Task<PaymentResponseDto?> GetByIdAsync(int id)
        {
            var payment = await _paymentRepository.GetByIdAsync(id);
            return payment != null ? MapToResponseDto(payment) : null;
        }

        // =====================================================================
        // MÉTODO: CreateAsync — Crear un nuevo pago
        // =====================================================================
        // VALIDACIÓN CRUZADA — Antes de crear el pago, verificamos
        // que el Customer referenciado exista y esté activo.
        //
        // ¿Por qué lanzamos una excepción en vez de retornar null?
        // Porque la creación de un pago sin cliente válido es un ERROR DE DATOS
        // del usuario, no una "no encontrado". El Controller capturará esta
        // excepción y la convertirá en HTTP 400 (Bad Request).
        //
        // Si solo retornáramos null, el Controller no sabría si falló por
        // un error técnico o por datos inválidos.
        // =====================================================================
        public async Task<PaymentResponseDto> CreateAsync(PaymentRequestDto dto)
        {
            // Validación cruzada — verificar que el cliente existe.
            var customer = await _customerRepository.GetByIdAsync(dto.CustomerId);
            if (customer == null)
            {
                throw new ArgumentException(
                    $"No se puede crear el pago: el cliente con Id {dto.CustomerId} no existe o está inactivo.");
            }

            var entity = MapToEntity(dto);
            var nuevoId = await _paymentRepository.CreateAsync(entity);

            // Recuperamos el pago completo (con CustomerNombre del JOIN).
            var creado = await _paymentRepository.GetByIdAsync(nuevoId);
            return MapToResponseDto(creado!);
        }

        // =====================================================================
        // MÉTODO: UpdateAsync — Actualizar un pago existente
        // =====================================================================
        // Doble validación:
        // 1. El pago debe existir (por su Id)
        // 2. El nuevo CustomerId (si cambió) debe referenciar un cliente válido
        //
        // Para el campo Estado: si el DTO no trae Estado (null), usamos el
        // estado actual del pago. Esto permite que el frontend envíe solo los
        // campos que quiere actualizar sin tener que enviar siempre el Estado.
        // =====================================================================
        public async Task<bool> UpdateAsync(int id, PaymentRequestDto dto)
        {
            // Validación 1: ¿Existe el pago?
            var existente = await _paymentRepository.GetByIdAsync(id);
            if (existente == null)
                return false;

            // Validación 2: ¿Existe el cliente referenciado?
            var customer = await _customerRepository.GetByIdAsync(dto.CustomerId);
            if (customer == null)
            {
                throw new ArgumentException(
                    $"No se puede actualizar el pago: el cliente con Id {dto.CustomerId} no existe o está inactivo.");
            }

            var entity = MapToEntity(dto);
            entity.Id = id;

            // Si no se envió un Estado en el DTO, mantenemos el actual.
            // Esto evita que un update accidental resetee el estado a null.
            if (string.IsNullOrEmpty(entity.Estado))
            {
                entity.Estado = existente.Estado;
            }

            return await _paymentRepository.UpdateAsync(entity);
        }

        // =====================================================================
        // MÉTODO: DeleteAsync — Eliminar un pago (hard delete)
        // =====================================================================
        // A diferencia de Customer (soft delete), los pagos se
        // eliminan permanentemente. Validamos existencia antes de intentar.
        // =====================================================================
        public async Task<bool> DeleteAsync(int id)
        {
            var existente = await _paymentRepository.GetByIdAsync(id);
            if (existente == null)
                return false;

            return await _paymentRepository.DeleteAsync(id);
        }

        public async Task<bool> UpdateStatusAsync(int id, string estado)
        {
            var estadosValidos = new[] { "Completado", "Pendiente", "Fallido" };
            if (!estadosValidos.Contains(estado))
            {
                throw new ArgumentException($"Estado '{estado}' no es válido.");
            }

            var existente = await _paymentRepository.GetByIdAsync(id);
            if (existente == null)
                return false;

            return await _paymentRepository.UpdateStatusAsync(id, estado);
        }

        // =====================================================================
        // MÉTODOS PRIVADOS DE MAPEO
        // =====================================================================
        // Nótese que MapToResponseDto incluye CustomerNombre,
        // que viene del JOIN que hace el SP con la tabla Customers.
        // Este campo permite al frontend mostrar "Pago de [nombre]" sin
        // necesidad de hacer otra llamada a la API para obtener los datos
        // del cliente. Es un ejemplo de DESNORMALIZACIÓN en el DTO.
        // =====================================================================

        private static PaymentResponseDto MapToResponseDto(Payment entity)
        {
            return new PaymentResponseDto
            {
                Id = entity.Id,
                CustomerId = entity.CustomerId,
                Monto = entity.Monto,
                MetodoPago = entity.MetodoPago,

                FechaPago = entity.FechaPago,
                Estado = entity.Estado,
                CustomerNombre = entity.CustomerNombre,
                FechaCreacion = entity.FechaCreacion
            };
        }

        private static Payment MapToEntity(PaymentRequestDto dto)
        {
            return new Payment
            {
                CustomerId = dto.CustomerId,
                Monto = dto.Monto,
                MetodoPago = dto.MetodoPago,

                // Estado puede ser null (en creación). Si es null,
                // el SP usa 'Pendiente' como valor por defecto de la tabla.
                // En actualización, el método UpdateAsync se encarga de
                // asignar el estado actual si no se envió uno nuevo.
                Estado = dto.Estado ?? string.Empty
            };
        }
    }
}
