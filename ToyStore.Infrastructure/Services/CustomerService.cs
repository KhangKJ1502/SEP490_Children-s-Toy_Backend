using AutoMapper;
using FluentValidation;
using Microsoft.Extensions.Logging;
using ToyStore.Application.DTOs;
using ToyStore.Application.DTOs.Customers;
using ToyStore.Application.Interfaces.Repositories;
using ToyStore.Application.Interfaces.Services;
using ToyStore.Domain.Entities;

namespace ToyStore.Infrastructure.Services;

public class CustomerService : ICustomerService
{
    private const byte CustomerRoleId = 1;
    private readonly IUnitOfWork _unitOfWork;
    private readonly IMapper _mapper;
    private readonly ILogger<CustomerService> _logger;
    private readonly IValidator<UpdateCustomerDto> _updateCustomerValidator;

    public CustomerService(
        IUnitOfWork unitOfWork,
        IMapper mapper,
        ILogger<CustomerService> logger,
        IValidator<UpdateCustomerDto> updateCustomerValidator)
    {
        _unitOfWork = unitOfWork;
        _mapper = mapper;
        _logger = logger;
        _updateCustomerValidator = updateCustomerValidator;
    }

    public async Task<Result<PaginatedResponse<CustomerListDto>>> GetCustomersAsync(
        int pageNumber = 1,
        int pageSize = 10,
        string? sortBy = null,
        bool sortDesc = false,
        string? searchTerm = null,
        CancellationToken cancellationToken = default)
    {
        if (pageNumber < 1)
        {
            return Result<PaginatedResponse<CustomerListDto>>.Failure("VALIDATION_ERROR", "Page number must be greater than 0.");
        }

        if (pageSize < 1 || pageSize > 100)
        {
            return Result<PaginatedResponse<CustomerListDto>>.Failure("VALIDATION_ERROR", "Page size must be between 1 and 100.");
        }

        var normalizedSearchTerm = NormalizeNullable(searchTerm);

        var customers = await _unitOfWork.Accounts.GetPagedAsync(
            pageNumber,
            pageSize,
            sortBy,
            sortDesc,
            normalizedSearchTerm,
            CustomerRoleId,
            cancellationToken);

        var totalCount = await _unitOfWork.Accounts.CountAsync(
            normalizedSearchTerm,
            CustomerRoleId,
            cancellationToken);

        var mappedItems = _mapper.Map<List<CustomerListDto>>(customers);
        await EnrichDeliveryAbuseSummariesAsync(mappedItems, cancellationToken);
        var response = new PaginatedResponse<CustomerListDto>(mappedItems, totalCount, pageNumber, pageSize);
        return Result<PaginatedResponse<CustomerListDto>>.Success(response);
    }

    public async Task<Result<CustomerDetailDto>> GetCustomerByIdAsync(
        int accountId,
        CancellationToken cancellationToken = default)
    {
        if (accountId <= 0)
        {
            return Result<CustomerDetailDto>.Failure("VALIDATION_ERROR", "Account ID must be greater than 0.");
        }

        var customer = await _unitOfWork.Accounts.GetByIdAsync(accountId, cancellationToken);
        if (customer == null || customer.RoleId != CustomerRoleId)
        {
            return Result<CustomerDetailDto>.NotFound("Customer", accountId);
        }

        var dto = _mapper.Map<CustomerDetailDto>(customer);
        await EnrichDeliveryAbuseSummaryAsync(dto, cancellationToken);
        return Result<CustomerDetailDto>.Success(dto);
    }

