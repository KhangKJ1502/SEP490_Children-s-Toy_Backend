using AutoMapper;
using FluentValidation;
using ToyStore.Application.DTOs.Addresses;
using ToyStore.Application.Interfaces.Repositories;
using ToyStore.Application.Interfaces.Services;
using ToyStore.Domain.Entities;

namespace ToyStore.Infrastructure.Services;

public class AddressService : IAddressService
{
    private const int MaxAddressPerUser = 5;
    private readonly IUnitOfWork _unitOfWork;
    private readonly ICurrentUserService _currentUserService;
    private readonly IMapper _mapper;
    private readonly IValidator<CreateAddressDto> _createAddressValidator;
    private readonly IValidator<UpdateAddressDto> _updateAddressValidator;
    private readonly ITimeProvider _timeProvider;

    public AddressService(
        IUnitOfWork unitOfWork,
        ICurrentUserService currentUserService,
        IMapper mapper,
        IValidator<CreateAddressDto> createAddressValidator,
        IValidator<UpdateAddressDto> updateAddressValidator,
        ITimeProvider timeProvider)
    {
        _unitOfWork             = unitOfWork;
        _currentUserService     = currentUserService;
        _mapper                 = mapper;
        _createAddressValidator = createAddressValidator;
        _updateAddressValidator = updateAddressValidator;
        _timeProvider           = timeProvider;
    }

    public async Task<Result<List<AddressDto>>> GetMyAddressesAsync(CancellationToken cancellationToken = default)
    {
        var accountId = _currentUserService.AccountId;
        if (accountId <= 0)
        {
            return Result<List<AddressDto>>.Unauthorized();
        }

        var addresses = await _unitOfWork.Addresses.GetActiveByAccountIdAsync(accountId, cancellationToken);
        return Result<List<AddressDto>>.Success(_mapper.Map<List<AddressDto>>(addresses));
    }

    public async Task<Result<AddressDto>> CreateMyAddressAsync(CreateAddressDto dto, CancellationToken cancellationToken = default)
    {
        var accountId = _currentUserService.AccountId;
        if (accountId <= 0)
        {
            return Result<AddressDto>.Unauthorized();
        }

        var validationResult = await _createAddressValidator.ValidateAsync(dto, cancellationToken);
        if (!validationResult.IsValid)
        {
            return Result<AddressDto>.ValidationFailure(validationResult.Errors
                .GroupBy(x => x.PropertyName)
                .ToDictionary(g => g.Key, g => g.Select(x => x.ErrorMessage).ToArray()));
        }

        var currentAddresses = await _unitOfWork.Addresses.GetActiveByAccountIdAsync(accountId, cancellationToken);
        if (currentAddresses.Count >= MaxAddressPerUser)
        {
            return Result<AddressDto>.BusinessError("Each user can have at most 5 addresses.");
        }

        var locationValidation = await ValidateLocationAsync(dto.ProvinceId, dto.DistrictId, dto.WardCode, cancellationToken);
        if (!locationValidation.IsSuccess)
        {
            return Result<AddressDto>.Failure(locationValidation.ErrorCode!, locationValidation.ErrorMessage!);
        }

        var newAddress = new Address
        {
            AccountId = accountId,
            RecipientName = NormalizeNullable(dto.RecipientName),
            PhoneNumber = NormalizeNullable(dto.PhoneNumber),
            AddressLine = dto.AddressLine.Trim(),
            ProvinceId = dto.ProvinceId,
            DistrictId = dto.DistrictId,
            WardCode = dto.WardCode.Trim(),
            IsDeleted = false,
            CreatedAt = _timeProvider.UtcNow,
            UpdatedAt = null,
            IsDefault = currentAddresses.Count == 0 || dto.IsDefault
        };

        await _unitOfWork.BeginTransactionAsync(cancellationToken);
        try
        {
            if (newAddress.IsDefault)
            {
                foreach (var address in currentAddresses)
                {
                    address.IsDefault = false;
                    address.UpdatedAt = _timeProvider.UtcNow;
                }

                // Save the "unset default" phase first to avoid filtered unique-index conflict.
                if (currentAddresses.Count > 0)
                {
                    await _unitOfWork.SaveChangesAsync(cancellationToken);
                }
            }

            await _unitOfWork.Addresses.AddAsync(newAddress, cancellationToken);
            await _unitOfWork.SaveChangesAsync(cancellationToken);
            await _unitOfWork.CommitTransactionAsync(cancellationToken);
        }
        catch
        {
            await _unitOfWork.RollbackTransactionAsync(cancellationToken);
            throw;
        }

        var created = await _unitOfWork.Addresses.GetActiveByIdAsync(newAddress.AddressId, cancellationToken);
        return Result<AddressDto>.Success(_mapper.Map<AddressDto>(created!));
    }

