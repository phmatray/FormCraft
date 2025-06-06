using FluentValidation;
using DynamicFormBlazor.Models;

namespace DynamicFormBlazor.Validators;

public class PersonValidator : AbstractValidator<Person>
{
    public PersonValidator()
    {
        RuleFor(x => x.FirstName)
            .NotEmpty().WithMessage("First name is required")
            .MinimumLength(2).WithMessage("First name must be at least 2 characters")
            .MaximumLength(50).WithMessage("First name cannot exceed 50 characters")
            .Matches(@"^[a-zA-Z\s-']+$").WithMessage("First name can only contain letters, spaces, hyphens, and apostrophes");

        RuleFor(x => x.LastName)
            .NotEmpty().WithMessage("Last name is required")
            .MinimumLength(2).WithMessage("Last name must be at least 2 characters")
            .MaximumLength(50).WithMessage("Last name cannot exceed 50 characters")
            .Matches(@"^[a-zA-Z\s-']+$").WithMessage("Last name can only contain letters, spaces, hyphens, and apostrophes");

        RuleFor(x => x.Email)
            .NotEmpty().WithMessage("Email is required")
            .EmailAddress().WithMessage("Please enter a valid email address");

        RuleFor(x => x.Age)
            .InclusiveBetween(1, 120).WithMessage("Age must be between 1 and 120");

        RuleFor(x => x.PhoneNumber)
            .Matches(@"^\+?[\d\s-()]+$").WithMessage("Please enter a valid phone number")
            .When(x => !string.IsNullOrEmpty(x.PhoneNumber));

        RuleFor(x => x.DateOfBirth)
            .LessThan(DateTime.Today).WithMessage("Date of birth must be in the past")
            .GreaterThan(DateTime.Today.AddYears(-120)).WithMessage("Date of birth cannot be more than 120 years ago")
            .When(x => x.DateOfBirth.HasValue);

        RuleFor(x => x.Address)
            .MaximumLength(100).WithMessage("Address cannot exceed 100 characters")
            .When(x => !string.IsNullOrEmpty(x.Address));

        RuleFor(x => x.City)
            .MaximumLength(50).WithMessage("City cannot exceed 50 characters")
            .Matches(@"^[a-zA-Z\s-]+$").WithMessage("City can only contain letters, spaces, and hyphens")
            .When(x => !string.IsNullOrEmpty(x.City));

        RuleFor(x => x.PostalCode)
            .Matches(@"^[A-Za-z0-9\s-]+$").WithMessage("Please enter a valid postal code")
            .MaximumLength(10).WithMessage("Postal code cannot exceed 10 characters")
            .When(x => !string.IsNullOrEmpty(x.PostalCode));
    }
}