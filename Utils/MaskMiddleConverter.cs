using System.Globalization;
using System.Windows.Data;

namespace wzd32.Utils;


public class MaskMiddleConverter : IValueConverter
{
    // parameter："2,1,x" -> keepFirst=2, keepLast=1, maskChar='x'
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        if (value is not string s) return value;

        // defaults
        int keepFirst = 2;
        int keepLast = 1;
        char mask = 'x';

        if (parameter is string p)
        {
            // assumptions
            var parts = p.Split(',');
            if (parts.Length >= 2 && int.TryParse(parts[0], out var kf) && int.TryParse(parts[1], out var kl))
            {
                keepFirst = Math.Max(0, kf);
                keepLast = Math.Max(0, kl);
            }
            if (parts.Length >= 3 && !string.IsNullOrEmpty(parts[2]))
                mask = parts[2][0];
        }

        if (s.Length <= keepFirst + keepLast) return new string(mask, s.Length);

        int maskCount = s.Length - keepFirst - keepLast;
        return s.Substring(0, keepFirst) + new string(mask, maskCount) + s.Substring(s.Length - keepLast, keepLast);
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        => throw new NotImplementedException();

}