    public async Task<Result<AddressDto>> UpdateMyAddressAsync(int addressId, UpdateAddressDto dto, CancellationToken cancellationToken = default)
    {
        var accountId = _currentUserService.AccountId;
        if (accountId <= 0)
        {
            return Result<AddressDto>.Unauthorized();
        }

        if (addressId <= 0)
        {
            return Result<AddressDto>.Failure("VALIDATION_ERROR", "Address ID must be greater than 0.");
        }

        var validationResult = await _updateAddressValidator.ValidateAsync(dto, cancellationToken);
        if (!validationResult.IsValid)
        {
            return Result<AddressDto>.ValidationFailure(validationResult.Errors
                .GroupBy(x => x.PropertyName)
                .ToDictionary(g => g.Key, g => g.Select(x => x.ErrorMessage).ToArray()));
        }

        var address = await _unitOfWork.Addresses.GetActiveByIdAsync(addressId, cancellationToken);
        if (address == null || address.AccountId != accountId)
        {
            return Result<AddressDto>.NotFound("Address", addressId);
        }

        var provinceId = dto.ProvinceId ?? address.ProvinceId;
        var districtId = dto.DistrictId ?? address.DistrictId;
        var wardCode = dto.WardCode ?? address.WardCode;

        if (!provinceId.HasValue || !districtId.HasValue || string.IsNullOrWhiteSpace(wardCode))
        {
            return Result<AddressDto>.Failure("VALIDATION_ERROR", "Province, district and ward are required.");
        }

        var locationValidation = await ValidateLocationAsync(provinceId.Value, districtId.Value, wardCode, cancellationToken);
        if (!locationValidation.IsSuccess)
        {
            return Result<AddressDto>.Failure(locationValidation.ErrorCode!, locationValidation.ErrorMessage!);
        }

        var allAddresses = await _unitOfWork.Addresses.GetActiveByAccountIdAsync(accountId, cancellationToken);
        var setAsDefault = dto.IsDefault == true;

        await _unitOfWork.BeginTransactionAsync(cancellationToken);
        try
        {
            address.RecipientName = dto.RecipientName != null ? NormalizeNullable(dto.RecipientName) : address.RecipientName;
            address.PhoneNumber = dto.PhoneNumber != null ? NormalizeNullable(dto.PhoneNumber) : address.PhoneNumber;
            address.AddressLine = dto.AddressLine != null ? dto.AddressLine.Trim() : address.AddressLine;
            address.ProvinceId = provinceId;
            address.DistrictId = districtId;
            address.WardCode = wardCode.Trim();
            address.UpdatedAt = _timeProvider.UtcNow;

            if (setAsDefault)
            {
                var otherDefaults = allAddresses
                    .Where(x => x.AddressId != address.AddressId && x.IsDefault)
                    .ToList();

                foreach (var item in otherDefaults)
                {
                    item.IsDefault = false;
                    item.UpdatedAt = _timeProvider.UtcNow;
                }

                // Save first so DB no longer has another default before setting this one.
                if (otherDefaults.Count > 0)
                {
                    await _unitOfWork.SaveChangesAsync(cancellationToken);
                }

                address.IsDefault = true;
            }
            else if (!allAddresses.Any(x => x.IsDefault && x.AddressId != address.AddressId))
            {
                address.IsDefault = true;
            }

            await _unitOfWork.SaveChangesAsync(cancellationToken);
            await _unitOfWork.CommitTransactionAsync(cancellationToken);
        }
        catch
        {
            await _unitOfWork.RollbackTransactionAsync(cancellationToken);
            throw;
        }

        var updated = await _unitOfWork.Addresses.GetActiveByIdAsync(address.AddressId, cancellationToken);
        return Result<AddressDto>.Success(_mapper.Map<AddressDto>(updated!));
    }

