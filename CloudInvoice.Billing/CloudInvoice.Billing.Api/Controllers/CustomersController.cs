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

        // Injeção de Dependências do serviço de clientes
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

                // Retorna HTTP 201 Created com a rota para aceder ao novo recurso e o respetivo DTO
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



        // PUT: api/customers/{id}
        // Endpoint responsável por receber os dados alterados da UI e atualizar o registo
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

                // HTTP 204 No Content: Indica que a operação foi bem-sucedida e não requer corpo de resposta
                return NoContent();
            }
            catch (Exception ex)
            {
                return BadRequest(new { message = ex.Message });
            }
        }



        // Endpoint GET para obter as últimas N faturas de um cliente
        // Exemplo de chamada: GET /api/customers/{id}/invoices?count=5
        [HttpGet("{id}/invoices")]
        public async Task<IActionResult> GetCustomerInvoices(Guid id, [FromQuery] int count = 5)
        {
            var invoices = await _customerService.GetCustomerInvoicesAsync(id, count);

            // Se o cliente não existir ou não tiver faturas, o serviço devolve vazio ou podemos validar
            return Ok(invoices);
        }



        [HttpGet]
        public async Task<ActionResult<PagedResultDto<CustomerResponseDto>>> GetAll([FromQuery] CustomerQueryParameters parameters)
        {
            var pagedResult = await _customerService.GetPagedCustomersAsync(parameters);
            return Ok(pagedResult);
        }


        // GET: api/customers/active
        [HttpGet("active")]
        public async Task<ActionResult<IEnumerable<CustomerResponseDto>>> GetActive()
        {
            var activeCustomers = await _customerService.GetAllActiveCustomersAsync();
            return Ok(activeCustomers);
        }
    }
}
