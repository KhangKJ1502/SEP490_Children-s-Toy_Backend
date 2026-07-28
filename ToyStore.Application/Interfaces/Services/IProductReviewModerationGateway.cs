namespace ToyStore.Application.Interfaces.Services;

public interface IProductReviewModerationGateway
{
    Task<bool> ModerateReviewAsync(int reviewId, CancellationToken cancellationToken = default);
}
