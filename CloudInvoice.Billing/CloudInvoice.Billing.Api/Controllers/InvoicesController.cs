using CloudInvoice.Billing.Application.DTOs;
using CloudInvoice.Billing.Application.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace CloudInvoice.Billing.Api.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    [Authorize]
    public class InvoicesController : ControllerBase
    {
        // O Controller depende apenas da Interface (baixo acoplamento)
        private readonly IInvoiceService _invoiceService;

        public InvoicesController(IInvoiceService invoiceService)
        {
            _invoiceService = invoiceService;
        }

        [HttpPost]
        public async Task<ActionResult<InvoiceResponseDto>> CreateInvoice([FromBody] CreateInvoiceDto request)
        {
            try
            {
                // 1. CÓDIGO DE DIAGNÓSTICO: Extrai e junta todas as chaves e valores lidos do token
                var todasAsClaims = string.Join(" | ", User.Claims.Select(c => c.Type + "=" + c.Value));

                string? userId = User.FindFirstValue(ClaimTypes.NameIdentifier)
                              ?? User.FindFirstValue("sub")
                              ?? User.FindFirstValue("nameid");

                if (string.IsNullOrEmpty(userId))
                {
                    // 2. Devolvemos a lista real de claims para detetar o nome correto
                    return Unauthorized(new
                    {
                        message = "Utilizador não autenticado ou token sem identificador.",
                        claimsRecebidas = todasAsClaims // Vê o resultado na consola do frontend ou Swagger
                    });
                }

                var result = await _invoiceService.CreateInvoiceAsync(userId, request);

                return CreatedAtAction(nameof(GetById), new { id = result.Id }, result);
            }
            catch (Exception ex)
            {
                return BadRequest(new { message = ex.Message });
            }
        }

        [HttpPut("{id:guid}")]
        public async Task<ActionResult<InvoiceResponseDto>> UpdateInvoice(Guid id, [FromBody] UpdateInvoiceDto request)
        {
            try
            {
                var updatedInvoice = await _invoiceService.UpdateInvoiceAsync(id, request);
                if (updatedInvoice == null)
                {
                    return NotFound(new { message = "Invoice not found." });
                }

                return Ok(updatedInvoice);
            }
            catch (Exception ex)
            {
                return BadRequest(new { message = ex.Message });
            }
        }


        [HttpDelete("{id:guid}")]
        public async Task<IActionResult> DeleteInvoice(Guid id)
        {
            try
            {
                var deleted = await _invoiceService.DeleteInvoiceAsync(id);
                if (!deleted)
                {
                    return NotFound(new { message = "Invoice not found." });
                }

                // 204 No Content indica que o recurso foi eliminado com sucesso e não há conteúdo a devolver
                return NoContent();
            }
            catch (InvalidOperationException ex)
            {
                // Retorna erro 400 se tentar violar a regra de negócio (ex: apagar fatura emitida)
                return BadRequest(new { message = ex.Message });
            }
            catch (Exception ex)
            {
                return StatusCode(StatusCodes.Status500InternalServerError, new { message = ex.Message });
            }
        }



        [HttpGet("{id:guid}")]
        public async Task<ActionResult<InvoiceResponseDto>> GetById(Guid id)
        {
            var invoice = await _invoiceService.GetInvoiceByIdAsync(id);
            if (invoice == null)
            {
                return NotFound();
            }

            return Ok(invoice);
        }



        // Exemplo de chamada HTTP: GET /api/invoices?pageNumber=1&pageSize=10
        [HttpGet]
        public async Task<ActionResult<PagedResultDto<InvoiceResponseDto>>> GetAllInvoices(
            [FromQuery] int pageNumber = 1,
            [FromQuery] int pageSize = 10)
        {
            var pagedResult = await _invoiceService.GetAllInvoicesAsync(pageNumber, pageSize);

            // Devolvemos um HTTP 200 OK contendo os metadados da paginação e a lista de faturas mapeadas em DTO
            return Ok(pagedResult);
        }

    }
}
