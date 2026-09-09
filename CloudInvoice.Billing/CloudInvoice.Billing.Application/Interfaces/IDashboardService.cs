using CloudInvoice.Billing.Application.DTOs.Dashboard;

namespace CloudInvoice.Billing.Application.Interfaces
{
    public interface IDashboardService
    {
        Task<DashboardOverviewDto> GetDashboardOverviewAsync();
    }
}
