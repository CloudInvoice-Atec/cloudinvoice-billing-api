using CloudInvoice.Billing.Application.DTOs.Dashboard;
using CloudInvoice.Billing.Application.Interfaces;
using Microsoft.AspNetCore.Mvc;

namespace CloudInvoice.Billing.Api.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class DashboardController : ControllerBase
    {
        private readonly IDashboardService _dashboardService;

        public DashboardController(IDashboardService dashboardService)
        {
            _dashboardService = dashboardService;
        }

        [HttpGet("overview")]
        public async Task<ActionResult<DashboardOverviewDto>> GetOverview()
        {
            try
            {
                var result = await _dashboardService.GetDashboardOverviewAsync();
                return Ok(result);
            }
            catch (Exception ex)
            {
                // Registar erro nos logs (ex: ILogger)
                return StatusCode(500, new { message = "Ocorreu um erro ao processar os dados do dashboard." });
            }
        }
    }
}
