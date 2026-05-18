using ToyStore.Application.DTOs.Campaigns;
using ToyStore.Application.Interfaces.Services;

namespace ToyStore.Infrastructure.Services.Resolvers;

/// <summary>
/// Factory tra ve IBusinessObjectResolver phu hop theo ReferenceType.
/// Duoc inject vao CampaignService.
/// </summary>
public class BusinessObjectResolverFactory
{
    private readonly IReadOnlyDictionary<string, IBusinessObjectResolver> _resolvers;

    public BusinessObjectResolverFactory(IEnumerable<IBusinessObjectResolver> resolvers)
    {
        _resolvers = resolvers.ToDictionary(r => r.ReferenceType, StringComparer.OrdinalIgnoreCase);
    }

    public IBusinessObjectResolver? GetResolver(string referenceType)
    {
        _resolvers.TryGetValue(referenceType, out var resolver);
        return resolver;
    }

    public IReadOnlyCollection<IBusinessObjectResolver> GetAll() => _resolvers.Values.ToList();

    /// <summary>
    /// Resolve doi tuong nghiep vu. Tra ve null neu referenceType khong duoc ho tro hoac khong tim thay doi tuong.
    /// </summary>
    public async Task<ResolvedReferenceDto?> ResolveAsync(
        string referenceType,
        int referenceId,
        CancellationToken cancellationToken = default)
    {
        var resolver = GetResolver(referenceType);
        if (resolver is null) return null;
        return await resolver.ResolveAsync(referenceId, cancellationToken);
    }
}
