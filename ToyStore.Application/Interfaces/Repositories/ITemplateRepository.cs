using ToyStore.Domain.Entities;

namespace ToyStore.Application.Interfaces.Repositories;

/// <summary>
/// Repository thao tac du lieu Template.
/// </summary>
public interface ITemplateRepository
{
    /// <summary>
    /// Lay danh sach Template co phan trang.
    /// </summary>
    Task<List<Template>> GetPagedAsync(
        int pageNumber,
        int pageSize,
        string? sortBy = null,
        bool sortDesc = false,
        string? searchTerm = null,
        bool? isActive = null,
        string? usageScope = null,
        DateTime? startDate = null,
        DateTime? endDate = null,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Dem tong so Template theo dieu kien tim kiem.
    /// </summary>
    Task<int> CountAsync(
        string? searchTerm = null,
        bool? isActive = null,
        string? usageScope = null,
        DateTime? startDate = null,
        DateTime? endDate = null,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Kiem tra Template code da ton tai chua.
    /// </summary>
    Task<bool> ExistsByCodeAsync(string templateCode, CancellationToken cancellationToken = default);

    /// <summary>
    /// Kiem tra Template code da ton tai chua, ngoai tru mot ID.
    /// </summary>
    Task<bool> ExistsByCodeExceptIdAsync(
        string templateCode,
        short templateId,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Kiem tra xem Template da duoc su dung chua.
    /// </summary>
    Task<bool> IsUsedAsync(string templateCode, CancellationToken cancellationToken = default);

    /// <summary>
    /// Tim Template theo ID.
    /// </summary>
    Task<Template?> GetByIdAsync(short templateId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Tao moi Template.
    /// </summary>
    Task<Template> CreateAsync(
        string templateCode,
        string titleTemplate,
        string messageTemplate,
        bool isActive,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Cap nhat hoac xoa mem Template.
    /// Tra ve null neu khong tim thay.
    /// </summary>
    Task<Template?> SaveAsync(
        short templateId,
        bool isDeleted,
        string? titleTemplate,
        string? messageTemplate,
        bool? isActive,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Lay Template theo TemplateCode — chi lay khi IsActive = 1 va IsDeleted = 0.
    /// Dung boi INotificationTemplateRenderer de render noi dung thong bao.
    /// </summary>
    Task<Template?> GetActiveByCodeAsync(string templateCode, CancellationToken cancellationToken = default);
}