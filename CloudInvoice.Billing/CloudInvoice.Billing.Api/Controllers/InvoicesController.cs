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
                var todasAsClaims = string.Join(" | ", User.Claims.Select(c => c.Type + "=" + c.Value));

                string? userId = User.FindFirstValue(ClaimTypes.NameIdentifier)
                              ?? User.FindFirstValue("sub")
                              ?? User.FindFirstValue("nameid");

                if (string.IsNullOrEmpty(userId))
                {
                    return Unauthorized(new
                    {
                        message = "Utilizador não autenticado ou token sem identificador.",
                        claimsRecebidas = todasAsClaims 
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

                return NoContent();
            }
            catch (InvalidOperationException ex)
            {
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



        [HttpGet]
        public async Task<ActionResult<PagedResultDto<InvoiceResponseDto>>> GetAllInvoices(
            [FromQuery] int pageNumber = 1,
            [FromQuery] int pageSize = 10)
        {
            var pagedResult = await _invoiceService.GetAllInvoicesAsync(pageNumber, pageSize);

            return Ok(pagedResult);
        }

        [HttpPut("{id}/cancel")]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> CancelInvoice(Guid id)
        {
            try
            {
                var success = await _invoiceService.CancelInvoiceAsync(id);

                if (!success)
                {
                    return BadRequest(new { message = "Não foi possível cancelar a fatura. A fatura não existe ou o seu estado atual não permite cancelamento." });
                }

                return NoContent();
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = $"Erro interno: {ex.Message}" });
            }
        }

        [HttpPut("{id}/pay")]
        [Authorize]
        public async Task<IActionResult> MarkAsPaid(Guid id)
        {
            try
            {
                var success = await _invoiceService.MarkAsPaidAsync(id);

                if (!success)
                {
                    return BadRequest(new { message = "Não foi possível registar o pagamento. A fatura não existe ou o seu estado atual não permite pagamento." });
                }

                return NoContent();
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = $"Erro interno: {ex.Message}" });
            }
        }

    }
}
