using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using CustomerPaymentAPI.DTOs.Customer;
using CustomerPaymentAPI.Services.Interfaces;

namespace CustomerPaymentAPI.Controllers
{
    // =====================================================================
    // CONTROLLER DE CUSTOMER — CRUD COMPLETO PROTEGIDO CON JWT
    // =====================================================================
    // Este controller expone los endpoints REST para gestionar clientes.
    // TODOS los endpoints requieren autenticación ([Authorize] a nivel de clase).
    //
    // Convenciones REST aplicadas:
    // ┌──────────┬──────────────────────┬────────┬─────────────────────┐
    // │ Verbo    │ Ruta                 │ Acción │ Status exitoso      │
    // ├──────────┼──────────────────────┼────────┼─────────────────────┤
    // │ GET      │ /api/customers       │ Listar │ 200 OK              │
    // │ GET      │ /api/customers/{id}  │ Detalle│ 200 OK / 404        │
    // │ POST     │ /api/customers       │ Crear  │ 201 Created         │
    // │ PUT      │ /api/customers/{id}  │ Editar │ 204 No Content /404 │
    // │ DELETE   │ /api/customers/{id}  │ Borrar │ 204 No Content /404 │
    // └──────────┴──────────────────────┴────────┴─────────────────────┘
    //
    // RESPONSABILIDAD DEL CONTROLLER:
    // - Recibir la petición HTTP y extraer datos (route params, body, query)
    // - Delegar al Service (NUNCA accede al Repository directamente)
    // - Convertir el resultado del Service a un HTTP Response adecuado
    // - El Controller NO contiene lógica de negocio
    // =====================================================================
    [Route("api/customers")]
    [ApiController]
    [Authorize]  // Todos los endpoints de este controller requieren JWT válido
    public class CustomerController : ControllerBase
    {
        private readonly ICustomerService _customerService;

        public CustomerController(ICustomerService customerService)
        {
            _customerService = customerService;
        }

        // =====================================================================
        // GET: api/customers — Listar todos los clientes activos
        // =====================================================================
        // Retorna HTTP 200 con un array JSON de clientes.
        // Si no hay clientes, retorna un array vacío [] (NO un 404).
        // Un array vacío es una respuesta válida — significa "hay 0 resultados".
        // 404 se reserva para recursos específicos que no existen.
        // =====================================================================
        [HttpGet]
        public async Task<IActionResult> GetAll()
        {
            var customers = await _customerService.GetAllAsync();
            return Ok(customers);
        }

        // =====================================================================
        // GET: api/customers/{id} — Obtener un cliente por Id
        // =====================================================================
        // El parámetro {id} viene de la URL (route parameter).
        // .NET lo bindea automáticamente al parámetro int id del método.
        //
        // Si el Service retorna null, devolvemos 404 con un mensaje descriptivo.
        // El frontend usa este status para mostrar "Cliente no encontrado".
        // =====================================================================
        [HttpGet("{id}")]
        public async Task<IActionResult> GetById(int id)
        {
            var customer = await _customerService.GetByIdAsync(id);

            if (customer == null)
            {
                return NotFound(new { mensaje = $"El cliente con Id {id} no fue encontrado." });
            }

            return Ok(customer);
        }

        // =====================================================================
        // POST: api/customers — Crear un nuevo cliente
        // =====================================================================
        // [FromBody] indica que el DTO viene del cuerpo JSON.
        // [ApiController] valida automáticamente las Data Annotations del DTO.
        // Si el DTO es inválido (ej: falta Nombre), retorna 400 automáticamente.
        //
        // Retornamos HTTP 201 (Created) con:
        // - Header "Location": /api/customers/{id} (URL del recurso creado)
        // - Body: el DTO del cliente recién creado con su Id y FechaCreacion
        //
        // CreatedAtAction genera automáticamente el header Location
        // apuntando al método GetById con el id del recurso creado.
        // Esto sigue la convención REST para respuestas de creación.
        // =====================================================================
        [HttpPost]
        public async Task<IActionResult> Create([FromBody] CustomerRequestDto dto)
        {
            try
            {
                var creado = await _customerService.CreateAsync(dto);
                return CreatedAtAction(nameof(GetById), new { id = creado.Id }, creado);
            }
            catch (Exception ex)
            {
                // Si el email ya existe (violación de UNIQUE en MySQL),
                // el Repository lanzará una excepción que capturamos aquí.
                return BadRequest(new { mensaje = "Error al crear el cliente.", detalle = ex.Message });
            }
        }

        // =====================================================================
        // PUT: api/customers/{id} — Actualizar un cliente existente
        // =====================================================================
        // El Id viene de la URL y los datos del body.
        // El Service verifica que el cliente exista antes de actualizar.
        //
        // Retornamos 204 (No Content) en éxito porque:
        // - El cliente ya tiene todos los datos que envió (no hay nueva info)
        // - Es la convención REST para PUT exitoso
        //
        // Si el cliente no existe, retornamos 404.
        // =====================================================================
        [HttpPut("{id}")]
        public async Task<IActionResult> Update(int id, [FromBody] CustomerRequestDto dto)
        {
            var resultado = await _customerService.UpdateAsync(id, dto);

            if (!resultado)
            {
                return NotFound(new { mensaje = $"El cliente con Id {id} no fue encontrado." });
            }

            return NoContent();
        }

        // =====================================================================
        // DELETE: api/customers/{id} — Desactivar un cliente (soft delete)
        // =====================================================================
        // El SP sp_Customer_Delete hace soft delete (Activo = FALSE).
        // El cliente no se borra físicamente, solo se marca como inactivo.
        // Esto preserva la relación con sus pagos existentes.
        //
        // Retornamos 204 en éxito, 404 si no existe.
        // =====================================================================
        [HttpDelete("{id}")]
        public async Task<IActionResult> Delete(int id)
        {
            try
            {
                var resultado = await _customerService.DeleteAsync(id);
                if (!resultado)
                {
                    return NotFound(new { mensaje = $"El cliente con Id {id} no fue encontrado." });
                }
                return NoContent();
            }
            catch (InvalidOperationException ex)
            {
                // HTTP 409 Conflict: No se puede eliminar por regla de negocio
                return Conflict(new { mensaje = ex.Message });
            }
        }
    }
}
