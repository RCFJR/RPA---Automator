using RPA.Core.Models;
using RPA.Core.Services;

namespace RPA.Core.Tests;

public class PlaceholderResolverTests
{
    [Fact]
    public void Resolve_WithValidPlaceholders_ReplacesWithRowValues()
    {
        var resolver = new PlaceholderResolver();
        var template = "Olá {{Nome}}, seu CPF é {{CPF}}.";
        var rowValues = new Dictionary<string, string>
        {
            { "Nome", "João Silva" },
            { "CPF", "123.456.789-00" }
        };

        var result = resolver.Resolve(template, rowValues);

        Assert.Equal("Olá João Silva, seu CPF é 123.456.789-00.", result);
    }

    [Fact]
    public void Resolve_WithMissingPlaceholder_KeepsPlaceholder()
    {
        var resolver = new PlaceholderResolver();
        var template = "Email: {{Email}}, Telefone: {{Telefone}}";
        var rowValues = new Dictionary<string, string>
        {
            { "Email", "joao@example.com" }
        };

        var result = resolver.Resolve(template, rowValues);

        Assert.Equal("Email: joao@example.com, Telefone: {{Telefone}}", result);
    }

    [Fact]
    public void ExtractPlaceholders_WithMultiplePlaceholders_ReturnsAllPlaceholders()
    {
        var resolver = new PlaceholderResolver();
        var template = "Olá {{Nome}}, seu CPF é {{CPF}}, email {{Email}}.";

        var placeholders = resolver.ExtractPlaceholders(template).ToList();

        Assert.Equal(3, placeholders.Count);
        Assert.Contains("Nome", placeholders);
        Assert.Contains("CPF", placeholders);
        Assert.Contains("Email", placeholders);
    }

    [Fact]
    public void ExtractPlaceholders_WithDuplicatePlaceholders_ReturnDistinct()
    {
        var resolver = new PlaceholderResolver();
        var template = "{{Nome}} e {{Nome}} são iguais.";

        var placeholders = resolver.ExtractPlaceholders(template).ToList();

        Assert.Single(placeholders);
        Assert.Contains("Nome", placeholders);
    }

    [Fact]
    public void Resolve_WithAcentuacao_PreservesAccents()
    {
        var resolver = new PlaceholderResolver();
        var template = "Cidade: {{Cidade}}, País: {{País}}";
        var rowValues = new Dictionary<string, string>
        {
            { "Cidade", "São Paulo" },
            { "País", "Brasil" }
        };

        var result = resolver.Resolve(template, rowValues);

        Assert.Equal("Cidade: São Paulo, País: Brasil", result);
    }
}
