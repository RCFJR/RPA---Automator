namespace RPA.Core.Services;

public interface IPlaceholderResolver
{
    string Resolve(string template, IReadOnlyDictionary<string, string> rowValues);
    IEnumerable<string> ExtractPlaceholders(string template);
}
