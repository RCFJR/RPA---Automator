using System.Text.RegularExpressions;

namespace RPA.Core.Services;

public class PlaceholderResolver : IPlaceholderResolver
{
    private static readonly Regex PlaceholderPattern = new(@"\{\{(\w+)\}\}", RegexOptions.Compiled);

    public string Resolve(string template, IReadOnlyDictionary<string, string> rowValues)
    {
        return PlaceholderPattern.Replace(template, m =>
        {
            var key = m.Groups[1].Value;
            return rowValues.TryGetValue(key, out var value) ? value : m.Value;
        });
    }

    public IEnumerable<string> ExtractPlaceholders(string template)
    {
        var matches = PlaceholderPattern.Matches(template);
        return matches.Cast<Match>()
            .Select(m => m.Groups[1].Value)
            .Distinct();
    }
}
