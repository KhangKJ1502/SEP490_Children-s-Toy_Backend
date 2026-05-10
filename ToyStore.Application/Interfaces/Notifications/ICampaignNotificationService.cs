namespace ToyStore.Application.Interfaces.Notifications;

public interface ICampaignNotificationService
{
    Task ProcessSystemCampaignAsync(string eventKey, Dictionary<string, string> vars, CancellationToken ct = default);

    Task DispatchAdminCampaignAsync(int campaignId, CancellationToken ct = default);
}
