using AutoMapper;
using FluentValidation;
using Microsoft.Extensions.Logging;
using ToyStore.Application.DTOs.CustomerChildren;
using ToyStore.Application.Interfaces.Repositories;
using ToyStore.Application.Interfaces.Services;
using ToyStore.Application.Validators.CustomerChildren;
using ToyStore.Domain.Entities;

namespace ToyStore.Infrastructure.Services;

public class CustomerChildService : ICustomerChildService
{
    private const byte CustomerRoleId = 1;

    private readonly IUnitOfWork _unitOfWork;
    private readonly ICurrentUserService _currentUserService;
    private readonly IMapper _mapper;
    private readonly ILogger<CustomerChildService> _logger;
    private readonly ITimeProvider _timeProvider;
    private readonly IValidator<CreateChildDto> _createValidator;
    private readonly IValidator<UpdateChildDto> _updateValidator;

    public CustomerChildService(
        IUnitOfWork unitOfWork,
        ICurrentUserService currentUserService,
        IMapper mapper,
        ILogger<CustomerChildService> logger,
        ITimeProvider timeProvider,
        IValidator<CreateChildDto> createValidator,
        IValidator<UpdateChildDto> updateValidator)
    {
        _unitOfWork = unitOfWork;
        _currentUserService = currentUserService;
        _mapper = mapper;
        _logger = logger;
        _timeProvider = timeProvider;
        _createValidator = createValidator;
        _updateValidator = updateValidator;
    }

    public async Task<Result<List<CustomerChildDto>>> GetMyChildrenAsync(CancellationToken cancellationToken = default)
    {
        var accountId = _currentUserService.AccountId;
        if (accountId <= 0)
            return Result<List<CustomerChildDto>>.Unauthorized();

        if (_currentUserService.RoleId != CustomerRoleId)
            return Result<List<CustomerChildDto>>.Failure("FORBIDDEN", "Only customers can access this resource.");

        var children = await _unitOfWork.CustomerChildren.GetActiveByAccountIdAsync(accountId, cancellationToken);
        return Result<List<CustomerChildDto>>.Success(_mapper.Map<List<CustomerChildDto>>(children));
    }

    public async Task<Result<CustomerChildDto>> CreateChildAsync(CreateChildDto dto, CancellationToken cancellationToken = default)
    {
        var accountId = _currentUserService.AccountId;
        if (accountId <= 0)
            return Result<CustomerChildDto>.Unauthorized();

        if (_currentUserService.RoleId != CustomerRoleId)
            return Result<CustomerChildDto>.Failure("FORBIDDEN", "Only customers can access this resource.");

        var validationResult = await _createValidator.ValidateAsync(dto, cancellationToken);
        if (!validationResult.IsValid)
        {
            return Result<CustomerChildDto>.ValidationFailure(validationResult.Errors
                .GroupBy(x => x.PropertyName)
                .ToDictionary(g => g.Key, g => g.Select(x => x.ErrorMessage).ToArray()));
        }

        var count = await _unitOfWork.CustomerChildren.CountAsync(x => x.AccountId == accountId && !x.IsDeleted);

        if (count >= CustomerChildValidationRules.MaxChildrenPerUser)
            return Result<CustomerChildDto>.BusinessError($"You can only have a maximum of {CustomerChildValidationRules.MaxChildrenPerUser} children profiles.");

        var child = new CustomerChild
        {
            AccountId = accountId,
            FullName = dto.FullName.Trim(),
            NickName = string.IsNullOrWhiteSpace(dto.NickName) ? null : dto.NickName.Trim(),
            Dob = dto.Dob,
            SexId = dto.SexId,
            IsDeleted = false,
            EditCount = 0,
            CreatedAt = _timeProvider.UtcNow,
            UpdatedAt = null
        };

        await _unitOfWork.BeginTransactionAsync(cancellationToken);
        try
        {
            await _unitOfWork.CustomerChildren.AddAsync(child, cancellationToken);
            await _unitOfWork.SaveChangesAsync(cancellationToken);
            await _unitOfWork.CommitTransactionAsync(cancellationToken);
        }
        catch (Exception ex)
        {
            await _unitOfWork.RollbackTransactionAsync(cancellationToken);
            _logger.LogError(ex, "Failed to create child for account {AccountId}.", accountId);
            throw;
        }

        var created = await _unitOfWork.CustomerChildren.GetActiveByIdAsync(child.ChildId, cancellationToken);
        return Result<CustomerChildDto>.Success(_mapper.Map<CustomerChildDto>(created!));
    }

