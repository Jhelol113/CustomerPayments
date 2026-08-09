using Microsoft.EntityFrameworkCore;
using MySqlConnector;
using System.Data;
using CustomerPaymentAPI.Data;
using CustomerPaymentAPI.Entities;
using CustomerPaymentAPI.Repositories.Interfaces;

namespace CustomerPaymentAPI.Repositories.Implementations
{
    // =====================================================================
    // TUTOR IA: IMPLEMENTACIÓN DEL REPOSITORIO DE PAYMENT
    // =====================================================================
    // A diferencia de CustomerRepository, aquí usamos ADO.NET (MySqlCommand)
    // para TODAS las operaciones, incluyendo las de lectura (GetAll, GetById).
    //
    // ¿Por qué NO usamos FromSqlRaw aquí?
    // Porque los SPs sp_Payment_GetAll y sp_Payment_GetById hacen un JOIN
    // con la tabla Customers y retornan una columna extra "CustomerNombre"
    // que NO existe como columna mapeada en la entidad Payment.
    //
    // En la entidad Payment, CustomerNombre tiene el atributo [NotMapped],
    // lo cual le dice a EF Core que lo ignore completamente. Si usáramos
    // FromSqlRaw, EF Core NO mapearía esa columna y perderíamos el nombre
    // del cliente en el resultado.
    //
    // Con ADO.NET leemos MANUALMENTE cada columna del DataReader, incluyendo
    // CustomerNombre, y construimos los objetos Payment a mano. Esto nos da
    // control total sobre el mapeo de datos.
    //
    // LECCIÓN CLAVE: Cuando un SP retorna datos que no coinciden exactamente
    // con una entidad de EF Core, ADO.NET directo es la mejor opción.
    // =====================================================================
    public class PaymentRepository : IPaymentRepository
    {
        private readonly AppDbContext _context;

        public PaymentRepository(AppDbContext context)
        {
            _context = context;
        }

        // =====================================================================
        // MÉTODO: GetAllAsync — Obtener pagos (todos o filtrados por cliente)
        // =====================================================================
        // TUTOR IA: Este método ejecuta sp_Payment_GetAll que recibe p_CustomerId.
        // Si es NULL → trae todos los pagos. Si tiene valor → filtra por cliente.
        //
        // El SP hace JOIN con Customers para traer CustomerNombre, por eso
        // usamos ADO.NET con mapeo manual en lugar de FromSqlRaw.
        //
        // Flujo de ejecución:
        // 1. Abrimos la conexión subyacente de EF Core
        // 2. Configuramos el MySqlCommand como StoredProcedure
        // 3. Ejecutamos ExecuteReaderAsync para obtener un DataReader
        // 4. Iteramos fila por fila, creando objetos Payment manualmente
        // 5. Para cada columna, usamos GetOrdinal para obtener el índice
        //    y IsDBNull para manejar valores nulos de forma segura
        // =====================================================================
        public async Task<IEnumerable<Payment>> GetAllAsync(int? customerId = null)
        {
            var payments = new List<Payment>();
            var connection = _context.Database.GetDbConnection();

            try
            {
                await _context.Database.OpenConnectionAsync();

                using var command = connection.CreateCommand();
                command.CommandText = "sp_Payment_GetAll";
                command.CommandType = CommandType.StoredProcedure;

                // TUTOR IA: Si customerId es null en C#, enviamos DBNull.Value a MySQL.
                // El SP interpreta NULL como "traer todos los pagos sin filtrar".
                command.Parameters.Add(new MySqlParameter("@p_CustomerId", (object?)customerId ?? DBNull.Value));

                // TUTOR IA: ExecuteReaderAsync retorna un DataReader que nos permite
                // leer los resultados fila por fila, a diferencia de ExecuteScalar
                // que solo retorna un único valor.
                using var reader = await command.ExecuteReaderAsync();

                while (await reader.ReadAsync())
                {
                    payments.Add(MapearPaymentDesdeReader(reader));
                }
            }
            finally
            {
                await _context.Database.CloseConnectionAsync();
            }

            return payments;
        }

        // =====================================================================
        // MÉTODO: GetByIdAsync — Obtener un pago específico por Id
        // =====================================================================
        // TUTOR IA: Mismo patrón que GetAll pero para un solo registro.
        // El SP sp_Payment_GetById también hace JOIN para traer CustomerNombre.
        // =====================================================================
        public async Task<Payment?> GetByIdAsync(int id)
        {
            Payment? payment = null;
            var connection = _context.Database.GetDbConnection();

            try
            {
                await _context.Database.OpenConnectionAsync();

                using var command = connection.CreateCommand();
                command.CommandText = "sp_Payment_GetById";
                command.CommandType = CommandType.StoredProcedure;

                command.Parameters.Add(new MySqlParameter("@p_Id", id));

                using var reader = await command.ExecuteReaderAsync();

                // TUTOR IA: ReadAsync retorna true si hay una fila para leer.
                // Si el SP no encontró el pago, ReadAsync retorna false
                // y payment queda como null, que es exactamente lo que queremos.
                if (await reader.ReadAsync())
                {
                    payment = MapearPaymentDesdeReader(reader);
                }
            }
            finally
            {
                await _context.Database.CloseConnectionAsync();
            }

            return payment;
        }

