using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace CustomerPaymentAPI.Entities
{
    // TUTOR IA: Una Entidad (Entity) es una clase que representa una tabla en la base de datos.
    // Usamos 'Data Annotations' (como [Key], [Required]) para configurar cómo se mapean estas 
    // propiedades a columnas en la base de datos sin necesidad de configuraciones extra complejas.
    public class Customer
    {
        // TUTOR IA: [Key] indica que esta propiedad será la Clave Primaria.
        // DatabaseGeneratedOption.Identity le dice a la base de datos que el valor será autoincrementable.
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int Id { get; set; }

        // TUTOR IA: [Required] hace que el campo sea obligatorio (NOT NULL en SQL).
        // [MaxLength] define el tamaño máximo del campo (VARCHAR(100)).
        [Required]
        [MaxLength(100)]
        public string Nombre { get; set; }

        [Required]
        [MaxLength(100)]
        public string Email { get; set; }

        [MaxLength(20)]
        public string? Telefono { get; set; }

        [MaxLength(255)]
        public string? Direccion { get; set; }

        public DateTime FechaCreacion { get; set; }

        public bool Activo { get; set; }

        // TUTOR IA: Esta es una 'Propiedad de Navegación'. No se guarda como una columna en la tabla 'Customers',
        // sino que Entity Framework la usa para representar la relación 1:N con la tabla 'Payments'.
        // Inicializamos con 'new List<Payment>()' para evitar excepciones de referencia nula al agregar pagos.
        public ICollection<Payment> Payments { get; set; } = new List<Payment>();
    }
}
