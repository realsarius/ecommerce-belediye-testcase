using System.Text.RegularExpressions;

namespace EcommerceAPI.Infrastructure.Utilities;

public static partial class SensitiveDataLogSanitizer
{
    private const string Redacted = "[REDACTED]";

    public static string? Sanitize(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return value;
        }

        var sanitized = value;
        sanitized = SensitiveKeyValueRegex().Replace(sanitized, match => $"{match.Groups["key"].Value}={Redacted}");
        sanitized = PanRegex().Replace(sanitized, _ => Redacted);
        sanitized = CvvRegex().Replace(sanitized, match => $"{match.Groups["key"].Value} {Redacted}");
        return sanitized;
    }

    [GeneratedRegex(@"(?ix)
        (?<key>cardnumber|pan|cvv|cvc|cardtoken|cardus(?:er)?key|secretkey|apikey|token)
        \s*[:=]\s*
        (?<value>[^\s,;]+)")]
    private static partial Regex SensitiveKeyValueRegex();

    [GeneratedRegex(@"(?<!\d)(?:\d[ -]?){12,19}(?!\d)")]
    private static partial Regex PanRegex();

    [GeneratedRegex(@"(?ix)
        (?<key>cvv|cvc|security\ code)
        [^\d]{0,10}
        \d{3,4}")]
    private static partial Regex CvvRegex();
}
