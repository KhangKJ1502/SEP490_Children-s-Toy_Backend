using AutoMapper;
using FluentValidation;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using ToyStore.Application.Common.Models;
using ToyStore.Application.DTOs.BankAccounts;
using ToyStore.Application.Interfaces.Repositories;
using ToyStore.Application.Interfaces.Services;
using ToyStore.Domain.Entities;

namespace ToyStore.Infrastructure.Services;

public class SavedBankAccountService : ISavedBankAccountService
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly ICurrentUserService _currentUserService;
    private readonly IMapper _mapper;
    private readonly IValidator<CreateSavedBankAccountDto> _validator;
    private readonly ITimeProvider _timeProvider;

    public SavedBankAccountService(
        IUnitOfWork unitOfWork,
        ICurrentUserService currentUserService,
        IMapper mapper,
        IValidator<CreateSavedBankAccountDto> validator,
        ITimeProvider timeProvider)
    {
        _unitOfWork = unitOfWork;
        _currentUserService = currentUserService;
        _mapper = mapper;
        _validator = validator;
        _timeProvider = timeProvider;
    }

    public async Task<Result<List<SavedBankAccountDto>>> GetMySavedBankAccountsAsync(CancellationToken cancellationToken = default)
    {
        var accountId = _currentUserService.AccountId;
        if (accountId <= 0)
        {
            return Result<List<SavedBankAccountDto>>.Unauthorized();
        }

        var accounts = await _unitOfWork.SavedBankAccounts.GetByAccountIdAsync(accountId, cancellationToken);
        return Result<List<SavedBankAccountDto>>.Success(_mapper.Map<List<SavedBankAccountDto>>(accounts));
    }

    public async Task<Result<SavedBankAccountDto>> CreateSavedBankAccountAsync(CreateSavedBankAccountDto dto, CancellationToken cancellationToken = default)
    {
        var accountId = _currentUserService.AccountId;
        if (accountId <= 0)
        {
            return Result<SavedBankAccountDto>.Unauthorized();
        }

        // Validate DTO
        var validationResult = await _validator.ValidateAsync(dto, cancellationToken);
        if (!validationResult.IsValid)
        {
            var errors = validationResult.Errors
                .GroupBy(e => e.PropertyName)
                .ToDictionary(g => g.Key, g => g.Select(e => e.ErrorMessage).ToArray());
            return Result<SavedBankAccountDto>.ValidationFailure(errors);
        }

        var existing = await _unitOfWork.SavedBankAccounts.GetByUniqueKeyAsync(accountId, dto.BankBin, dto.AccountNumber, cancellationToken);
        if (existing != null && !existing.IsDeleted)
        {
            return Result<SavedBankAccountDto>.Conflict("This bank account is already saved.");
        }

        var activeAccounts = await _unitOfWork.SavedBankAccounts.GetByAccountIdAsync(accountId, cancellationToken);
        var shouldBeDefault = dto.IsDefault || !activeAccounts.Any();

        await _unitOfWork.BeginTransactionAsync(cancellationToken);
        try
        {
            // If setting this new account as default, unset any previous defaults first.
            if (shouldBeDefault)
            {
                var previousDefaults = activeAccounts.Where(x => x.IsDefault).ToList();
                foreach (var account in previousDefaults)
                {
                    account.IsDefault = false;
                    _unitOfWork.SavedBankAccounts.Update(account);
                }

                if (previousDefaults.Any())
                {
                    // Save first to avoid conflict on unique filtered index UQ_SavedBankAccounts_OneDefault
                    await _unitOfWork.SaveChangesAsync(cancellationToken);
                }
            }

            SavedBankAccount resultEntity;

            if (existing != null)
            {
                // Restore soft-deleted account
                existing.IsDeleted = false;
                existing.BankName = dto.BankName.Trim();
                existing.BankShortName = dto.BankShortName.Trim();
                existing.BankCode = dto.BankCode.Trim();
                existing.AccountName = dto.AccountName.Trim().ToUpper();
                existing.IsDefault = shouldBeDefault;
                existing.LastUsedAt = null; // Reset last used

                resultEntity = existing;
                _unitOfWork.SavedBankAccounts.Update(existing);
            }
            else
            {
                // Create new account
                var newAccount = new SavedBankAccount
                {
                    AccountId = accountId,
                    BankBin = dto.BankBin.Trim(),
                    BankName = dto.BankName.Trim(),
                    BankShortName = dto.BankShortName.Trim(),
                    BankCode = dto.BankCode.Trim(),
                    AccountNumber = dto.AccountNumber.Trim(),
                    AccountName = dto.AccountName.Trim().ToUpper(),
                    IsDefault = shouldBeDefault,
                    IsDeleted = false,
                    CreatedAt = _timeProvider.UtcNow
                };

                await _unitOfWork.SavedBankAccounts.AddAsync(newAccount, cancellationToken);
                resultEntity = newAccount;
            }

            await _unitOfWork.SaveChangesAsync(cancellationToken);
            await _unitOfWork.CommitTransactionAsync(cancellationToken);

            return Result<SavedBankAccountDto>.Success(_mapper.Map<SavedBankAccountDto>(resultEntity));
        }
        catch
        {
            await _unitOfWork.RollbackTransactionAsync(cancellationToken);
            throw;
        }
    }

    public async Task<Result> DeleteSavedBankAccountAsync(int id, CancellationToken cancellationToken = default)
    {
        var accountId = _currentUserService.AccountId;
        if (accountId <= 0)
        {
            return Result.Unauthorized();
        }

        var account = await _unitOfWork.SavedBankAccounts.GetByIdAsync(id, cancellationToken);
        if (account == null)
        {
            return Result.NotFound("SavedBankAccount", id);
        }

        if (account.AccountId != accountId)
        {
            return Result.Unauthorized();
        }

        // Check if there is any pending or processing withdrawal request
        var hasPendingWithdrawals = await _unitOfWork.SavedBankAccounts.HasPendingWithdrawalRequestsAsync(account.BankBin, account.AccountNumber, cancellationToken);
        if (hasPendingWithdrawals)
        {
            return Result.BusinessError("Cannot delete bank account as it has pending or processing withdrawal requests.");
        }

        await _unitOfWork.BeginTransactionAsync(cancellationToken);
        try
        {
            var wasDefault = account.IsDefault;
            account.IsDeleted = true;
            account.IsDefault = false;
            _unitOfWork.SavedBankAccounts.Update(account);

            if (wasDefault)
            {
                var activeAccounts = await _unitOfWork.SavedBankAccounts.GetByAccountIdAsync(accountId, cancellationToken);
                var nextDefault = activeAccounts.FirstOrDefault(x => x.SavedBankAccountId != id);
                if (nextDefault != null)
                {
                    nextDefault.IsDefault = true;
                    _unitOfWork.SavedBankAccounts.Update(nextDefault);
                }
            }

            await _unitOfWork.SaveChangesAsync(cancellationToken);
            await _unitOfWork.CommitTransactionAsync(cancellationToken);

            return Result.Success();
        }
        catch
        {
            await _unitOfWork.RollbackTransactionAsync(cancellationToken);
            throw;
        }
    }

    public async Task<Result> SetDefaultBankAccountAsync(int id, CancellationToken cancellationToken = default)
    {
        var accountId = _currentUserService.AccountId;
        if (accountId <= 0)
        {
            return Result.Unauthorized();
        }

        var account = await _unitOfWork.SavedBankAccounts.GetByIdAsync(id, cancellationToken);
        if (account == null)
        {
            return Result.NotFound("SavedBankAccount", id);
        }

        if (account.AccountId != accountId)
        {
            return Result.Unauthorized();
        }

        if (account.IsDefault)
        {
            return Result.Success();
        }

        await _unitOfWork.BeginTransactionAsync(cancellationToken);
        try
        {
            var activeAccounts = await _unitOfWork.SavedBankAccounts.GetByAccountIdAsync(accountId, cancellationToken);
            var previousDefaults = activeAccounts.Where(x => x.IsDefault).ToList();
            foreach (var previousDefault in previousDefaults)
            {
                previousDefault.IsDefault = false;
                _unitOfWork.SavedBankAccounts.Update(previousDefault);
            }

            if (previousDefaults.Any())
            {
                // Save first to avoid conflict on unique filtered index UQ_SavedBankAccounts_OneDefault
                await _unitOfWork.SaveChangesAsync(cancellationToken);
            }

            account.IsDefault = true;
            _unitOfWork.SavedBankAccounts.Update(account);

            await _unitOfWork.SaveChangesAsync(cancellationToken);
            await _unitOfWork.CommitTransactionAsync(cancellationToken);

            return Result.Success();
        }
        catch
        {
            await _unitOfWork.RollbackTransactionAsync(cancellationToken);
            throw;
        }
    }
}
