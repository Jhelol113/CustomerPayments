using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace CustomerPaymentAPI.Entities
{
    public class User
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int Id { get; set; }

        [Required]
        [MaxLength(50)]
        public string Username { get; set; }

        // NUNCA se debe almacenar la contraseña en texto plano por motivos de seguridad.
        // En lugar de eso, almacenamos un 'Hash'. BCrypt es un algoritmo de hashing seguro que 
        // incorpora 'salts' automáticamente, haciéndolo resistente a ataques de diccionario y fuerza bruta.
        [Required]
        [MaxLength(255)]
        public string PasswordHash { get; set; }

        [Required]
        [MaxLength(20)]
        public string Rol { get; set; }

        public DateTime FechaCreacion { get; set; }

        public bool Activo { get; set; }
    }
}
