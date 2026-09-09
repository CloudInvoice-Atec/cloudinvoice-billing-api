namespace CloudInvoice.Billing.Application.DTOs.Dashboard
{
    public class MonthlyRevenueDto
    {
        public string MonthLabel { get; set; } = string.Empty;
        public decimal RevenueAmount { get; set; }
    }
}
