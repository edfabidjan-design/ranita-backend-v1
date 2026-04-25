using System.Text;
using System.Text.RegularExpressions;

namespace EcommerceApi.Helpers;

public static class SlugHelper
{
    public static string Slugify(string input)
    {
        if (string.IsNullOrWhiteSpace(input))
            return string.Empty;

        string s = input.Trim().ToLowerInvariant();

        // ✅ Supprime les accents
        s = s.Normalize(NormalizationForm.FormD);
        var sb = new StringBuilder();
        foreach (var c in s)
        {
            var uc = System.Globalization.CharUnicodeInfo.GetUnicodeCategory(c);
            if (uc != System.Globalization.UnicodeCategory.NonSpacingMark)
                sb.Append(c);
        }
        s = sb.ToString().Normalize(NormalizationForm.FormC);

        // ✅ Nettoyage
        s = Regex.Replace(s, @"[^a-z0-9\s-]", "");
        s = Regex.Replace(s, @"\s+", " ").Trim();
        s = s.Replace(" ", "-");
        s = Regex.Replace(s, @"-+", "-").Trim('-');

        return s;
    }
}
