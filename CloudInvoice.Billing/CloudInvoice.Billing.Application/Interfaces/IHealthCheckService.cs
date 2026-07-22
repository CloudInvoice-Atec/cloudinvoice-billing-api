using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CloudInvoice.Billing.Application.Interfaces
{
    public interface IHealthCheckService
    {
        Task<bool> CanConnectAsync();
    }
}
