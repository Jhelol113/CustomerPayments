using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace CustomerPaymentAPI.Entities
{
    public class Payment
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int Id { get; set; }

        // TUTOR IA: CustomerId actuará como una Clave Foránea (Foreign Key) 
        // vinculando este pago a un cliente específico en la tabla Customers.
        public int CustomerId { get; set; }

        // TUTOR IA: [Column] especifica el tipo exacto en SQL.
        // En este caso, indicamos que Monto será DECIMAL de 18 dígitos en total, con 2 decimales.
        [Column(TypeName = "decimal(18,2)")]
        public decimal Monto { get; set; }

        [Required]
        [MaxLength(50)]
        public string MetodoPago { get; set; }

        public DateTime FechaPago { get; set; }

        public DateTime FechaCreacion { get; set; }

        [Required]
        [MaxLength(20)]
        public string Estado { get; set; }

        // TUTOR IA: [ForeignKey] indica explícitamente a qué propiedad foránea está ligada esta relación N:1.
        // Esto permite navegar desde un Payment hacia los datos de su Customer asociado.
        [ForeignKey("CustomerId")]
        public Customer Customer { get; set; }

        // TUTOR IA: [NotMapped] le dice a Entity Framework que NO intente crear una columna para esta propiedad en la base de datos.
        // La usamos exclusivamente para recibir campos calculados o datos producto de un JOIN al ejecutar Procedimientos Almacenados.
        [NotMapped]
        public string? CustomerNombre { get; set; }
    }
}
