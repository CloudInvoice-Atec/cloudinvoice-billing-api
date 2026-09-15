using CloudInvoice.Billing.Application.DTOs;
using CloudInvoice.Billing.Application.Interfaces;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http.Json;
using System.Text;
using System.Threading.Tasks;

namespace CloudInvoice.Billing.Infrastructure.Services
{
    /// <summary>
    /// Implementação do serviço de integração com o Catálogo utilizando o IHttpClientFactory.
    /// </summary>
    public class CatalogIntegrationService : ICatalogIntegrationService
    {
        private readonly HttpClient _httpClient;

        /// <summary>
        /// Inicializa uma nova instância do serviço de integração.
        /// </summary>
        /// <param name="httpClient">O cliente HTTP injetado e pré-configurado pelo IHttpClientFactory no Program.cs.</param>
        public CatalogIntegrationService(HttpClient httpClient)
        {
            _httpClient = httpClient;
        }
        
        /// <inheritdoc />
        public async Task<AvailabilityResponseDto> CheckAvailabilityAsync(Guid productId)
        {
            try
            {

                var response = await _httpClient.GetAsync($"/api/Products/{productId}/check-availability");

                if (response.IsSuccessStatusCode)
                {

                    var result = await response.Content.ReadFromJsonAsync<AvailabilityResponseDto>();
                    return result ?? new AvailabilityResponseDto { IsAvailable = false };
                }


                return new AvailabilityResponseDto { IsAvailable = false };
            }
            catch (Exception ex)
            {

                return new AvailabilityResponseDto { IsAvailable = false };
            }
        }
    }
}
