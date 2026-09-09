namespace CloudInvoice.Billing.Application.DTOs.Dashboard
{
    public class DashboardMetricsDto
    {
        public decimal TotalRevenue { get; set; }
        public decimal OverdueAmount { get; set; }
        public int InvoicesCount { get; set; }
        public int NewCustomersCount { get; set; }
    }
}
