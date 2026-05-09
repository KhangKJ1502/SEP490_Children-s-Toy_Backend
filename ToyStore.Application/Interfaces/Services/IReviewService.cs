using ToyStore.Application.Common.Models;
using ToyStore.Application.DTOs;
using ToyStore.Application.DTOs.Reviews;
using ToyStore.Domain.Entities;

namespace ToyStore.Application.Interfaces.Services;

public interface IReviewService
{
    // --- Public / Customer ---
    Task<Result<PaginatedResponse<ReviewProductListDto>>> GetPublicListAsync(
        ReviewQueryDto query, CancellationToken cancellationToken = default);

    Task<Result<ReviewProductDto>> GetPublicDetailAsync(
        int reviewId, CancellationToken cancellationToken = default);

    Task<Result<ReviewProductDto>> CreateReviewAsync(
        CreateReviewProductDto dto, CancellationToken cancellationToken = default);

    Task<Result<ReviewProductDto>> UpdateReviewAsync(
        int reviewId, UpdateReviewProductDto dto, CancellationToken cancellationToken = default);

    Task<Result> DeleteReviewAsync(
        int reviewId, CancellationToken cancellationToken = default);

    // --- Admin / Staff ---
    Task<Result<PaginatedResponse<AdminReviewListDto>>> GetAdminListAsync(
        AdminReviewQueryDto query, CancellationToken cancellationToken = default);

    Task<Result<AdminReviewDetailDto>> GetAdminDetailAsync(
        int reviewId, CancellationToken cancellationToken = default);

    Task<Result<AdminReviewDetailDto>> UpdateModerationStatusAsync(
        int reviewId, UpdateModerationStatusDto dto, CancellationToken cancellationToken = default);

    Task<Result<StaffReplyDto>> CreateReplyAsync(
        int reviewId, CreateStaffReplyDto dto, CancellationToken cancellationToken = default);

    Task<Result<StaffReplyDto>> UpdateReplyAsync(
        int reviewId, int replyId, UpdateStaffReplyDto dto, CancellationToken cancellationToken = default);

    Task<Result> DeleteReplyAsync(
        int reviewId, int replyId, CancellationToken cancellationToken = default);
}
