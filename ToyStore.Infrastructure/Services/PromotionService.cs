using AutoMapper;
using FluentValidation;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using ToyStore.Application.Common.Models;
using ToyStore.Application.DTOs;
using ToyStore.Application.DTOs.Promotions;
using ToyStore.Application.Interfaces.Repositories;
using ToyStore.Application.Interfaces.Services;
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
    private readonly ICurrentUserService _currentUserService;
    private readonly ITimeProvider _timeProvider;
    private readonly IConfiguration _configuration;

    public PromotionService(
        IUnitOfWork unitOfWork,
        IMapper mapper,
        ILogger<PromotionService> logger,
        ICartService cartService,
        IValidator<CreatePromotionDto> createValidator,
        IValidator<UpdatePromotionDto> updateValidator,
        ICurrentUserService currentUserService,
        ITimeProvider timeProvider,
        IConfiguration configuration)
    {
        _unitOfWork = unitOfWork;
        _mapper = mapper;
        _logger = logger;
        _cartService = cartService;
        _createValidator = createValidator;
        _updateValidator = updateValidator;
        _currentUserService = currentUserService;
        _timeProvider = timeProvider;
        _configuration = configuration;
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

    public async Task<Result<List<PromotionDto>>> GetFlashSalePromotionsAsync(
        CancellationToken cancellationToken = default)
    {
        int visibilityDays = _configuration.GetValue<int>("Promotions:FlashSaleVisibilityDays", 2);
        var promotions = await _unitOfWork.Promotions.GetFlashSalePromotionsAsync(visibilityDays, cancellationToken);
        var dtos = _mapper.Map<List<PromotionDto>>(promotions);

        _logger.LogInformation(
            "Retrieved {Count} FLASH_SALE promotions for public display",
            dtos.Count);

        return Result<List<PromotionDto>>.Success(dtos);
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
            includeProperties: "ProductPromotions,ProductPromotions.Product,PromotionTimeSlots,PromotionTimeSlots.PromotionProductSlots,PromotionTimeSlots.PromotionProductSlots.Product"
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
        // Set dynamically from current authenticated user
        promotion.CreatedBy = _currentUserService.AccountId;
        promotion.CreatedAt = _timeProvider.UtcNow;
        promotion.UpdatedAt = null;
        promotion.IsDeleted = false;

        if (request.ProductPromotions != null && request.ProductPromotions.Any())
        {
            if (string.Equals(promotion.PromotionType, "DISCOUNT", StringComparison.OrdinalIgnoreCase))
            {
                foreach (var pp in request.ProductPromotions)
                {
                    pp.SaleQuantity = null;
                }
            }

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
                productPromotion.CreatedAt = _timeProvider.UtcNow;
                promotion.ProductPromotions.Add(productPromotion);
            }
        }

        if (request.PromotionTimeSlots != null && request.PromotionTimeSlots.Any())
        {
            foreach (var ts in request.PromotionTimeSlots)
            {
                var timeSlot = _mapper.Map<PromotionTimeSlot>(ts);
                timeSlot.CreatedAt = _timeProvider.UtcNow;
                promotion.PromotionTimeSlots.Add(timeSlot);
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
            includeProperties: "ProductPromotions,ProductPromotions.Product,PromotionTimeSlots,PromotionTimeSlots.PromotionProductSlots,PromotionTimeSlots.PromotionProductSlots.Product"
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
            includeProperties: "ProductPromotions,PromotionTimeSlots,PromotionTimeSlots.PromotionProductSlots"
        );
        if (existingPromotion is null)
        {
            return Result<PromotionDto>.NotFound("Promotion", promotionId);
        }

        // Nếu là yêu cầu xoá (Soft Delete)
        if (request.IsDeleted == true)
        {
            if (string.Equals(existingPromotion.Status, "Active", StringComparison.OrdinalIgnoreCase))
            {
                return Result<PromotionDto>.Failure("VALIDATION_ERROR", "Cannot delete an Active promotion.");
            }

            existingPromotion.IsDeleted = true;
            existingPromotion.UpdatedAt = _timeProvider.UtcNow;

            // Fix legacy invalid data to satisfy SQL Server CHECK constraints during soft delete
            if (existingPromotion.StartDate == default)
            {
                existingPromotion.StartDate = _timeProvider.UtcNow;
            }
            if (existingPromotion.EndDate <= existingPromotion.StartDate)
            {
                existingPromotion.EndDate = existingPromotion.StartDate.AddDays(1);
            }
        }
        else
        {
            var oldStatus = existingPromotion.Status;
            var now = _timeProvider.UtcNow;
            bool hasTransactions = existingPromotion.ProductPromotions.Any(p => p.SoldQuantity > 0)
                || existingPromotion.PromotionTimeSlots.Any(ts => ts.PromotionProductSlots.Any(pps => pps.SoldQuantity > 0));

            if (string.Equals(oldStatus, "Expired", StringComparison.OrdinalIgnoreCase))
            {
                return Result<PromotionDto>.Failure("VALIDATION_ERROR", "Cannot update an Expired promotion.");
            }

            // Enforce edit safeguards on products and time slots if promotion is active or has transactions
            if (hasTransactions || string.Equals(oldStatus, "Active", StringComparison.OrdinalIgnoreCase))
            {
                if (request.PromotionType is not null && !string.Equals(request.PromotionType, existingPromotion.PromotionType, StringComparison.OrdinalIgnoreCase))
                {
                    return Result<PromotionDto>.Failure("VALIDATION_ERROR", "Cannot modify PromotionType for a promotion that is active or has transactions.");
                }

                if (request.ProductPromotions is not null)
                {
                    var existingActiveProducts = existingPromotion.ProductPromotions.Where(p => !p.IsDeleted).ToList();
                    var incomingProductIds = request.ProductPromotions.Select(p => p.ProductId).ToList();
                    var existingProductIds = existingActiveProducts.Select(p => p.ProductId).ToList();

                    if (incomingProductIds.Count != existingProductIds.Count || !incomingProductIds.All(existingProductIds.Contains))
                    {
                        return Result<PromotionDto>.Failure("VALIDATION_ERROR", "Cannot add or remove products for a promotion that is active or has transactions.");
                    }

                    foreach (var incomingPp in request.ProductPromotions)
                    {
                        var existingPp = existingActiveProducts.First(p => p.ProductId == incomingPp.ProductId);
                        if (incomingPp.SalePrice != existingPp.SalePrice || incomingPp.SaleQuantity != existingPp.SaleQuantity)
                        {
                            return Result<PromotionDto>.Failure("VALIDATION_ERROR", "Cannot modify sale price or sale quantity for products in a promotion that is active or has transactions.");
                        }
                    }
                }

                if (request.PromotionTimeSlots is not null)
                {
                    var existingActiveTimeSlots = existingPromotion.PromotionTimeSlots.Where(ts => !ts.IsDeleted).ToList();

                    if (request.PromotionTimeSlots.Count != existingActiveTimeSlots.Count)
                    {
                        return Result<PromotionDto>.Failure("VALIDATION_ERROR", "Cannot add or remove time slots for a promotion that is active or has transactions.");
                    }

                    foreach (var incomingTs in request.PromotionTimeSlots)
                    {
                        var existingTs = existingActiveTimeSlots.FirstOrDefault(ts => ts.StartAt == incomingTs.StartAt);
                        if (existingTs is null)
                        {
                            int idx = request.PromotionTimeSlots.IndexOf(incomingTs);
                            if (idx >= 0 && idx < existingActiveTimeSlots.Count)
                            {
                                existingTs = existingActiveTimeSlots[idx];
                            }
                        }

                        if (existingTs is not null)
                        {
                            var existingSlotProducts = existingTs.PromotionProductSlots.Where(p => !p.IsDeleted).ToList();
                            var incomingSlotProductIds = incomingTs.PromotionProductSlots.Select(p => p.ProductId).ToList();
                            var existingSlotProductIds = existingSlotProducts.Select(p => p.ProductId).ToList();

                            if (incomingSlotProductIds.Count != existingSlotProductIds.Count || !incomingSlotProductIds.All(existingSlotProductIds.Contains))
                            {
                                return Result<PromotionDto>.Failure("VALIDATION_ERROR", "Cannot add or remove products in time slots for a promotion that is active or has transactions.");
                            }

                            foreach (var incomingPs in incomingTs.PromotionProductSlots)
                            {
                                var existingPs = existingSlotProducts.First(p => p.ProductId == incomingPs.ProductId);
                                if (incomingPs.SalePrice != existingPs.SalePrice || incomingPs.SaleQuantity != existingPs.SaleQuantity)
                                {
                                    return Result<PromotionDto>.Failure("VALIDATION_ERROR", "Cannot modify sale price or sale quantity in time slots for a promotion that is active or has transactions.");
                                }
                            }
                        }
                    }
                }
            }

            var targetStatus = request.Status ?? oldStatus;
            var targetStartDate = request.StartDate ?? existingPromotion.StartDate;
            var targetEndDate = request.EndDate ?? existingPromotion.EndDate;

            if (targetStartDate >= targetEndDate)
            {
                return Result<PromotionDto>.Failure("VALIDATION_ERROR", "Start date must be earlier than end date.");
            }

            if (string.Equals(targetStatus, "Active", StringComparison.OrdinalIgnoreCase) ||
                string.Equals(targetStatus, "Scheduled", StringComparison.OrdinalIgnoreCase))
            {
                if (targetEndDate <= now)
                {
                    return Result<PromotionDto>.Failure("VALIDATION_ERROR", "Cannot activate or schedule a promotion with an end date in the past. Please extend the End Date.");
                }
            }

            // Handle transition checks
            if (string.Equals(oldStatus, "Active", StringComparison.OrdinalIgnoreCase))
            {
                if (!string.Equals(targetStatus, "Active", StringComparison.OrdinalIgnoreCase) &&
                    !string.Equals(targetStatus, "Inactive", StringComparison.OrdinalIgnoreCase))
                {
                    return Result<PromotionDto>.Failure("VALIDATION_ERROR", "Active promotion can only be changed to Inactive or remain Active.");
                }
            }
            else if (string.Equals(oldStatus, "Inactive", StringComparison.OrdinalIgnoreCase))
            {
                if (!string.Equals(targetStatus, "Inactive", StringComparison.OrdinalIgnoreCase) &&
                    !string.Equals(targetStatus, "Scheduled", StringComparison.OrdinalIgnoreCase) &&
                    !string.Equals(targetStatus, "Active", StringComparison.OrdinalIgnoreCase))
                {
                    return Result<PromotionDto>.Failure("VALIDATION_ERROR", "Inactive promotion can only be rescheduled/reactivated or remain Inactive.");
                }
            }
            else if (string.Equals(oldStatus, "Scheduled", StringComparison.OrdinalIgnoreCase))
            {
                if (!string.Equals(targetStatus, "Scheduled", StringComparison.OrdinalIgnoreCase) &&
                    !string.Equals(targetStatus, "Active", StringComparison.OrdinalIgnoreCase) &&
                    !string.Equals(targetStatus, "Inactive", StringComparison.OrdinalIgnoreCase))
                {
                    return Result<PromotionDto>.Failure("VALIDATION_ERROR", "Scheduled promotion can only be changed to Active, Inactive, or remain Scheduled.");
                }
            }

            // Normalization of StartDate / Status on reactivation or rescheduling
            if (string.Equals(targetStatus, "Scheduled", StringComparison.OrdinalIgnoreCase) ||
                string.Equals(targetStatus, "Active", StringComparison.OrdinalIgnoreCase))
            {
                if (targetStartDate <= now)
                {
                    existingPromotion.StartDate = now;
                    existingPromotion.Status = "Active";
                }
                else
                {
                    existingPromotion.StartDate = targetStartDate;
                    existingPromotion.Status = "Scheduled";
                }
            }
            else
            {
                existingPromotion.Status = targetStatus;
                if (request.StartDate.HasValue)
                {
                    existingPromotion.StartDate = request.StartDate.Value;
                }
            }

            existingPromotion.EndDate = targetEndDate;

            if (request.PromotionType is not null && !hasTransactions && !string.Equals(oldStatus, "Active", StringComparison.OrdinalIgnoreCase))
            {
                existingPromotion.PromotionType = request.PromotionType;
            }

            if (request.PromotionName is not null)
            {
                existingPromotion.PromotionName = request.PromotionName;
                var promotionNameExists = await _unitOfWork.Promotions.ExistsPromotionNameAsync(
                    existingPromotion.PromotionName,
                    promotionId,
                    cancellationToken);

                if (promotionNameExists)
                {
                    return Result<PromotionDto>.Conflict("Promotion name already exists.");
                }
            }

            if (request.Description is not null)
            {
                existingPromotion.Description = request.Description;
            }

            if (request.Priority.HasValue)
            {
                existingPromotion.Priority = request.Priority.Value;
            }

            existingPromotion.UpdatedAt = now;

            if (request.ProductPromotions != null)
            {
                if (string.Equals(existingPromotion.PromotionType, "DISCOUNT", StringComparison.OrdinalIgnoreCase)
                    || (request.PromotionType != null && string.Equals(request.PromotionType, "DISCOUNT", StringComparison.OrdinalIgnoreCase)))
                {
                    foreach (var pp in request.ProductPromotions)
                    {
                        pp.SaleQuantity = null;
                    }
                }

                var productIds = request.ProductPromotions.Select(p => p.ProductId).Distinct().ToList();
                var products = await _unitOfWork.Products.GetByIdsAsync(productIds, cancellationToken);

                if (products.Count != productIds.Count)
                {
                    return Result<PromotionDto>.Failure("VALIDATION_ERROR", "One or more products do not exist.");
                }

                var incomingProductIds = request.ProductPromotions.Select(p => p.ProductId).ToList();
                var toRemove = existingPromotion.ProductPromotions
                    .Where(pp => !incomingProductIds.Contains(pp.ProductId))
                    .ToList();

                foreach (var item in toRemove)
                {
                    item.IsDeleted = true;
                    item.UpdatedAt = _timeProvider.UtcNow;
                }

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
                        existingPp.SalePrice = incomingPp.SalePrice;
                        existingPp.DiscountPercent = incomingPp.DiscountPercent;
                        existingPp.SaleQuantity = incomingPp.SaleQuantity;
                        existingPp.UpdatedAt = _timeProvider.UtcNow;
                        existingPp.IsDeleted = false;
                    }
                    else
                    {
                        var newPp = _mapper.Map<ProductPromotion>(incomingPp);
                        newPp.PromotionId = promotionId;
                        newPp.CreatedAt = _timeProvider.UtcNow;
                        existingPromotion.ProductPromotions.Add(newPp);
                    }
                }
            }

            if (request.PromotionTimeSlots != null)
            {
                var existingTimeSlots = existingPromotion.PromotionTimeSlots.ToList();
                var incomingTimeSlots = request.PromotionTimeSlots.ToList();
                var incomingStartTimes = incomingTimeSlots.Select(ts => ts.StartAt).ToList();

                var toRemoveSlots = existingTimeSlots
                    .Where(ts => !incomingStartTimes.Contains(ts.StartAt))
                    .ToList();

                foreach (var item in toRemoveSlots)
                {
                    if (item.Status == "Active" || item.Status == "Inactive" || item.Status == "Expired")
                    {
                        return Result<PromotionDto>.Failure("VALIDATION_ERROR", "Cannot delete an Active, Inactive, or Expired time slot.");
                    }
                    item.IsDeleted = true;
                    item.UpdatedAt = _timeProvider.UtcNow;
                }

                foreach (var incomingTs in incomingTimeSlots)
                {
                    var existingTs = existingPromotion.PromotionTimeSlots.FirstOrDefault(ts => ts.StartAt == incomingTs.StartAt);
                    if (existingTs != null)
                    {
                        if (existingTs.Status == "Active")
                        {
                            if (incomingTs.Status != "Active" && incomingTs.Status != "Inactive")
                            {
                                return Result<PromotionDto>.Failure("VALIDATION_ERROR", "Active time slot can only be changed to Inactive.");
                            }

                            if (existingTs.Status != incomingTs.Status)
                            {
                                existingTs.Status = incomingTs.Status;
                                existingTs.UpdatedAt = _timeProvider.UtcNow;
                            }
                            continue;
                        }
                        else if (existingTs.Status == "Inactive" || existingTs.Status == "Expired")
                        {
                            if (existingTs.Status != incomingTs.Status)
                            {
                                return Result<PromotionDto>.Failure("VALIDATION_ERROR", "Cannot modify the status of an Inactive or Expired time slot.");
                            }
                            continue;
                        }

                        existingTs.EndAt = incomingTs.EndAt;
                        existingTs.Status = incomingTs.Status;
                        existingTs.UpdatedAt = _timeProvider.UtcNow;
                        existingTs.IsDeleted = false;

                        if (incomingTs.PromotionProductSlots != null)
                        {
                            var incomingProductIds = incomingTs.PromotionProductSlots.Select(p => p.ProductId).ToList();
                            var toRemoveProducts = existingTs.PromotionProductSlots
                                .Where(p => !incomingProductIds.Contains(p.ProductId))
                                .ToList();

                            foreach (var rp in toRemoveProducts)
                            {
                                rp.IsDeleted = true;
                                rp.UpdatedAt = _timeProvider.UtcNow;
                            }

                            foreach (var incomingP in incomingTs.PromotionProductSlots)
                            {
                                var existingP = existingTs.PromotionProductSlots.FirstOrDefault(p => p.ProductId == incomingP.ProductId);
                                if (existingP != null)
                                {
                                    existingP.SalePrice = incomingP.SalePrice;
                                    existingP.DiscountPercent = incomingP.DiscountPercent;
                                    existingP.SaleQuantity = incomingP.SaleQuantity;
                                    existingP.UpdatedAt = _timeProvider.UtcNow;
                                    existingP.IsDeleted = false;
                                }
                                else
                                {
                                    var newP = _mapper.Map<PromotionProductSlot>(incomingP);
                                    newP.CreatedAt = _timeProvider.UtcNow;
                                    existingTs.PromotionProductSlots.Add(newP);
                                }
                            }
                        }
                    }
                    else
                    {
                        if (incomingTs.Status == "Scheduled" && incomingTs.StartAt < _timeProvider.UtcNow.AddMinutes(9))
                        {
                            return Result<PromotionDto>.Failure("VALIDATION_ERROR", "New time slot start time must be at least 10 minutes from now.");
                        }

                        var newTs = _mapper.Map<PromotionTimeSlot>(incomingTs);
                        newTs.PromotionId = promotionId;
                        newTs.CreatedAt = _timeProvider.UtcNow;
                        existingPromotion.PromotionTimeSlots.Add(newTs);
                    }
                }
            }

            // Run full validation on the final combined state
            var fullValidationRequest = _mapper.Map<CreatePromotionDto>(existingPromotion);

            var context = new FluentValidation.ValidationContext<CreatePromotionDto>(fullValidationRequest);
            context.RootContextData["IsUpdate"] = true;

            var fullValidationResult = await _createValidator.ValidateAsync(context, cancellationToken);

            if (!fullValidationResult.IsValid)
            {
                return fullValidationResult.ToResult<PromotionDto>();
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
            includeProperties: "ProductPromotions,ProductPromotions.Product,PromotionTimeSlots,PromotionTimeSlots.PromotionProductSlots,PromotionTimeSlots.PromotionProductSlots.Product"
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

    public async Task<Result<List<ProductPromotionInfoDto>>> GetPromotionsByProductIdAsync(
        int productId,
        CancellationToken cancellationToken = default)
    {
        if (productId <= 0)
        {
            return Result<List<ProductPromotionInfoDto>>.Failure("VALIDATION_ERROR", "Product ID must be greater than 0.");
        }

        var product = await _unitOfWork.Products.GetByIdAsync(productId, cancellationToken);
        if (product is null)
        {
            return Result<List<ProductPromotionInfoDto>>.NotFound("Product", productId);
        }

        var list = await _unitOfWork.Promotions.GetPromotionsByProductIdAsync(productId, cancellationToken);
        return Result<List<ProductPromotionInfoDto>>.Success(list);
    }
}
