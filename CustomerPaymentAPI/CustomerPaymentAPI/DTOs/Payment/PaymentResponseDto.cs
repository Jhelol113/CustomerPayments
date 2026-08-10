namespace CustomerPaymentAPI.DTOs.Payment
{
    public class PaymentResponseDto
    {
        public int Id { get; set; }
        public int CustomerId { get; set; }
        public decimal Monto { get; set; }
        public string MetodoPago { get; set; }
        public DateTime FechaPago { get; set; }
        public string Estado { get; set; }
        public DateTime FechaCreacion { get; set; }
        
        // Aplanamos los datos que vienen del JOIN (CustomerNombre) directamente en el DTO de respuesta.
        // Esto facilita el trabajo en el frontend para mostrar el nombre del cliente en tablas de pagos sin hacer consultas extra.
        public string? CustomerNombre { get; set; }
    }
}
