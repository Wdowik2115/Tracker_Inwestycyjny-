using Investe.Application.DTOs;

namespace Investe.Application.Interfaces.Services
{
    public interface IPriceAlertService
    {
        /// <summary>Creates a new price alert for the user.</summary>
        Task<AlertDto> CreateAlertAsync(Guid userId, CreateAlertDto dto);

        /// <summary>Returns all price alerts for the user.</summary>
        Task<IEnumerable<AlertDto>> GetUserAlertsAsync(Guid userId);

        /// <summary>Deletes an alert owned by the user. Throws KeyNotFoundException or UnauthorizedAccessException.</summary>
        Task DeleteAlertAsync(Guid userId, Guid alertId);

        /// <summary>Checks all active alerts against current prices and marks triggered ones.</summary>
        Task CheckAndTriggerAlertsAsync();
    }
}
