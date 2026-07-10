using CloudInvoice.Billing.Application.DTOs;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CloudInvoice.Billing.Application.Interfaces
{
    /// <summary>
    /// Serviço responsável por gerir a comunicação HTTP com a Catalog.API.
    /// </summary>
    public interface ICatalogIntergrationService
    {
        /// <summary>
        /// Verifica a disponibilidade e o preço de um artigo no Catálogo.
        /// </summary>
        /// <remarks>
        /// Em caso de falha de comunicação com a Catalog.API ou se o artigo não for encontrado (404),
        /// o método é resiliente e devolve o artigo como Indisponível, não quebrando o fluxo da faturação.
        /// </remarks>
        /// <param name="productId">O identificador único (GUID) do artigo a consultar.</param>
        /// <returns>Um DTO contendo a flag de disponibilidade e os dados de preço (Preço Base e Taxa de IVA).</returns>
        Task<AvailabilityResponseDto> CheckAvailabilityAsync(Guid productId);
    }
}
