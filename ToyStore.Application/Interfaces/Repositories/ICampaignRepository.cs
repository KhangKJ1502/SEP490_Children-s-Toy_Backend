using ToyStore.Application.DTOs.Campaigns;

namespace ToyStore.Application.Interfaces.Repositories;

/// <summary>
/// Repository thao tac du lieu Campaign.
/// </summary>
public interface ICampaignRepository
{
    /// <summary>
    /// Lay danh sach Campaign co phan trang, multi-field search, filter va sort.
    /// </summary>
    Task<List<CampaignListDto>> GetPagedAsync(
        CampaignQueryDto query,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Dem tong so Campaign theo dieu kien (dung voi query tuong tu GetPagedAsync).
    /// </summary>
    Task<int> CountAsync(
        CampaignQueryDto query,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Tim Campaign theo ID, bao gom Stat va Targets.
    /// </summary>
    Task<CampaignDto?> GetByIdAsync(int campaignId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Kiem tra ten Campaign da ton tai chua (case-insensitive, chi trong ban ghi chua xoa).
    /// </summary>
    Task<bool> ExistsByNameAsync(string campaignName, CancellationToken cancellationToken = default);

    /// <summary>
    /// Tao moi Campaign kem danh sach CampaignTarget.
    /// </summary>
    Task<CampaignDto> CreateAsync(
        CreateCampaignDto dto,
        CancellationToken cancellationToken = default);
}
