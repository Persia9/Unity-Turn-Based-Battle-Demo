using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text.RegularExpressions;

namespace Infrastructure.MasterData.Editor
{
    public static class LocalizationSourceValidator
    {
        public static IReadOnlyList<string> Validate(List<LocalizationSourceRow> rows)
        {
            var errors = new List<string>();
            if (rows == null || rows.Count == 0)
            {
                errors.Add("Localization: at least one row is required.");
                return errors;
            }

            var keys = new HashSet<string>(StringComparer.Ordinal);
            foreach (var row in rows)
            {
                if (row == null)
                {
                    errors.Add("Localization: contains an empty row.");
                    continue;
                }

                if (string.IsNullOrWhiteSpace(row.Key)) errors.Add("Localization: Key is required.");
                else if (!keys.Add(row.Key)) errors.Add($"Localization: duplicate Key '{row.Key}'.");
                if (string.IsNullOrWhiteSpace(row.English))
                {
                    errors.Add($"Localization '{row.Key}': English fallback is required.");
                    continue;
                }

                try
                {
                    var englishArguments = Arguments(row.English);
                    if (!string.IsNullOrEmpty(row.Japanese) && !englishArguments.SetEquals(Arguments(row.Japanese)))
                        errors.Add(
                            $"Localization '{row.Key}': placeholder indices differ between English and Japanese.");
                }
                catch (FormatException error)
                {
                    errors.Add(
                        $"Localization '{row.Key}': invalid format (argument indices must be 0-31): {error.Message}");
                }
            }

            return errors;
        }

        private static HashSet<int> Arguments(string text)
        {
            string.Format(CultureInfo.InvariantCulture, text, new object[32]);
            var indices = new HashSet<int>();
            foreach (Match match in Regex.Matches(text, @"(?<!\{)(?:\{\{)*\{(\d+)"))
                indices.Add(int.Parse(match.Groups[1].Value, CultureInfo.InvariantCulture));
            return indices;
        }
    }
}