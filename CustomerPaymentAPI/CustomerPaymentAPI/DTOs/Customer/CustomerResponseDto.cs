namespace CustomerPaymentAPI.DTOs.Customer
{
    // Este DTO de respuesta solo expone lo que el cliente necesita ver.
    // Por ejemplo, podríamos omitir información sensible de la entidad original.
    // Además, mantiene un contrato estable con el frontend, independiente de cómo cambie la base de datos.
    public class CustomerResponseDto
    {
        public int Id { get; set; }
        public string Nombre { get; set; }
        public string Email { get; set; }
        public string? Telefono { get; set; }
        public string? Direccion { get; set; }
        public DateTime FechaCreacion { get; set; }
        public bool Activo { get; set; }
    }
}
