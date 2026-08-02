using System.Globalization;
using System.Text;

namespace ForgeDotNet.Domain.Content;

internal static class ContentSearchText
{
    public static string Normalize(string value)
    {
        ArgumentNullException.ThrowIfNull(value);

        string decomposed = value.Normalize(NormalizationForm.FormD);
        var builder = new StringBuilder(decomposed.Length);
        bool previousWasSeparator = true;
        foreach (char character in decomposed)
        {
            if (CharUnicodeInfo.GetUnicodeCategory(character) == UnicodeCategory.NonSpacingMark)
            {
                continue;
            }

            char normalized = char.ToLowerInvariant(character);
            if (char.IsLetterOrDigit(normalized) || normalized is '.' or '-')
            {
                builder.Append(normalized);
                previousWasSeparator = false;
            }
            else if (!previousWasSeparator)
            {
                builder.Append(' ');
                previousWasSeparator = true;
            }
        }

        return builder.ToString().Trim();
    }
}
