using System.Text.Json.Serialization;

namespace RPA.Core.Models;

[JsonPolymorphic(TypeDiscriminatorPropertyName = "$type")]
[JsonDerivedType(typeof(CsvExecutionSource), "csv")]
[JsonDerivedType(typeof(RepeatExecutionSource), "repeat")]
public abstract record ExecutionSource
{
    public abstract int GetIterationCount();
    public abstract IReadOnlyDictionary<string, string> GetRowAt(int index);
}

public record CsvExecutionSource(List<CsvRow> Rows) : ExecutionSource
{
    public override int GetIterationCount() => Rows.Count;

    public override IReadOnlyDictionary<string, string> GetRowAt(int index) =>
        Rows[index];
}

public record RepeatExecutionSource(int RepeatCount) : ExecutionSource
{
    public override int GetIterationCount() => RepeatCount;

    public override IReadOnlyDictionary<string, string> GetRowAt(int index) =>
        new Dictionary<string, string> { ["_iteration"] = (index + 1).ToString() };
}
