using CustomerPaymentAPI.DTOs.Customer;
using FluentValidation;

namespace CustomerPaymentAPI.Validators
{
    public class CustomerRequestValidator : AbstractValidator<CustomerRequestDto>
    {
        public CustomerRequestValidator()
        {
            RuleFor(x => x.Nombre)
                .NotEmpty().WithMessage("El nombre es obligatorio")
                .MaximumLength(100).WithMessage("El nombre no puede tener más de 100 caracteres");

            RuleFor(x => x.Email)
                .NotEmpty().WithMessage("El email es obligatorio")
                .EmailAddress().WithMessage("Formato de email inválido")
                .MaximumLength(100).WithMessage("El email no puede tener más de 100 caracteres");

            RuleFor(x => x.Telefono)
                .MaximumLength(20).WithMessage("El teléfono no puede tener más de 20 caracteres");

            RuleFor(x => x.Direccion)
                .MaximumLength(255).WithMessage("La dirección no puede tener más de 255 caracteres");
        }
    }
}
