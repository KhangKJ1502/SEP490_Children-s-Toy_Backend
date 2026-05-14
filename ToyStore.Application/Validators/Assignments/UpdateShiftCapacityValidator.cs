using FluentValidation;
using ToyStore.Application.DTOs.Assignments;

namespace ToyStore.Application.Validators.Assignments;

public class UpdateShiftCapacityValidator : AbstractValidator<UpdateShiftCapacityDto>
{
    public UpdateShiftCapacityValidator()
    {
        RuleFor(x => x.MaxLoad)
            .GreaterThan((short)0).WithMessage("Max load must be greater than 0.")
            .LessThanOrEqualTo((short)200).WithMessage("Max load must not exceed 200.");
    }
}
