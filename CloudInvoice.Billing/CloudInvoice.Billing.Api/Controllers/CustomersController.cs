using CloudInvoice.Billing.Application.DTOs;
using CloudInvoice.Billing.Application.Interfaces;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace CloudInvoice.Billing.Api.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class CustomersController : ControllerBase
    {
        private readonly ICustomerService _customerService;

        public CustomersController(ICustomerService customerService)
        {
            _customerService = customerService;
        }

        [HttpPost]
        public async Task<ActionResult<CustomerResponseDto>> CreateCustomer([FromBody] CreateCustomerDto request)
        {
            try
            {
                var result = await _customerService.CreateCustomerAsync(request);

                return CreatedAtAction(nameof(GetById), new { id = result.Id }, result);
            }
            catch (Exception ex)
            {
                return BadRequest(new { message = ex.Message });
            }
        }

        [HttpGet("{id:guid}")]
        public async Task<ActionResult<CustomerResponseDto>> GetById(Guid id)
        {
            var customer = await _customerService.GetCustomerByIdAsync(id);
            if (customer == null)
            {
                return NotFound();
            }

            return Ok(customer);
        }


        [HttpPut("{id:guid}")]
        public async Task<IActionResult> UpdateCustomer(Guid id, [FromBody] UpdateCustomerDto request)
        {
            try
            {
                var success = await _customerService.UpdateCustomerAsync(id, request);

                if (!success)
                {
                    return NotFound(new { message = "Cliente não encontrado para atualização." });
                }

                return NoContent();
            }
            catch (Exception ex)
            {
                return BadRequest(new { message = ex.Message });
            }
        }


        [HttpDelete("{id:guid}")]
        public async Task<IActionResult> DeleteCustomer(Guid id)
        {
            try
            {
                var success = await _customerService.DeleteCustomerAsync(id);
                if (!success)
                {
                    return NotFound(new { message = "Customer not found." });
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



        [HttpGet("{id}/invoices")]
        public async Task<IActionResult> GetCustomerInvoices(Guid id, [FromQuery] int count = 5)
        {
            var invoices = await _customerService.GetCustomerInvoicesAsync(id, count);

            return Ok(invoices);
        }



        [HttpGet]
        public async Task<ActionResult<PagedResultDto<CustomerResponseDto>>> GetAll([FromQuery] CustomerQueryParameters parameters)
        {
            var pagedResult = await _customerService.GetPagedCustomersAsync(parameters);
            return Ok(pagedResult);
        }


        [HttpGet("active")]
        public async Task<ActionResult<IEnumerable<CustomerResponseDto>>> GetActive()
        {
            var activeCustomers = await _customerService.GetAllActiveCustomersAsync();
            return Ok(activeCustomers);
        }
    }
}
