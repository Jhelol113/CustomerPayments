namespace CustomerPaymentAPI.DTOs.Payment
{
    // =====================================================================
    // TUTOR IA: DTO DE REQUEST PARA PAYMENT
    // =====================================================================
    // Este DTO se usa tanto para CREAR como para ACTUALIZAR pagos.
    // La diferencia clave está en el campo 'Estado':
    //
    // - Al CREAR: Estado no se envía (es opcional/null). El SP usa el valor
    //   por defecto de la tabla: 'Pendiente'.
    // - Al ACTUALIZAR: Estado SÍ se puede enviar para cambiar el estado
    //   del pago (Pendiente → Completado → Cancelado).
    //
    // ¿Por qué no crear dos DTOs separados (CreateDto y UpdateDto)?
    // Se podría, pero para este sistema los campos son casi idénticos.
    // Usar un solo DTO con campos opcionales es más pragmático y reduce
    // la cantidad de clases a mantener. En sistemas más grandes, DTOs
    // separados ofrecen mejor claridad.
    // =====================================================================
    public class PaymentRequestDto
    {
        // TUTOR IA: El CustomerId vincula este pago a un cliente existente.
        // La capa Service validará que este cliente exista antes de crear el pago.
        public int CustomerId { get; set; }

        // TUTOR IA: Range valida que el monto sea positivo (mayor a 0.01).
        // No tiene sentido crear un pago de $0.00 o negativo.
        public decimal Monto { get; set; }

        // TUTOR IA: Ejemplos de métodos de pago: 'Efectivo', 'Tarjeta', 'Transferencia'.
        public string MetodoPago { get; set; } = string.Empty;

        // TUTOR IA: Estado es OPCIONAL en este DTO.
        // - En la creación: no se envía → el SP usa 'Pendiente' por defecto.
        // - En la actualización: se puede enviar para cambiar el estado.
        // Valores válidos: 'Pendiente', 'Completado', 'Cancelado'.
        public string? Estado { get; set; }
    }
}