    public async Task<Result<CustomerChildDto>> UpdateChildAsync(int childId, UpdateChildDto dto, CancellationToken cancellationToken = default)
    {
        var accountId = _currentUserService.AccountId;
        if (accountId <= 0)
            return Result<CustomerChildDto>.Unauthorized();

        if (_currentUserService.RoleId != CustomerRoleId)
            return Result<CustomerChildDto>.Failure("FORBIDDEN", "Only customers can access this resource.");

        if (childId <= 0)
            return Result<CustomerChildDto>.Failure("VALIDATION_ERROR", "Child ID must be greater than 0.");

        var validationResult = await _updateValidator.ValidateAsync(dto, cancellationToken);
        if (!validationResult.IsValid)
        {
            return Result<CustomerChildDto>.ValidationFailure(validationResult.Errors
                .GroupBy(x => x.PropertyName)
                .ToDictionary(g => g.Key, g => g.Select(x => x.ErrorMessage).ToArray()));
        }

        var child = await _unitOfWork.CustomerChildren.GetActiveByIdAsync(childId, cancellationToken);
        if (child == null || child.AccountId != accountId)
            return Result<CustomerChildDto>.NotFound("CustomerChild", childId);

        if (child.EditCount >= CustomerChildValidationRules.MaxChildProfileEdits)
            return Result<CustomerChildDto>.BusinessError($"You can only edit a child profile up to {CustomerChildValidationRules.MaxChildProfileEdits} times.");

        var hasChanges = false;

        if (dto.FullName != null)
        {
            var trimmed = dto.FullName.Trim();
            if (!string.Equals(trimmed, child.FullName, StringComparison.Ordinal))
            {
                child.FullName = trimmed;
                hasChanges = true;
            }
        }

        if (dto.NickName != null)
        {
            var trimmed = string.IsNullOrWhiteSpace(dto.NickName) ? null : dto.NickName.Trim();
            if (!string.Equals(trimmed, child.NickName, StringComparison.Ordinal))
            {
                child.NickName = trimmed;
                hasChanges = true;
            }
        }

        if (dto.Dob.HasValue && dto.Dob.Value != child.Dob)
        {
            child.Dob = dto.Dob.Value;
            hasChanges = true;
        }

        if (dto.SexId.HasValue && dto.SexId.Value != child.SexId)
        {
            child.SexId = dto.SexId.Value;
            hasChanges = true;
        }

        if (!hasChanges)
            return Result<CustomerChildDto>.Success(_mapper.Map<CustomerChildDto>(child));

        child.EditCount += 1;
        child.UpdatedAt = _timeProvider.UtcNow;

        await _unitOfWork.BeginTransactionAsync(cancellationToken);
        try
        {
            await _unitOfWork.SaveChangesAsync(cancellationToken);
            await _unitOfWork.CommitTransactionAsync(cancellationToken);
        }
        catch (Exception ex)
        {
            await _unitOfWork.RollbackTransactionAsync(cancellationToken);
            _logger.LogError(ex, "Failed to update child {ChildId} for account {AccountId}.", childId, accountId);
            throw;
        }

        var updated = await _unitOfWork.CustomerChildren.GetActiveByIdAsync(childId, cancellationToken);
        return Result<CustomerChildDto>.Success(_mapper.Map<CustomerChildDto>(updated!));
    }

    public async Task<Result> DeleteChildAsync(int childId, CancellationToken cancellationToken = default)
    {
        var accountId = _currentUserService.AccountId;
        if (accountId <= 0)
            return Result.Failure("UNAUTHORIZED", "You are not authorized to perform this action.");

        if (_currentUserService.RoleId != CustomerRoleId)
            return Result.Failure("FORBIDDEN", "Only customers can access this resource.");

        if (childId <= 0)
            return Result.Failure("VALIDATION_ERROR", "Child ID must be greater than 0.");

        var child = await _unitOfWork.CustomerChildren.GetActiveByIdAsync(childId, cancellationToken);
        if (child == null || child.AccountId != accountId)
            return Result.NotFound("CustomerChild", childId);

        await _unitOfWork.BeginTransactionAsync(cancellationToken);
        try
        {
            child.IsDeleted = true;
            child.UpdatedAt = _timeProvider.UtcNow;

            await _unitOfWork.SaveChangesAsync(cancellationToken);
            await _unitOfWork.CommitTransactionAsync(cancellationToken);
        }
        catch (Exception ex)
        {
            await _unitOfWork.RollbackTransactionAsync(cancellationToken);
            _logger.LogError(ex, "Failed to delete child {ChildId} for account {AccountId}.", childId, accountId);
            throw;
        }

        _logger.LogInformation("Child {ChildId} soft-deleted by account {AccountId}.", childId, accountId);
        return Result.Success();
    }
}
