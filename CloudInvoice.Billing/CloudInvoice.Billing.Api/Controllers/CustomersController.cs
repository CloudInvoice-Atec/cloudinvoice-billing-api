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


        /// <summary>
        /// Creates a new customer.
        /// </summary>
        /// <param name="request">The request body containing the customer details.</param>
        /// <returns></returns>
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



        /// <summary>
        /// Retrieves a customer by their unique identifier.
        /// </summary>
        /// <param name="id">The unique identifier of the customer to retrieve.</param>
        /// <returns></returns>
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




        /// <summary>
        /// Updates an existing customer by their unique identifier.
        /// </summary>
        /// <param name="id"></param>
        /// <param name="request"></param>
        /// <returns></returns>
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




        /// <summary>
        /// Deletes a customer by their unique identifier.
        /// </summary>
        /// <param name="id"></param>
        /// <returns></returns>
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




        /// <summary>
        /// Retrieves a list of invoices associated with a specific customer.
        /// </summary>
        /// <param name="id"></param>
        /// <param name="count"></param>
        /// <returns></returns>
        [HttpGet("{id}/invoices")]
        public async Task<IActionResult> GetCustomerInvoices(Guid id, [FromQuery] int count = 5)
        {
            var invoices = await _customerService.GetCustomerInvoicesAsync(id, count);

            return Ok(invoices);
        }


        /// <summary>
        /// Retrieves a paginated list of customers based on the provided query parameters.
        /// </summary>
        /// <param name="parameters"></param>
        /// <returns></returns>
        [HttpGet]
        public async Task<ActionResult<PagedResultDto<CustomerResponseDto>>> GetAll([FromQuery] CustomerQueryParameters parameters)
        {
            var pagedResult = await _customerService.GetPagedCustomersAsync(parameters);
            return Ok(pagedResult);
        }

        /// <summary>
        /// Retrieves a list of all active customers.
        /// </summary>
        /// <returns></returns>
        [HttpGet("active")]
        public async Task<ActionResult<IEnumerable<CustomerResponseDto>>> GetActive()
        {
            var activeCustomers = await _customerService.GetAllActiveCustomersAsync();
            return Ok(activeCustomers);
        }
    }
}
