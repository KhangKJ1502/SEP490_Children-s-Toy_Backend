using FluentValidation;
using ToyStore.Application.DTOs.Customers;

namespace ToyStore.Application.Validators.Customers;

public class UpdateCustomerValidator : AbstractValidator<UpdateCustomerDto>
{
    public UpdateCustomerValidator()
    {
        RuleFor(x => x.IsActive)
            .NotNull().WithMessage("Status is required.");
    }
}
