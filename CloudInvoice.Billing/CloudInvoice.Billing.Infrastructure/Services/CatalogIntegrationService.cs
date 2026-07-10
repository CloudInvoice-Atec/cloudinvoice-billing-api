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
    public class CatalogIntegrationService : ICatalogIntergrationService
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
                // O BaseAddress já vai estar configurado no Program.cs
                // Só precisamos de chamar o endpoint específico
                var response = await _httpClient.GetAsync($"/api/catalog/{productId}/availability");

                if (response.IsSuccessStatusCode)
                {
                    // Converte o JSON mágico da Catalog API para a nossa classe C#
                    var result = await response.Content.ReadFromJsonAsync<AvailabilityResponseDto>();
                    return result ?? new AvailabilityResponseDto { IsAvailable = false };
                }

                // Se o artigo não existir (404) ou der erro, assumimos que não está disponível
                return new AvailabilityResponseDto { IsAvailable = false };
            }
            catch (Exception ex)
            {
                // O ideal aqui é fazer Log do erro. 
                // Se a Catalog API estiver desligada, entra neste Catch (resiliência básica).
                return new AvailabilityResponseDto { IsAvailable = false };
            }
        }
    }
}
