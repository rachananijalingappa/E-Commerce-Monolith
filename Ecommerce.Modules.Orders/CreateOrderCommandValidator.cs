using FluentValidation;

namespace Ecommerce.Modules.Orders;

public class CreateOrderCommandValidator : AbstractValidator<CreateOrderCommand>
{
    public CreateOrderCommandValidator()
    {
        RuleFor(x => x.UserId)
            .NotEmpty()
            .WithMessage("UserId is required.");

        RuleFor(x => x.ProductIds)
            .NotEmpty()
            .WithMessage("At least one product is required.");
    }
}