        // =====================================================================
        // MÉTODO: CreateAsync — Crear un nuevo pago
        // =====================================================================
        // TUTOR IA: El SP sp_Payment_Create inserta el registro y retorna
        // LAST_INSERT_ID() con el nuevo Id generado.
        // El Estado inicial será 'Pendiente' (valor por defecto en la tabla).
        // =====================================================================
        public async Task<int> CreateAsync(Payment payment)
        {
            var connection = _context.Database.GetDbConnection();

            try
            {
                await _context.Database.OpenConnectionAsync();

                using var command = connection.CreateCommand();
                command.CommandText = "sp_Payment_Create";
                command.CommandType = CommandType.StoredProcedure;

                command.Parameters.Add(new MySqlParameter("@p_CustomerId", payment.CustomerId));
                command.Parameters.Add(new MySqlParameter("@p_Monto", payment.Monto));
                command.Parameters.Add(new MySqlParameter("@p_MetodoPago", payment.MetodoPago));


                var resultado = await command.ExecuteScalarAsync();
                return Convert.ToInt32(resultado);
            }
            finally
            {
                await _context.Database.CloseConnectionAsync();
            }
        }

        // =====================================================================
        // MÉTODO: UpdateAsync — Actualizar un pago existente
        // =====================================================================
        // TUTOR IA: Permite actualizar todos los campos del pago, incluyendo
        // el Estado (Pendiente → Completado → Cancelado). En un sistema real,
        // podrías agregar validaciones de transición de estado en la capa Service.
        // =====================================================================
        public async Task<bool> UpdateAsync(Payment payment)
        {
            var connection = _context.Database.GetDbConnection();

            try
            {
                await _context.Database.OpenConnectionAsync();

                using var command = connection.CreateCommand();
                command.CommandText = "sp_Payment_Update";
                command.CommandType = CommandType.StoredProcedure;

                command.Parameters.Add(new MySqlParameter("@p_Id", payment.Id));
                command.Parameters.Add(new MySqlParameter("@p_CustomerId", payment.CustomerId));
                command.Parameters.Add(new MySqlParameter("@p_Monto", payment.Monto));
                command.Parameters.Add(new MySqlParameter("@p_MetodoPago", payment.MetodoPago));

                command.Parameters.Add(new MySqlParameter("@p_Estado", payment.Estado));

                var resultado = await command.ExecuteScalarAsync();
                var filasAfectadas = Convert.ToInt32(resultado);

                return filasAfectadas > 0;
            }
            finally
            {
                await _context.Database.CloseConnectionAsync();
            }
        }

        // =====================================================================
        // MÉTODO: DeleteAsync — Eliminar un pago (hard delete)
        // =====================================================================
        // TUTOR IA: A diferencia del Customer (soft delete), aquí se realiza
        // un borrado FÍSICO (DELETE FROM). El SP sp_Payment_Delete elimina
        // permanentemente el registro de la base de datos.
        // =====================================================================
        public async Task<bool> DeleteAsync(int id)
        {
            var connection = _context.Database.GetDbConnection();

            try
            {
                await _context.Database.OpenConnectionAsync();

                using var command = connection.CreateCommand();
                command.CommandText = "sp_Payment_Delete";
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

        // =====================================================================
        // MÉTODO PRIVADO: MapearPaymentDesdeReader
        // =====================================================================
        // TUTOR IA: Este método auxiliar extrae los datos del DataReader y
        // construye un objeto Payment manualmente. Lo separamos en su propio
        // método para evitar duplicación de código (DRY — Don't Repeat Yourself).
        //
        // GetOrdinal("NombreColumna") obtiene el índice numérico de la columna.
        // IsDBNull(indice) verifica si el valor es NULL antes de intentar leerlo.
        // Esto previene excepciones InvalidCast cuando un campo es NULL en MySQL.
        // =====================================================================
        private Payment MapearPaymentDesdeReader(System.Data.Common.DbDataReader reader)
        {
            return new Payment
            {
                Id = reader.GetInt32(reader.GetOrdinal("Id")),
                CustomerId = reader.GetInt32(reader.GetOrdinal("CustomerId")),
                CustomerNombre = reader.IsDBNull(reader.GetOrdinal("CustomerNombre"))
                    ? null
                    : reader.GetString(reader.GetOrdinal("CustomerNombre")),
                Monto = reader.GetDecimal(reader.GetOrdinal("Monto")),
                MetodoPago = reader.GetString(reader.GetOrdinal("MetodoPago")),

                FechaPago = reader.GetDateTime(reader.GetOrdinal("FechaPago")),
                Estado = reader.GetString(reader.GetOrdinal("Estado")),
                FechaCreacion = reader.GetDateTime(reader.GetOrdinal("FechaCreacion"))
            };
        }

        public async Task<bool> UpdateStatusAsync(int id, string estado)
        {
            var connection = _context.Database.GetDbConnection();

            try
            {
                await _context.Database.OpenConnectionAsync();

                using var command = connection.CreateCommand();
                command.CommandText = "sp_Payment_UpdateStatus";
                command.CommandType = CommandType.StoredProcedure;

                command.Parameters.Add(new MySqlParameter("@p_Id", id));
                command.Parameters.Add(new MySqlParameter("@p_Estado", estado));

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
