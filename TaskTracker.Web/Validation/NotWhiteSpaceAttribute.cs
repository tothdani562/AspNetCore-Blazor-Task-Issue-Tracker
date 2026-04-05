using System.ComponentModel.DataAnnotations;

namespace TaskTracker.Web.Validation;

[AttributeUsage(AttributeTargets.Property | AttributeTargets.Field | AttributeTargets.Parameter, AllowMultiple = false)]
public sealed class NotWhiteSpaceAttribute : ValidationAttribute
{
    public NotWhiteSpaceAttribute()
    {
        ErrorMessage = "A mező nem lehet csak whitespace.";
    }

    public override bool IsValid(object? value)
    {
        if (value is null)
        {
            return true;
        }

        return value is string stringValue
            ? !string.IsNullOrWhiteSpace(stringValue)
            : false;
    }
}
