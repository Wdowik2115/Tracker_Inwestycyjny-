using Investe.Application.DTOs;

namespace Investe.Application.Interfaces.Services
{
    public interface IPriceAlertService
    {
        /// <summary>Creates a new price alert for the user.</summary>
        Task<AlertDto> CreateAlertAsync(Guid userId, CreateAlertDto dto);

        /// <summary>Returns all price alerts for the user.</summary>
        Task<IEnumerable<AlertDto>> GetUserAlertsAsync(Guid userId);

        /// <summary>Gets a specific alert by ID. User must own the alert. Throws KeyNotFoundException or UnauthorizedAccessException.</summary>
        Task<AlertDto> GetAlertByIdAsync(Guid userId, Guid alertId);

        /// <summary>Updates an alert owned by the user. Cannot update triggered alerts. Throws KeyNotFoundException, UnauthorizedAccessException, or InvalidOperationException.</summary>
        Task<AlertDto> UpdateAlertAsync(Guid userId, Guid alertId, UpdateAlertDto dto);

        /// <summary>Deletes an alert owned by the user. Throws KeyNotFoundException or UnauthorizedAccessException.</summary>
        Task DeleteAlertAsync(Guid userId, Guid alertId);

        /// <summary>Checks all active alerts against current prices and marks triggered ones.</summary>
        Task CheckAndTriggerAlertsAsync();
    }
}