    public async Task<Result> DeleteMyAddressAsync(int addressId, CancellationToken cancellationToken = default)
    {
        var accountId = _currentUserService.AccountId;
        if (accountId <= 0)
        {
            return Result.Failure("UNAUTHORIZED", "You are not authorized to perform this action.");
        }

        if (addressId <= 0)
        {
            return Result.Failure("VALIDATION_ERROR", "Address ID must be greater than 0.");
        }

        var address = await _unitOfWork.Addresses.GetActiveByIdAsync(addressId, cancellationToken);
        if (address == null || address.AccountId != accountId)
        {
            return Result.NotFound("Address", addressId);
        }

        var wasDefault = address.IsDefault;
        await _unitOfWork.BeginTransactionAsync(cancellationToken);
        try
        {
            address.IsDeleted = true;
            address.IsDefault = false;
            address.UpdatedAt = _timeProvider.UtcNow;
            await _unitOfWork.SaveChangesAsync(cancellationToken);

            if (wasDefault)
            {
                var remaining = await _unitOfWork.Addresses.GetActiveByAccountIdAsync(accountId, cancellationToken);
                var newDefault = remaining
                    .OrderByDescending(x => x.CreatedAt)
                    .ThenByDescending(x => x.AddressId)
                    .FirstOrDefault();
                if (newDefault != null)
                {
                    newDefault.IsDefault = true;
                    newDefault.UpdatedAt = _timeProvider.UtcNow;
                    await _unitOfWork.SaveChangesAsync(cancellationToken);
                }
            }

            await _unitOfWork.CommitTransactionAsync(cancellationToken);
            return Result.Success();
        }
        catch
        {
            await _unitOfWork.RollbackTransactionAsync(cancellationToken);
            throw;
        }
    }

    public async Task<Result<List<ProvinceOptionDto>>> GetProvincesAsync(CancellationToken cancellationToken = default)
    {
        var provinces = await _unitOfWork.Addresses.GetProvincesAsync(cancellationToken);
        return Result<List<ProvinceOptionDto>>.Success(_mapper.Map<List<ProvinceOptionDto>>(provinces));
    }

    public async Task<Result<List<DistrictOptionDto>>> GetDistrictsAsync(int provinceId, CancellationToken cancellationToken = default)
    {
        if (provinceId <= 0)
        {
            return Result<List<DistrictOptionDto>>.Failure("VALIDATION_ERROR", "Province ID must be greater than 0.");
        }

        var districts = await _unitOfWork.Addresses.GetDistrictsByProvinceIdAsync(provinceId, cancellationToken);
        return Result<List<DistrictOptionDto>>.Success(_mapper.Map<List<DistrictOptionDto>>(districts));
    }

    public async Task<Result<List<WardOptionDto>>> GetWardsAsync(int districtId, CancellationToken cancellationToken = default)
    {
        if (districtId <= 0)
        {
            return Result<List<WardOptionDto>>.Failure("VALIDATION_ERROR", "District ID must be greater than 0.");
        }

        var wards = await _unitOfWork.Addresses.GetWardsByDistrictIdAsync(districtId, cancellationToken);
        return Result<List<WardOptionDto>>.Success(_mapper.Map<List<WardOptionDto>>(wards));
    }

    private async Task<Result> ValidateLocationAsync(
        int provinceId,
        int districtId,
        string wardCode,
        CancellationToken cancellationToken)
    {
        var province = await _unitOfWork.Addresses.GetProvinceByIdAsync(provinceId, cancellationToken);
        if (province == null)
        {
            return Result.NotFound("Province", provinceId);
        }

        var district = await _unitOfWork.Addresses.GetDistrictByIdAsync(districtId, cancellationToken);
        if (district == null)
        {
            return Result.NotFound("District", districtId);
        }

        if (district.ProvinceId != provinceId)
        {
            return Result.BusinessError("District does not belong to selected province.");
        }

        var ward = await _unitOfWork.Addresses.GetWardByCodeAsync(wardCode.Trim(), cancellationToken);
        if (ward == null)
        {
            return Result.NotFound("Ward", wardCode);
        }

        if (ward.DistrictId != districtId)
        {
            return Result.BusinessError("Ward does not belong to selected district.");
        }

        return Result.Success();
    }

    private static string? NormalizeNullable(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return null;
        }

        return value.Trim();
    }
}
