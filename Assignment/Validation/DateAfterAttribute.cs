using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Mvc.ModelBinding.Validation;

namespace Assignment.Validation;

/// <summary>
/// Validates that a date is after another property's date, on the server and,
/// through jQuery unobtrusive validation (Scripts/validation.js), in the browser.
/// </summary>
[AttributeUsage(AttributeTargets.Property)]
public class DateAfterAttribute : ValidationAttribute, IClientModelValidator
{
    public string OtherProperty { get; }

    public DateAfterAttribute(string otherProperty)
        : base("{0} must be after {1}.") => OtherProperty = otherProperty;

    public override string FormatErrorMessage(string name) => string.Format(ErrorMessageString, name, OtherProperty);

    protected override ValidationResult? IsValid(object? value, ValidationContext context)
    {
        var other = context.ObjectType.GetProperty(OtherProperty)?.GetValue(context.ObjectInstance);
        if (value is DateTime end && other is DateTime start && end <= start)
            return new ValidationResult(FormatErrorMessage(context.DisplayName), new[] { context.MemberName! });
        return ValidationResult.Success;
    }

    public void AddValidation(ClientModelValidationContext context)
    {
        context.Attributes["data-val"] = "true";
        context.Attributes["data-val-dateafter"] = FormatErrorMessage(context.ModelMetadata.GetDisplayName());
        context.Attributes["data-val-dateafter-other"] = "*." + OtherProperty;
    }
}

/// <summary>Validates that a date is not in the past (with a few minutes' grace).</summary>
[AttributeUsage(AttributeTargets.Property)]
public class NotInPastAttribute : ValidationAttribute, IClientModelValidator
{
    public NotInPastAttribute() : base("{0} cannot be in the past.") { }

    public override bool IsValid(object? value) =>
        value is not DateTime d || d >= DateTime.Now.AddMinutes(-5);

    public void AddValidation(ClientModelValidationContext context)
    {
        context.Attributes["data-val"] = "true";
        context.Attributes["data-val-notinpast"] = FormatErrorMessage(context.ModelMetadata.GetDisplayName());
    }
}
