using AutoMapper;
using FluentValidation;
using Microsoft.Extensions.Logging;
using ToyStore.Application.Common.Models;
using ToyStore.Application.DTOs;
using ToyStore.Application.DTOs.Promotions;
using ToyStore.Application.Interfaces.Repositories;
using ToyStore.Application.Interfaces.Services;
using ToyStore.Application.Validators.Promotions;
using ToyStore.Domain.Entities;

namespace ToyStore.Infrastructure.Services;

/// <summary>
/// Service xử lý nghiệp vụ promotion.
/// </summary>
public class PromotionService : IPromotionService
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly IMapper _mapper;
    private readonly ILogger<PromotionService> _logger;
    private readonly ICartService _cartService;
    private readonly IValidator<CreatePromotionDto> _createValidator;
    private readonly IValidator<UpdatePromotionDto> _updateValidator;

    public PromotionService(
        IUnitOfWork unitOfWork,
        IMapper mapper,
        ILogger<PromotionService> logger,
        ICartService cartService,
        IValidator<CreatePromotionDto> createValidator,
        IValidator<UpdatePromotionDto> updateValidator)
    {
        _unitOfWork = unitOfWork;
        _mapper = mapper;
        _logger = logger;
        _cartService = cartService;
        _createValidator = createValidator;
        _updateValidator = updateValidator;
    }

    public async Task<Result<PaginatedResponse<PromotionListDto>>> GetPromotionsAsync(
        int pageNumber = 1,
        int pageSize = 10,
        string? sortBy = null,
        bool sortDesc = false,
        string? searchTerm = null,
        string? status = null,
        CancellationToken cancellationToken = default)
    {
        if (pageNumber < 1)
        {
            return Result<PaginatedResponse<PromotionListDto>>.Failure(
                "VALIDATION_ERROR", "Page number must be greater than 0.");
        }

        if (pageSize < 1 || pageSize > 100)
        {
            return Result<PaginatedResponse<PromotionListDto>>.Failure(
                "VALIDATION_ERROR", "Page size must be between 1 and 100.");
        }

        var pagedPromotions = await _unitOfWork.Promotions.GetPagedAsync(
            pageNumber,
            pageSize,
            sortBy,
            sortDesc,
            searchTerm,
            status,
            cancellationToken);

        var items = _mapper.Map<List<PromotionListDto>>(pagedPromotions.Items);

        _logger.LogInformation(
            "Retrieved promotions list with PageNumber {PageNumber}, PageSize {PageSize}, SearchTerm {SearchTerm}, Status {Status}",
            pageNumber,
            pageSize,
            searchTerm,
            status);

        var response = new PaginatedResponse<PromotionListDto>(
            items,
            pagedPromotions.TotalCount,
            pageNumber,
            pageSize);

        return Result<PaginatedResponse<PromotionListDto>>.Success(response);
    }

    public async Task<Result<PromotionDto>> GetPromotionByIdAsync(
        int promotionId,
        CancellationToken cancellationToken = default)
    {
        if (promotionId <= 0)
        {
            return Result<PromotionDto>.Failure("VALIDATION_ERROR", "Promotion ID must be greater than 0.");
        }

        var promotion = await _unitOfWork.Promotions.GetByIdAsync(
            promotionId,
            cancellationToken,
            includeProperties: "ProductPromotions,ProductPromotions.Product"
        );
        
        if (promotion is null)
        {
            return Result<PromotionDto>.NotFound("Promotion", promotionId);
        }

        return Result<PromotionDto>.Success(_mapper.Map<PromotionDto>(promotion));
    }

    public async Task<Result<PromotionDto>> CreatePromotionAsync(
        CreatePromotionDto request,
        CancellationToken cancellationToken = default)
    {
        var validationResult = await _createValidator.ValidateAsync(request, cancellationToken);

        if (!validationResult.IsValid)
        {
            return validationResult.ToResult<PromotionDto>();
        }

        var promotionNameExists = await _unitOfWork.Promotions.ExistsPromotionNameAsync(
            request.PromotionName,
            null,
            cancellationToken);

        if (promotionNameExists)
        {
            return Result<PromotionDto>.Conflict("Promotion name already exists.");
        }

        var promotion = _mapper.Map<Promotion>(request);
        // Will be updated by auth mechanism later, set to 1 for now or 0
        promotion.CreatedBy = 0; 
        promotion.CreatedAt = DateTime.UtcNow;
        promotion.UpdatedAt = null;
        promotion.IsDeleted = false;

        if (request.ProductPromotions != null && request.ProductPromotions.Any())
        {
            var productIds = request.ProductPromotions.Select(p => p.ProductId).Distinct().ToList();
            var products = await _unitOfWork.Products.GetByIdsAsync(productIds, cancellationToken);

            if (products.Count != productIds.Count)
            {
                return Result<PromotionDto>.Failure("VALIDATION_ERROR", "One or more products do not exist.");
            }

            foreach (var pp in request.ProductPromotions)
            {
                var product = products.First(p => p.ProductId == pp.ProductId);
                if (pp.SalePrice > product.Price)
                {
                    return Result<PromotionDto>.Failure("VALIDATION_ERROR", $"Sale price for product {product.ProductName} cannot be greater than original price ({product.Price}).");
                }

                if (pp.SaleQuantity.HasValue && pp.SaleQuantity.Value > product.Quantity)
                {
                    return Result<PromotionDto>.Failure("VALIDATION_ERROR", $"Sale quantity for product {product.ProductName} cannot exceed available stock ({product.Quantity}).");
                }

                var productPromotion = _mapper.Map<ProductPromotion>(pp);
                productPromotion.CreatedAt = DateTime.UtcNow;
                promotion.ProductPromotions.Add(productPromotion);
            }
        }

        await _unitOfWork.BeginTransactionAsync(cancellationToken);
        try
        {
            await _unitOfWork.Promotions.AddAsync(promotion, cancellationToken);
            await _unitOfWork.SaveChangesAsync(cancellationToken);
            await _unitOfWork.CommitTransactionAsync(cancellationToken);
        }
        catch (Exception ex)
        {
            await _unitOfWork.RollbackTransactionAsync(cancellationToken);
            _logger.LogError(ex, "Failed to create promotion {PromotionName}", promotion.PromotionName);
            throw;
        }

        _logger.LogInformation(
            "Created promotion with name {PromotionName}",
            promotion.PromotionName);

        // Since ID is generated, we might just return what we have mapped minus ID if we can't fetch it by Name
        // Or we can fetch it again:
        var createdPromotion = await _unitOfWork.Promotions.GetByIdAsync(
            promotion.PromotionId, 
            cancellationToken, 
            includeProperties: "ProductPromotions,ProductPromotions.Product"
        );
        var dto = _mapper.Map<PromotionDto>(createdPromotion ?? promotion);

        var createdProductIds = (createdPromotion ?? promotion).ProductPromotions
            .Select(x => x.ProductId)
            .Distinct()
            .ToList();
        if (createdProductIds.Count > 0)
        {
            await _cartService.NotifyPromotionChangedAsync(createdProductIds, cancellationToken);
        }

        return Result<PromotionDto>.Success(dto);
    }

    public async Task<Result<PromotionDto>> UpdatePromotionAsync(
        int promotionId,
        UpdatePromotionDto request,
        CancellationToken cancellationToken = default)
    {
        if (promotionId <= 0)
        {
            return Result<PromotionDto>.Failure("VALIDATION_ERROR", "Promotion ID must be greater than 0.");
        }

        var updateValidation = await _updateValidator.ValidateAsync(request, cancellationToken);

        if (!updateValidation.IsValid)
        {
            return updateValidation.ToResult<PromotionDto>();
        }

        var existingPromotion = await _unitOfWork.Promotions.GetByIdAsync(
            promotionId, 
            cancellationToken,
            includeProperties: "ProductPromotions"
        );
        if (existingPromotion is null)
        {
            return Result<PromotionDto>.NotFound("Promotion", promotionId);
        }

        // Nếu là yêu cầu xoá (Soft Delete)
        if (request.IsDeleted == true)
        {
            existingPromotion.IsDeleted = true;
            existingPromotion.UpdatedAt = DateTime.UtcNow;

            // Fix legacy invalid data to satisfy SQL Server CHECK constraints during soft delete
            if (existingPromotion.StartDate == default)
            {
                existingPromotion.StartDate = DateTime.UtcNow;
            }
            if (existingPromotion.EndDate <= existingPromotion.StartDate)
            {
                existingPromotion.EndDate = existingPromotion.StartDate.AddDays(1);
            }
        }
        else
        {
            _mapper.Map(request, existingPromotion);
            existingPromotion.UpdatedAt = DateTime.UtcNow;

            var fullValidationRequest = _mapper.Map<CreatePromotionDto>(existingPromotion);
            var fullValidationResult = await _createValidator.ValidateAsync(fullValidationRequest, cancellationToken);

            if (!fullValidationResult.IsValid)
            {
                return fullValidationResult.ToResult<PromotionDto>();
            }

            var promotionNameExists = await _unitOfWork.Promotions.ExistsPromotionNameAsync(
                existingPromotion.PromotionName,
                promotionId,
                cancellationToken);

            if (promotionNameExists)
            {
                return Result<PromotionDto>.Conflict("Promotion name already exists.");
            }

            if (request.ProductPromotions != null)
            {
                var productIds = request.ProductPromotions.Select(p => p.ProductId).Distinct().ToList();
                var products = await _unitOfWork.Products.GetByIdsAsync(productIds, cancellationToken);

                if (products.Count != productIds.Count)
                {
                    return Result<PromotionDto>.Failure("VALIDATION_ERROR", "One or more products do not exist.");
                }

                // Remove deleted items
                var incomingProductIds = request.ProductPromotions.Select(p => p.ProductId).ToList();
                var toRemove = existingPromotion.ProductPromotions
                    .Where(pp => !incomingProductIds.Contains(pp.ProductId))
                    .ToList();
                
                foreach (var item in toRemove)
                {
                    _unitOfWork.Promotions.RemoveProductPromotion(item);
                }

                // Update or add items
                foreach (var incomingPp in request.ProductPromotions)
                {
                    var product = products.First(p => p.ProductId == incomingPp.ProductId);
                    if (incomingPp.SalePrice > product.Price)
                    {
                        return Result<PromotionDto>.Failure("VALIDATION_ERROR", $"Sale price for product {product.ProductName} cannot be greater than original price ({product.Price}).");
                    }

                    if (incomingPp.SaleQuantity.HasValue && incomingPp.SaleQuantity.Value > product.Quantity)
                    {
                        return Result<PromotionDto>.Failure("VALIDATION_ERROR", $"Sale quantity for product {product.ProductName} cannot exceed available stock ({product.Quantity}).");
                    }

                    var existingPp = existingPromotion.ProductPromotions
                        .FirstOrDefault(pp => pp.ProductId == incomingPp.ProductId);

                    if (existingPp != null)
                    {
                        // Update
                        existingPp.SalePrice = incomingPp.SalePrice;
                        existingPp.DiscountPercent = incomingPp.DiscountPercent;
                        existingPp.SaleQuantity = incomingPp.SaleQuantity;
                        existingPp.IsActive = incomingPp.IsActive;
                        existingPp.UpdatedAt = DateTime.UtcNow;
                    }
                    else
                    {
                        // Add
                        var newPp = _mapper.Map<ProductPromotion>(incomingPp);
                        newPp.PromotionId = promotionId;
                        newPp.CreatedAt = DateTime.UtcNow;
                        existingPromotion.ProductPromotions.Add(newPp);
                    }
                }
            }
        }

        await _unitOfWork.BeginTransactionAsync(cancellationToken);
        try
        {
            await _unitOfWork.SaveChangesAsync(cancellationToken);
            await _unitOfWork.CommitTransactionAsync(cancellationToken);
        }
        catch (Exception ex)
        {
            await _unitOfWork.RollbackTransactionAsync(cancellationToken);
            _logger.LogError(ex, "Failed to update promotion {PromotionId}", promotionId);
            throw;
        }

        var updatedPromotion = await _unitOfWork.Promotions.GetByIdAsync(
            promotionId, 
            cancellationToken,
            includeProperties: "ProductPromotions,ProductPromotions.Product"
        ) ?? existingPromotion;

        _logger.LogInformation(
            "Updated promotion {PromotionId} with name {PromotionName}",
            promotionId,
            updatedPromotion.PromotionName);

        var impactedProductIds = updatedPromotion.ProductPromotions
            .Select(x => x.ProductId)
            .Distinct()
            .ToList();
        if (impactedProductIds.Count > 0)
        {
            await _cartService.NotifyPromotionChangedAsync(impactedProductIds, cancellationToken);
        }

        return Result<PromotionDto>.Success(_mapper.Map<PromotionDto>(updatedPromotion));
    }
}
