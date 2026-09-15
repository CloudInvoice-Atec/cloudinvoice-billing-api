using CloudInvoice.Billing.Application.Interfaces;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace CloudInvoice.Billing.Api.Controllers
{
    [ApiController]
    [Route("api/system")]
    public class SystemController(IHealthCheckService healthCheckService) : ControllerBase
    {
        private readonly IHealthCheckService _healthCheckService = healthCheckService;

        /// <summary>
        /// Verifies the health of the API and its connection to the database.
        /// </summary>
        [HttpGet("health")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        public async Task<IActionResult> CheckHealth()
        {
            try
            {
                var canConnect = await _healthCheckService.CanConnectAsync();

                if (canConnect)
                {
                    return Ok(new { status = "Healthy", message = "API e Base de Dados a funcionar em pleno!" });
                }

                return StatusCode(StatusCodes.Status500InternalServerError, new { status = "Unhealthy", message = "Base de dados inacessível." });
            }
            catch (Exception ex)
            {
                return StatusCode(StatusCodes.Status500InternalServerError, new { status = "Unhealthy", error = ex.Message });
            }
        }
    }
}
