using Microsoft.EntityFrameworkCore;
using MySqlConnector;
using System.Data;
using CustomerPaymentAPI.Data;
using CustomerPaymentAPI.Entities;
using CustomerPaymentAPI.Repositories.Interfaces;

namespace CustomerPaymentAPI.Repositories.Implementations
{
    // =====================================================================
    // TUTOR IA: IMPLEMENTACIÓN DEL REPOSITORIO DE CUSTOMER
    // =====================================================================
    // Esta clase es la que REALMENTE ejecuta los Stored Procedures contra MySQL.
    // Aquí usamos DOS técnicas complementarias de Entity Framework Core:
    //
    // TÉCNICA 1 — FromSqlRaw (para LECTURA / SELECT):
    //   Se usa en GetAll y GetById porque los SPs retornan columnas que
    //   coinciden EXACTAMENTE con las propiedades de la entidad Customer.
    //   EF Core mapea automáticamente cada columna del resultado a una propiedad.
    //   Ejemplo: _context.Customers.FromSqlRaw("CALL sp_Customer_GetAll")
    //
    // TÉCNICA 2 — ADO.NET directo (para ESCRITURA / INSERT, UPDATE, DELETE):
    //   Se usa en Create, Update y Delete porque estos SPs retornan valores
    //   escalares (LAST_INSERT_ID, ROW_COUNT) que NO son entidades completas.
    //   FromSqlRaw no puede mapear un solo número a un objeto Customer,
    //   así que usamos la conexión ADO.NET subyacente de EF Core.
    //   Ejemplo: _context.Database.GetDbConnection() → MySqlCommand → ExecuteScalar
    //
    // ¿Por qué AsNoTracking()?
    //   Cuando hacemos consultas de solo lectura, AsNoTracking() le dice a EF Core
    //   que NO rastree los cambios de esas entidades. Esto mejora el rendimiento
    //   porque EF Core no necesita mantener una copia interna del estado original.
    // =====================================================================
    public class CustomerRepository : ICustomerRepository
    {
        // TUTOR IA: Inyectamos el AppDbContext mediante el constructor.
        // .NET lo resuelve automáticamente gracias a la Inyección de Dependencias
        // configurada en Program.cs (builder.Services.AddDbContext<AppDbContext>).
        private readonly AppDbContext _context;

        public CustomerRepository(AppDbContext context)
        {
            _context = context;
        }

        // =====================================================================
        // MÉTODO: GetAllAsync — Obtener todos los clientes activos
        // =====================================================================
        // TUTOR IA: FromSqlRaw ejecuta el SP y mapea cada fila del resultado
        // a un objeto Customer. El SP ya filtra por Activo = TRUE y ordena
        // por FechaCreacion DESC, así que no necesitamos agregar .Where() o .OrderBy().
        //
        // Flujo de ejecución:
        // 1. EF Core abre una conexión a MySQL
        // 2. Ejecuta: CALL sp_Customer_GetAll
        // 3. Lee cada fila del resultado
        // 4. Crea un objeto Customer por cada fila, asignando columna → propiedad
        // 5. Retorna la lista completa
        // =====================================================================
        public async Task<IEnumerable<Customer>> GetAllAsync()
        {
            return await _context.Customers
                .FromSqlRaw("CALL sp_Customer_GetAll")
                .AsNoTracking()
                .ToListAsync();
        }

        // =====================================================================
        // MÉTODO: GetByIdAsync — Obtener un cliente por su Id
        // =====================================================================
        // TUTOR IA: Usamos {0} como placeholder para el parámetro.
        // EF Core lo convierte automáticamente en un parámetro SQL seguro,
        // protegiendo contra inyección SQL. NUNCA concatenes valores directamente
        // en el string SQL (ejemplo INSEGURO: $"CALL sp_Customer_GetById({id})").
        //
        // ¿Por qué ToListAsync() y luego FirstOrDefault()?
        // Porque MySQL puede tener problemas al componer consultas LINQ
        // adicionales sobre el resultado de un SP. Materializar con ToList
        // primero y luego filtrar en memoria es la forma más segura.
        // =====================================================================
        public async Task<Customer?> GetByIdAsync(int id)
        {
            var resultado = await _context.Customers
                .FromSqlRaw("CALL sp_Customer_GetById({0})", id)
                .AsNoTracking()
                .ToListAsync();

            return resultado.FirstOrDefault();
        }

