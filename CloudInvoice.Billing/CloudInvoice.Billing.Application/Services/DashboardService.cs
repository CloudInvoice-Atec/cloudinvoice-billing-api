using CloudInvoice.Billing.Application.DTOs.Dashboard;
using CloudInvoice.Billing.Application.Interfaces;
using CloudInvoice.Billing.Domain.Entities;
using CloudInvoice.Billing.Domain.Interfaces;
using System.Globalization;

namespace CloudInvoice.Billing.Application.Services
{
    public class DashboardService : IDashboardService
    {
        private readonly IInvoiceRepository _invoiceRepository;
        private readonly ICustomerRepository _customerRepository;

        public DashboardService(IInvoiceRepository invoiceRepository, ICustomerRepository customerRepository)
        {
            _invoiceRepository = invoiceRepository;
            _customerRepository = customerRepository;
        }

        public async Task<DashboardOverviewDto> GetDashboardOverviewAsync()
        {
            var now = DateTime.UtcNow;
            var startOfMonth = new DateTime(now.Year, now.Month, 1);
            var sixMonthsAgo = startOfMonth.AddMonths(-5);

            // 1. Obtém apenas as faturas dos últimos 6 meses para o gráfico e métricas do mês
            var recentInvoicesDb = await _invoiceRepository.GetInvoicesFromDateAsync(sixMonthsAgo);
            var allCustomers = await _customerRepository.GetAllAsync();

            var invoicesList = recentInvoicesDb.ToList();
            var currentMonthInvoices = invoicesList.Where(i => i.IssueDate >= startOfMonth).ToList();

            // 2. Métricas
            var metrics = new DashboardMetricsDto
            {
                TotalRevenue = currentMonthInvoices.Sum(i => i.TotalAmount),
                InvoicesCount = currentMonthInvoices.Count,
                // Faturas vencidas: data de vencimento ultrapassada e por pagar
                OverdueAmount = invoicesList
                    .Where(i => i.DueDate < DateTime.UtcNow && i.PaymentStatus != PaymentStatus.Paid)
                    .Sum(i => i.TotalAmount),
                NewCustomersCount = allCustomers.Count(c => c.CreatedAt >= startOfMonth)
            };

            // 3. Faturas Recentes (Últimas 5)
            var recentInvoices = invoicesList
                .OrderByDescending(i => i.IssueDate)
                .Take(5)
                .Select(i => new RecentInvoiceDto
                {
                    Id = i.Id,
                    InvoiceNumber = i.InvoiceNumber,
                    CustomerName = i.Customer?.Name ?? "Desconhecido",
                    IssueDate = i.IssueDate,
                    TotalAmount = i.TotalAmount,
                    Status = i.Status.ToString() // 👈 Converte o enum InvoiceStatus para string
                }).ToList();

            // 4. Gráfico de Evolução (Últimos 6 meses)
            var chartData = invoicesList
                .GroupBy(i => new { i.IssueDate.Year, i.IssueDate.Month })
                .OrderBy(g => g.Key.Year).ThenBy(g => g.Key.Month)
                .Select(g => new MonthlyRevenueDto
                {
                    MonthLabel = CultureInfo.GetCultureInfo("pt-PT").DateTimeFormat.GetAbbreviatedMonthName(g.Key.Month) + " " + g.Key.Year.ToString().Substring(2),
                    RevenueAmount = g.Sum(i => i.TotalAmount)
                }).ToList();

            var completeChartData = GenerateLastSixMonthsLabels(sixMonthsAgo)
                .Select(label => new MonthlyRevenueDto
                {
                    MonthLabel = label,
                    RevenueAmount = chartData.FirstOrDefault(c => c.MonthLabel == label)?.RevenueAmount ?? 0
                }).ToList();

            return new DashboardOverviewDto
            {
                Metrics = metrics,
                RecentInvoices = recentInvoices,
                RevenueChart = completeChartData
            };
        }

        private List<string> GenerateLastSixMonthsLabels(DateTime startDate)
        {
            var labels = new List<string>();
            var ptCulture = CultureInfo.GetCultureInfo("pt-PT");
            for (int i = 0; i < 6; i++)
            {
                var date = startDate.AddMonths(i);
                labels.Add(ptCulture.DateTimeFormat.GetAbbreviatedMonthName(date.Month) + " " + date.Year.ToString().Substring(2));
            }
            return labels;
        }
    }
}
