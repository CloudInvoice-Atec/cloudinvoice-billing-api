namespace CloudInvoice.Billing.Application.DTOs.Dashboard
{
    public class DashboardOverviewDto
    {
        public DashboardMetricsDto Metrics { get; set; } = new();
        public List<RecentInvoiceDto> RecentInvoices { get; set; } = new();
        public List<MonthlyRevenueDto> RevenueChart { get; set; } = new();
    }
}