        // =====================================================================
        // MÉTODO: CreateAsync — Crear un nuevo cliente
        // =====================================================================
        // TUTOR IA: Aquí cambiamos a ADO.NET porque el SP retorna
        // SELECT LAST_INSERT_ID() AS Id, que es un valor escalar (un solo número),
        // no una fila completa de Customer. FromSqlRaw no puede mapear eso.
        //
        // Flujo de ejecución:
        // 1. Obtenemos la conexión subyacente de EF Core
        // 2. Creamos un MySqlCommand configurado como StoredProcedure
        // 3. Agregamos los parámetros con MySqlParameter
        // 4. ExecuteScalarAsync ejecuta el SP y retorna el primer valor del resultado
        // 5. Convertimos ese valor a int (el nuevo Id)
        //
        // ¿Por qué DBNull.Value para valores nulos?
        // MySQL no entiende el null de C#. DBNull.Value es la representación
        // de NULL en ADO.NET que MySQL sí comprende.
        // =====================================================================
        public async Task<int> CreateAsync(Customer customer)
        {
            var connection = _context.Database.GetDbConnection();

            try
            {
                await _context.Database.OpenConnectionAsync();

                using var command = connection.CreateCommand();
                command.CommandText = "sp_Customer_Create";
                command.CommandType = CommandType.StoredProcedure;

                // TUTOR IA: Cada parámetro debe coincidir con el nombre definido en el SP.
                // El prefijo '@' es opcional en MySqlConnector, pero lo incluimos por claridad.
                command.Parameters.Add(new MySqlParameter("@p_Nombre", customer.Nombre));
                command.Parameters.Add(new MySqlParameter("@p_Email", customer.Email));
                command.Parameters.Add(new MySqlParameter("@p_Telefono", (object?)customer.Telefono ?? DBNull.Value));
                command.Parameters.Add(new MySqlParameter("@p_Direccion", (object?)customer.Direccion ?? DBNull.Value));

                // TUTOR IA: ExecuteScalarAsync retorna el primer valor de la primera fila del resultado.
                // En nuestro caso, el SP hace SELECT LAST_INSERT_ID() AS Id, por lo que retorna el nuevo Id.
                var resultado = await command.ExecuteScalarAsync();
                return Convert.ToInt32(resultado);
            }
            finally
            {
                // TUTOR IA: Siempre cerramos la conexión en el bloque finally para evitar
                // conexiones huérfanas, incluso si ocurre una excepción.
                await _context.Database.CloseConnectionAsync();
            }
        }

        // =====================================================================
        // MÉTODO: UpdateAsync — Actualizar un cliente existente
        // =====================================================================
        // TUTOR IA: Similar a CreateAsync, usamos ADO.NET porque el SP retorna
        // ROW_COUNT() (un escalar), no una entidad Customer.
        // ROW_COUNT() retorna cuántas filas fueron afectadas:
        // - 1 = actualización exitosa
        // - 0 = el Id no existía, no se actualizó nada
        // =====================================================================
        public async Task<bool> UpdateAsync(Customer customer)
        {
            var connection = _context.Database.GetDbConnection();

            try
            {
                await _context.Database.OpenConnectionAsync();

                using var command = connection.CreateCommand();
                command.CommandText = "sp_Customer_Update";
                command.CommandType = CommandType.StoredProcedure;

                command.Parameters.Add(new MySqlParameter("@p_Id", customer.Id));
                command.Parameters.Add(new MySqlParameter("@p_Nombre", customer.Nombre));
                command.Parameters.Add(new MySqlParameter("@p_Email", customer.Email));
                command.Parameters.Add(new MySqlParameter("@p_Telefono", (object?)customer.Telefono ?? DBNull.Value));
                command.Parameters.Add(new MySqlParameter("@p_Direccion", (object?)customer.Direccion ?? DBNull.Value));

                var resultado = await command.ExecuteScalarAsync();
                var filasAfectadas = Convert.ToInt32(resultado);

                // TUTOR IA: Retornamos true si al menos una fila fue afectada.
                return filasAfectadas > 0;
            }
            finally
            {
                await _context.Database.CloseConnectionAsync();
            }
        }

        // =====================================================================
        // MÉTODO: DeleteAsync — Eliminar (soft delete) un cliente
        // =====================================================================
        // TUTOR IA: El SP sp_Customer_Delete realiza un "Soft Delete":
        // UPDATE Customers SET Activo = FALSE WHERE Id = @p_Id
        // El registro NO se borra físicamente, solo se marca como inactivo.
        // Esto preserva la integridad referencial con la tabla Payments.
        // =====================================================================
        public async Task<bool> DeleteAsync(int id)
        {
            var connection = _context.Database.GetDbConnection();

            try
            {
                await _context.Database.OpenConnectionAsync();

                using var command = connection.CreateCommand();
                command.CommandText = "sp_Customer_Delete";
                command.CommandType = CommandType.StoredProcedure;

                command.Parameters.Add(new MySqlParameter("@p_Id", id));

                var resultado = await command.ExecuteScalarAsync();
                var filasAfectadas = Convert.ToInt32(resultado);

                return filasAfectadas > 0;
            }
            finally
            {
                await _context.Database.CloseConnectionAsync();
            }
        }
    }
}
