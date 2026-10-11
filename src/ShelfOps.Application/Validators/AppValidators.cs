using FluentValidation;
using ShelfOps.Application.DTOs;

namespace ShelfOps.Application.Validators
{
    public class CreateProductValidator : AbstractValidator<CreateProductRequest>
    {
        public CreateProductValidator()
        {
            RuleFor(x => x.Sku).NotEmpty().MaximumLength(50);
            RuleFor(x => x.Name).NotEmpty().MaximumLength(200);
            RuleFor(x => x.CategoryId).NotEmpty();
            RuleFor(x => x.PurchasePrice).GreaterThanOrEqualTo(0);
            RuleFor(x => x.SalePrice).GreaterThan(0);
        }
    }

    public class UpdateProductValidator : AbstractValidator<UpdateProductRequest>
    {
        public UpdateProductValidator()
        {
            RuleFor(x => x.Name).NotEmpty().MaximumLength(200);
            RuleFor(x => x.CategoryId).NotEmpty();
            RuleFor(x => x.PurchasePrice).GreaterThanOrEqualTo(0);
            RuleFor(x => x.SalePrice).GreaterThan(0);
        }
    }

    public class CompleteSaleValidator : AbstractValidator<CompleteSaleRequest>
    {
        public CompleteSaleValidator()
        {
            RuleFor(x => x.Items).NotEmpty().WithMessage("Sale must have at least one item.");
            RuleForEach(x => x.Items).ChildRules(item =>
            {
                item.RuleFor(i => i.ProductId).NotEmpty();
                item.RuleFor(i => i.Quantity).GreaterThan(0);
                item.RuleFor(i => i.DiscountAmount).GreaterThanOrEqualTo(0);
            });
        }
    }

    public class LoginValidator : AbstractValidator<LoginRequest>
    {
        public LoginValidator()
        {
            RuleFor(x => x.Username).NotEmpty();
            RuleFor(x => x.Password).NotEmpty();
        }
    }

    public class CreateUserValidator : AbstractValidator<CreateUserRequest>
    {
        public CreateUserValidator()
        {
            RuleFor(x => x.Username).NotEmpty().MaximumLength(50);
            RuleFor(x => x.DisplayName).NotEmpty().MaximumLength(100);
            RuleFor(x => x.Password).NotEmpty().MinimumLength(6);
            RuleFor(x => x.Role).NotEmpty().Must(r => r is "Administrator" or "Manager" or "Cashier");
        }
    }

    public class ReceiptValidator : AbstractValidator<ReceiptRequest>
    {
        public ReceiptValidator()
        {
            RuleFor(x => x.WarehouseId).NotEmpty();
            RuleFor(x => x.Items).NotEmpty();
            RuleForEach(x => x.Items).ChildRules(item =>
            {
                item.RuleFor(i => i.ProductId).NotEmpty();
                item.RuleFor(i => i.Quantity).GreaterThan(0);
                item.RuleFor(i => i.UnitCost).GreaterThanOrEqualTo(0);
            });
        }
    }
}