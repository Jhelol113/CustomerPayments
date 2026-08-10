using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using CustomerPaymentAPI.DTOs.Payment;
using CustomerPaymentAPI.Services.Interfaces;

namespace CustomerPaymentAPI.Controllers
{
    // =====================================================================
    // CONTROLLER DE PAYMENT — CRUD CON FILTRO Y VALIDACIÓN CRUZADA
    // =====================================================================
    // Este controller tiene dos particularidades respecto a CustomerController:
    //
    // 1. FILTRO POR QUERY PARAMETER: GetAll acepta ?customerId=5 opcional
    //    para filtrar pagos de un cliente específico.
    //
    // 2. MANEJO DE ArgumentException: Los métodos Create y Update del
    //    PaymentService lanzan ArgumentException cuando el CustomerId
    //    referenciado no existe. El controller captura esa excepción
    //    y la convierte en HTTP 400 (Bad Request) con un mensaje amigable.
    //
    // Convenciones REST aplicadas:
    // ┌──────────┬───────────────────────────────────┬────────┬──────────┐
    // │ Verbo    │ Ruta                              │ Acción │ Status   │
    // ├──────────┼───────────────────────────────────┼────────┼──────────┤
    // │ GET      │ /api/payments                     │ Listar │ 200      │
    // │ GET      │ /api/payments?customerId=5        │ Filtrar│ 200      │
    // │ GET      │ /api/payments/{id}                │ Detalle│ 200/404  │
    // │ POST     │ /api/payments                     │ Crear  │ 201/400  │
    // │ PUT      │ /api/payments/{id}                │ Editar │ 204/400  │
    // │ DELETE   │ /api/payments/{id}                │ Borrar │ 204/404  │
    // └──────────┴───────────────────────────────────┴────────┴──────────┘
    // =====================================================================
    [Route("api/payments")]
    [ApiController]
    [Authorize]
    public class PaymentController : ControllerBase
    {
        private readonly IPaymentService _paymentService;

        public PaymentController(IPaymentService paymentService)
        {
            _paymentService = paymentService;
        }

        // =====================================================================
        // GET: api/payments?customerId=5 — Listar pagos (con filtro opcional)
        // =====================================================================
        // [FromQuery] bindea el parámetro desde el query string.
        // Ejemplos de uso:
        //   GET /api/payments          → Todos los pagos
        //   GET /api/payments?customerId=3  → Solo pagos del cliente 3
        //
        // El parámetro es nullable (int?) para que sea opcional.
        // Si no se envía, el Service pasa null al Repository, y el SP
        // retorna todos los pagos sin filtrar.
        // =====================================================================
        [HttpGet]
        public async Task<IActionResult> GetAll([FromQuery] int? customerId = null)
        {
            var payments = await _paymentService.GetAllAsync(customerId);
            return Ok(payments);
        }

        // =====================================================================
        // GET: api/payments/{id} — Obtener un pago por Id
        // =====================================================================
        // La respuesta incluye CustomerNombre (del JOIN en el SP).
        // Esto permite al frontend mostrar "Pago de Juan Pérez" sin hacer
        // una segunda petición para obtener los datos del cliente.
        // =====================================================================
        [HttpGet("{id}")]
        public async Task<IActionResult> GetById(int id)
        {
            var payment = await _paymentService.GetByIdAsync(id);

            if (payment == null)
            {
                return NotFound(new { mensaje = $"El pago con Id {id} no fue encontrado." });
            }

            return Ok(payment);
        }

        // =====================================================================
        // POST: api/payments — Crear un nuevo pago
        // =====================================================================
        // Este endpoint tiene un try-catch para manejar la
        // ArgumentException que lanza PaymentService cuando el CustomerId
        // no existe.
        //
        // Flujo de error:
        // 1. Frontend envía: { "customerId": 999, "monto": 100 }
        // 2. PaymentService verifica que el cliente 999 exista
        // 3. Si no existe → throw new ArgumentException("No se puede crear...")
        // 4. Este catch captura la excepción
        // 5. Retornamos HTTP 400 con el mensaje de error
        //
        // ¿Por qué 400 y no 404?
        // Porque la petición fue al recurso /api/payments (que sí existe).
        // El problema es que los DATOS enviados son inválidos (customerId
        // inexistente), lo cual es un error del cliente → 400 Bad Request.
        // =====================================================================
        [HttpPost]
        public async Task<IActionResult> Create([FromBody] PaymentRequestDto dto)
        {
            try
            {
                var creado = await _paymentService.CreateAsync(dto);
                return CreatedAtAction(nameof(GetById), new { id = creado.Id }, creado);
            }
            catch (ArgumentException ex)
            {
                // Capturamos ArgumentException específicamente
                // porque es la excepción que usa PaymentService para errores
                // de validación de negocio (cliente no existe).
                return BadRequest(new { mensaje = ex.Message });
            }
            catch (Exception ex)
            {
                // Capturamos Exception genérica para errores
                // inesperados (ej: error de conexión a BD).
                return BadRequest(new { mensaje = "Error al crear el pago.", detalle = ex.Message });
            }
        }

        // =====================================================================
        // PUT: api/payments/{id} — Actualizar un pago existente
        // =====================================================================
        // Mismo patrón de try-catch que Create porque UpdateAsync
        // también puede lanzar ArgumentException si el CustomerId es inválido.
        //
        // El Service retorna false si el pago no existe (→ 404).
        // El Service lanza ArgumentException si el CustomerId es inválido (→ 400).
        // =====================================================================
        [HttpPut("{id}")]
        public async Task<IActionResult> Update(int id, [FromBody] PaymentRequestDto dto)
        {
            try
            {
                var resultado = await _paymentService.UpdateAsync(id, dto);

                if (!resultado)
                {
                    return NotFound(new { mensaje = $"El pago con Id {id} no fue encontrado." });
                }

                return NoContent();
            }
            catch (ArgumentException ex)
            {
                return BadRequest(new { mensaje = ex.Message });
            }
        }

        // =====================================================================
        // DELETE: api/payments/{id} — Eliminar un pago (hard delete)
        // =====================================================================
        // A diferencia de Customer (soft delete), los pagos se
        // eliminan permanentemente de la base de datos.
        // El SP sp_Payment_Delete ejecuta: DELETE FROM Payments WHERE Id = @p_Id
        // =====================================================================
        [HttpDelete("{id}")]
        public async Task<IActionResult> Delete(int id)
        {
            var resultado = await _paymentService.DeleteAsync(id);

            if (!resultado)
            {
                return NotFound(new { mensaje = $"El pago con Id {id} no fue encontrado." });
            }

            return NoContent();
        }

        [HttpPatch("{id}/status")]
        public async Task<IActionResult> UpdateStatus(int id, [FromBody] UpdatePaymentStatusDto dto)
        {
            try
            {
                var resultado = await _paymentService.UpdateStatusAsync(id, dto.Estado);
                if (!resultado) return NotFound(new { mensaje = $"El pago con Id {id} no fue encontrado." });
                return NoContent();
            }
            catch (ArgumentException ex)
            {
                return BadRequest(new { mensaje = ex.Message });
            }
        }
    }
}
