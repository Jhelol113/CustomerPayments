using CustomerPaymentAPI.DTOs.Payment;
using FluentValidation;

namespace CustomerPaymentAPI.Validators
{
    public class PaymentRequestValidator : AbstractValidator<PaymentRequestDto>
    {
        public PaymentRequestValidator()
        {
            RuleFor(x => x.CustomerId)
                .NotEmpty().WithMessage("El ID del cliente es obligatorio");

            RuleFor(x => x.Monto)
                .NotEmpty().WithMessage("El monto es obligatorio")
                .GreaterThan(0).WithMessage("El monto debe ser mayor a 0");

            RuleFor(x => x.MetodoPago)
                .NotEmpty().WithMessage("El método de pago es obligatorio")
                .MaximumLength(50).WithMessage("El método de pago no puede tener más de 50 caracteres");

            RuleFor(x => x.Estado)
                .MaximumLength(20).WithMessage("El estado no puede tener más de 20 caracteres");
        }
    }
}
