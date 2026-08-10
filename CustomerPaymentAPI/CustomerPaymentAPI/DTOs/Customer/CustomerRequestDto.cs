namespace CustomerPaymentAPI.DTOs.Customer
{
    // El patrón DTO (Data Transfer Object) se usa para transportar datos entre el cliente (frontend) y el servidor (backend).
    // Separamos el Request del Entity para evitar 'Overposting' (ataques donde se intentan modificar campos protegidos como ID o Activo)
    // y para validar los datos de entrada sin ensuciar la entidad del dominio.
    public class CustomerRequestDto
    {
        public string Nombre { get; set; }
        public string Email { get; set; }
        public string? Telefono { get; set; }
        public string? Direccion { get; set; }
    }
}
