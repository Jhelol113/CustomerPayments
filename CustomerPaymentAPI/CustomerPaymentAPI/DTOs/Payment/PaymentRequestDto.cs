namespace CustomerPaymentAPI.DTOs.Payment
{
    // =====================================================================
    // DTO DE REQUEST PARA PAYMENT
    // =====================================================================
    // Este DTO se usa tanto para CREAR como para ACTUALIZAR pagos.
    // La diferencia clave está en el campo 'Estado':
    //
    // - Al CREAR: Estado no se envía (es opcional/null). El SP usa el valor
    //   por defecto de la tabla: 'Pendiente'.
    // - Al ACTUALIZAR: Estado SÍ se puede enviar para cambiar el estado
    //   del pago (Pendiente → Completado → Cancelado).
    //
    // =====================================================================
    public class PaymentRequestDto
    {
        // El CustomerId vincula este pago a un cliente existente.
        // La capa Service validará que este cliente exista antes de crear el pago.
        public int CustomerId { get; set; }

        // Range valida que el monto sea positivo (mayor a 0.01).
        // No tiene sentido crear un pago de $0.00 o negativo.
        public decimal Monto { get; set; }

        // Ejemplos de métodos de pago: 'Efectivo', 'Tarjeta', 'Transferencia'.
        public string MetodoPago { get; set; } = string.Empty;

        // Estado es OPCIONAL en este DTO.
        // - En la creación: no se envía → el SP usa 'Pendiente' por defecto.
        // - En la actualización: se puede enviar para cambiar el estado.
        // Valores válidos: 'Pendiente', 'Completado', 'Cancelado'.
        public string? Estado { get; set; }
    }
}
