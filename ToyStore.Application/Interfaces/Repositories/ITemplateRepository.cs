using ToyStore.Application.Common.Models;

namespace ToyStore.Application.Interfaces.Repositories;

/// <summary>
/// Repository thao tac du lieu Template.
/// </summary>
public interface ITemplateRepository
{
    /// <summary>
    /// Lay danh sach Template co phan trang.
    /// </summary>
    Task<List<TemplateModel>> GetPagedAsync(
        int pageNumber,
        int pageSize,
        string? sortBy = null,
        bool sortDesc = false,
        string? searchTerm = null,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Dem tong so Template theo dieu kien tim kiem.
    /// </summary>
    Task<int> CountAsync(string? searchTerm = null, CancellationToken cancellationToken = default);

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
    Task<TemplateModel?> GetByIdAsync(short templateId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Tao moi Template.
    /// </summary>
    Task<TemplateModel> CreateAsync(
        string templateCode,
        string titleTemplate,
        string messageTemplate,
        bool isActive,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Cap nhat Template.
    /// </summary>
    Task<TemplateModel> UpdateAsync(
        short templateId,
        string templateCode,
        string titleTemplate,
        string messageTemplate,
        bool isActive,
        CancellationToken cancellationToken = default);
}