using AutoMapper;
using Microsoft.Extensions.Logging;
using ToyStore.Application.DTOs.CustomerChildren;
using ToyStore.Application.Interfaces.Repositories;
using ToyStore.Application.Interfaces.Services;
using ToyStore.Domain.Entities;

namespace ToyStore.Infrastructure.Services;

public class CustomerChildService : ICustomerChildService
{
    private const int MaxChildrenPerUser = 4;
    private const byte CustomerRoleId = 1;

    private readonly IUnitOfWork _unitOfWork;
    private readonly ICurrentUserService _currentUserService;
    private readonly IMapper _mapper;
    private readonly ILogger<CustomerChildService> _logger;

    public CustomerChildService(
        IUnitOfWork unitOfWork,
        ICurrentUserService currentUserService,
        IMapper mapper,
        ILogger<CustomerChildService> logger)
    {
        _unitOfWork = unitOfWork;
        _currentUserService = currentUserService;
        _mapper = mapper;
        _logger = logger;
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

        if (string.IsNullOrWhiteSpace(dto.FullName))
            return Result<CustomerChildDto>.ValidationFailure(new Dictionary<string, string[]>
            {
                ["FullName"] = ["Full name is required."]
            });

        if (dto.Dob == default || dto.Dob > DateTime.UtcNow)
            return Result<CustomerChildDto>.ValidationFailure(new Dictionary<string, string[]>
            {
                ["Dob"] = ["Date of birth must be a valid past date."]
            });

        var count = await _unitOfWork.CustomerChildren.CountAsync(x => x.AccountId == accountId && !x.IsDeleted);

        if (count >= 4)
            return Result<CustomerChildDto>.BusinessError($"You can only have a maximum of {MaxChildrenPerUser} children profiles.");

        var child = new CustomerChild
        {
            AccountId = accountId,
            FullName = dto.FullName.Trim(),
            NickName = string.IsNullOrWhiteSpace(dto.NickName) ? null : dto.NickName.Trim(),
            Dob = dto.Dob,
            SexId = dto.SexId,
            IsDeleted = false,
            CreatedAt = DateTime.UtcNow,
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

        var child = await _unitOfWork.CustomerChildren.GetActiveByIdAsync(childId, cancellationToken);
        if (child == null || child.AccountId != accountId)
            return Result<CustomerChildDto>.NotFound("CustomerChild", childId);

        if (dto.FullName != null && string.IsNullOrWhiteSpace(dto.FullName))
            return Result<CustomerChildDto>.ValidationFailure(new Dictionary<string, string[]>
            {
                ["FullName"] = ["Full name cannot be empty."]
            });

        if (dto.Dob.HasValue && dto.Dob.Value > DateTime.UtcNow)
            return Result<CustomerChildDto>.ValidationFailure(new Dictionary<string, string[]>
            {
                ["Dob"] = ["Date of birth must be a valid past date."]
            });

        await _unitOfWork.BeginTransactionAsync(cancellationToken);
        try
        {
            if (dto.FullName != null)
                child.FullName = dto.FullName.Trim();
            if (dto.NickName != null)
                child.NickName = string.IsNullOrWhiteSpace(dto.NickName) ? null : dto.NickName.Trim();
            if (dto.Dob.HasValue)
                child.Dob = dto.Dob.Value;
            if (dto.SexId.HasValue)
                child.SexId = dto.SexId.Value;

            child.UpdatedAt = DateTime.UtcNow;

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
            child.UpdatedAt = DateTime.UtcNow;

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
