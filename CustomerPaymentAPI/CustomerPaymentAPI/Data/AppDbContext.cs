using Microsoft.EntityFrameworkCore;
using CustomerPaymentAPI.Entities;

namespace CustomerPaymentAPI.Data
{
    public class AppDbContext : DbContext
    {
        public AppDbContext(DbContextOptions<AppDbContext> options) : base(options)
        {
        }

        public DbSet<Customer> Customers { get; set; }
        public DbSet<Payment> Payments { get; set; }
        public DbSet<User> Users { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            // Usamos DbContext principalmente para mapear los resultados de los SPs (con FromSqlRaw).
            // Configuramos los DbSets que servirán como receptores de esos resultados.

            modelBuilder.Entity<Customer>(entity => 
            { 
                entity.ToTable("Customers"); 
                entity.HasKey(e => e.Id); 
            });

            // Configuramos la relación 1:N entre Customer y Payment.
            // Restringimos el borrado en cascada (DeleteBehavior.Restrict) para proteger la integridad referencial.
            modelBuilder.Entity<Payment>(entity => 
            { 
                entity.ToTable("Payments"); 
                entity.HasKey(e => e.Id); 
                entity.HasOne(p => p.Customer)
                      .WithMany(c => c.Payments)
                      .HasForeignKey(p => p.CustomerId)
                      .OnDelete(DeleteBehavior.Restrict); 
                
                entity.Property(p => p.Monto).HasColumnType("decimal(18,2)"); 
                
                // Ignoramos CustomerNombre porque es un campo calculado por el SP (JOIN), 
                // por lo tanto no existe físicamente en la tabla Payments.
                entity.Ignore(p => p.CustomerNombre); 
            });

            modelBuilder.Entity<User>(entity => 
            { 
                entity.ToTable("Users"); 
                entity.HasKey(e => e.Id); 
            });

            // En esta arquitectura, EF Core se usa primordialmente para gestionar la conexión y ejecutar los SPs, 
            // no para autogenerar el esquema (Migraciones), ya que los Scripts SQL (SPs) tomarán el control de la lógica.
        }
    }
}
