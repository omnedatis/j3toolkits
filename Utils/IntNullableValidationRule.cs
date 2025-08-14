using System.Globalization;
using System.Windows.Controls;

namespace wzd32.Utils;

public class IntNullableValidationRule : ValidationRule
{
    public override ValidationResult Validate(object value, CultureInfo cultureInfo)
    {
        var s = value?.ToString() ?? string.Empty;
        if (string.IsNullOrWhiteSpace(s))
            return ValidationResult.ValidResult; // 空白 => null

        if (int.TryParse(s, NumberStyles.Integer, cultureInfo, out _))
            return ValidationResult.ValidResult;

        return new ValidationResult(false, "必須為整數或留空");
    }
}