    public async Task<Result<CustomerDetailDto>> UpdateCustomerAsync(
        int accountId,
        UpdateCustomerDto dto,
        CancellationToken cancellationToken = default)
    {
        if (accountId <= 0)
        {
            return Result<CustomerDetailDto>.Failure("VALIDATION_ERROR", "Account ID must be greater than 0.");
        }

        var validationResult = await _updateCustomerValidator.ValidateAsync(dto, cancellationToken);
        if (!validationResult.IsValid)
        {
            var errors = validationResult.Errors
                .GroupBy(x => x.PropertyName)
                .ToDictionary(g => g.Key, g => g.Select(x => x.ErrorMessage).ToArray());

            return Result<CustomerDetailDto>.ValidationFailure(errors);
        }

        var existingCustomer = await _unitOfWork.Accounts.GetByIdAsync(accountId, cancellationToken);
        if (existingCustomer == null || existingCustomer.RoleId != CustomerRoleId)
        {
            return Result<CustomerDetailDto>.NotFound("Customer", accountId);
        }

        var nextIsActive = dto.IsActive!.Value;
        if (existingCustomer.IsActive == nextIsActive)
        {
            return Result<CustomerDetailDto>.Success(_mapper.Map<CustomerDetailDto>(existingCustomer));
        }

        await _unitOfWork.BeginTransactionAsync(cancellationToken);
        try
        {
            await _unitOfWork.Accounts.UpdateStatusAsync(accountId, nextIsActive, cancellationToken);
            await _unitOfWork.CommitTransactionAsync(cancellationToken);

            var updated = await _unitOfWork.Accounts.GetByIdAsync(accountId, cancellationToken);
            if (updated == null || updated.RoleId != CustomerRoleId)
            {
                return Result<CustomerDetailDto>.NotFound("Customer", accountId);
            }

            var updatedDto = _mapper.Map<CustomerDetailDto>(updated);
            await EnrichDeliveryAbuseSummaryAsync(updatedDto, cancellationToken);

            _logger.LogInformation("Customer {AccountId} status updated to {IsActive}.", accountId, nextIsActive);
            return Result<CustomerDetailDto>.Success(updatedDto);
        }
        catch (Exception ex)
        {
            await _unitOfWork.RollbackTransactionAsync(cancellationToken);
            _logger.LogError(ex, "Failed to update customer status for {AccountId}.", accountId);
            throw;
        }
    }

    private static string? NormalizeNullable(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return null;
        }

        return value.Trim();
    }

    private async Task EnrichDeliveryAbuseSummariesAsync(
        List<CustomerListDto> customers,
        CancellationToken cancellationToken)
    {
        var accountIds = customers.Select(x => x.AccountId).ToArray();
        var summaries = await _unitOfWork.Accounts.GetDeliveryAbuseSummariesAsync(accountIds, cancellationToken);
        var summaryByAccountId = summaries.ToDictionary(x => x.AccountId);

        foreach (var customer in customers)
        {
            if (!summaryByAccountId.TryGetValue(customer.AccountId, out var summary))
            {
                continue;
            }

            customer.SuspiciousDeliveryFailOrderCount = summary.SuspiciousOrderCount;
            customer.IsSuspiciousDeliveryAbuse = summary.SuspiciousOrderCount >= 3;
            customer.LastSuspiciousGHNFailCode = summary.LastFailCode;
            customer.LastSuspiciousOrderDate = summary.LastOrderDate;
        }
    }

    private async Task EnrichDeliveryAbuseSummaryAsync(
        CustomerDetailDto customer,
        CancellationToken cancellationToken)
    {
        var summaries = await _unitOfWork.Accounts.GetDeliveryAbuseSummariesAsync(
            new[] { customer.AccountId },
            cancellationToken);
        var summary = summaries.FirstOrDefault();
        if (summary is null)
        {
            return;
        }

        customer.SuspiciousDeliveryFailOrderCount = summary.SuspiciousOrderCount;
        customer.IsSuspiciousDeliveryAbuse = summary.SuspiciousOrderCount >= 3;
        customer.LastSuspiciousGHNFailCode = summary.LastFailCode;
        customer.LastSuspiciousOrderDate = summary.LastOrderDate;
    }
}
