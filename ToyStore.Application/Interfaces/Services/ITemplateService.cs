using ToyStore.Application.Common.Models;
using ToyStore.Application.DTOs;
using ToyStore.Application.DTOs.Templates;

namespace ToyStore.Application.Interfaces.Services;

/// <summary>
/// Service quan ly Template.
/// </summary>
public interface ITemplateService
{
    /// <summary>
    /// Lay danh sach Template co phan trang.
    /// </summary>
    Task<Result<PaginatedResponse<TemplateListDto>>> GetTemplatesAsync(
        int pageNumber = 1,
        int pageSize = 10,
        string? sortBy = null,
        bool sortDesc = false,
        string? searchTerm = null,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Tao moi Template.
    /// </summary>
    Task<Result<TemplateListDto>> CreateTemplateAsync(
        CreateTemplateDto dto,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Cap nhat Template.
    /// </summary>
    Task<Result<TemplateListDto>> UpdateTemplateAsync(
        short templateId,
        UpdateTemplateDto dto,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Tim kiem Template.
    /// </summary>
    Task<Result<PaginatedResponse<TemplateListDto>>> SearchTemplatesAsync(
        string searchTerm,
        int pageNumber = 1,
        int pageSize = 10,
        string? sortBy = null,
        bool sortDesc = false,
        CancellationToken cancellationToken = default);
}