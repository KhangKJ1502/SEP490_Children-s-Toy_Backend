using AutoMapper;
using FluentValidation;
using ToyStore.Application.Common.Models;
using ToyStore.Application.DTOs.Shifts;
using ToyStore.Application.Interfaces.Repositories;
using ToyStore.Application.Interfaces.Services;
using ToyStore.Domain.Entities;

namespace ToyStore.Infrastructure.Services;

public class ShiftTemplateService : IShiftTemplateService
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly IMapper _mapper;
    private readonly IValidator<CreateShiftTemplateDto> _createValidator;
    private readonly IValidator<UpdateShiftTemplateDto> _updateValidator;

    public ShiftTemplateService(
        IUnitOfWork unitOfWork,
        IMapper mapper,
        IValidator<CreateShiftTemplateDto> createValidator,
        IValidator<UpdateShiftTemplateDto> updateValidator)
    {
        _unitOfWork = unitOfWork;
        _mapper = mapper;
        _createValidator = createValidator;
        _updateValidator = updateValidator;
    }

    public async Task<Result<List<ShiftTemplateListDto>>> GetActiveAsync(CancellationToken cancellationToken = default)
    {
        var items = await _unitOfWork.ShiftTemplates.GetActiveAsync(cancellationToken);
        var dtos = _mapper.Map<List<ShiftTemplateListDto>>(items);
        return Result<List<ShiftTemplateListDto>>.Success(dtos);
    }

    public async Task<Result<ShiftTemplateDto>> CreateAsync(CreateShiftTemplateDto dto, CancellationToken cancellationToken = default)
    {
        var validation = await _createValidator.ValidateAsync(dto, cancellationToken);
        if (!validation.IsValid)
        {
            var errors = validation.Errors
                .GroupBy(x => x.PropertyName)
                .ToDictionary(g => g.Key, g => g.Select(x => x.ErrorMessage).ToArray());
            return Result<ShiftTemplateDto>.ValidationFailure(errors);
        }

        var normalizedName = dto.ShiftName.Trim();
        var exists = await _unitOfWork.ShiftTemplates.ExistsByNameAsync(normalizedName, cancellationToken);
        if (exists)
        {
            return Result<ShiftTemplateDto>.Conflict("Shift name already exists.");
        }

        var entity = new ShiftTemplate
        {
            ShiftName = normalizedName,
            StartTime = dto.StartTime,
            EndTime = dto.EndTime,
            MaxOrdersPerShift = dto.MaxOrdersPerShift,
            IsActive = dto.IsActive
        };

        var created = await _unitOfWork.ShiftTemplates.CreateAsync(entity, cancellationToken);
        return Result<ShiftTemplateDto>.Success(_mapper.Map<ShiftTemplateDto>(created));
    }

    public async Task<Result<ShiftTemplateDto>> UpdateAsync(byte shiftTemplateId, UpdateShiftTemplateDto dto, CancellationToken cancellationToken = default)
    {
        if (shiftTemplateId <= 0)
        {
            return Result<ShiftTemplateDto>.Failure("VALIDATION_ERROR", "Shift template ID must be greater than 0.");
        }

        var validation = await _updateValidator.ValidateAsync(dto, cancellationToken);
        if (!validation.IsValid)
        {
            var errors = validation.Errors
                .GroupBy(x => x.PropertyName)
                .ToDictionary(g => g.Key, g => g.Select(x => x.ErrorMessage).ToArray());
            return Result<ShiftTemplateDto>.ValidationFailure(errors);
        }

        if (dto.ShiftName is null && dto.StartTime is null && dto.EndTime is null && dto.MaxOrdersPerShift is null && dto.IsActive is null)
        {
            return Result<ShiftTemplateDto>.Failure("VALIDATION_ERROR", "No fields provided for update.");
        }

        var existing = await _unitOfWork.ShiftTemplates.GetByIdAsync(shiftTemplateId, cancellationToken);
        if (existing is null)
        {
            return Result<ShiftTemplateDto>.NotFound("ShiftTemplate", shiftTemplateId);
        }

        if (dto.ShiftName is not null)
        {
            var normalizedName = dto.ShiftName.Trim();
            var nameExists = await _unitOfWork.ShiftTemplates.ExistsByNameExceptIdAsync(normalizedName, shiftTemplateId, cancellationToken);
            if (nameExists)
            {
                return Result<ShiftTemplateDto>.Conflict("Shift name already exists.");
            }

            existing.ShiftName = normalizedName;
        }

        if (dto.StartTime.HasValue)
        {
            existing.StartTime = dto.StartTime.Value;
        }

        if (dto.EndTime.HasValue)
        {
            existing.EndTime = dto.EndTime.Value;
        }

        if (dto.MaxOrdersPerShift.HasValue)
        {
            existing.MaxOrdersPerShift = dto.MaxOrdersPerShift.Value;
        }

        if (dto.IsActive.HasValue)
        {
            existing.IsActive = dto.IsActive.Value;
        }

        var updated = await _unitOfWork.ShiftTemplates.UpdateAsync(existing, cancellationToken);
        return Result<ShiftTemplateDto>.Success(_mapper.Map<ShiftTemplateDto>(updated));
    }
}
