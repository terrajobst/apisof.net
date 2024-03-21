#nullable enable
using System.Collections.Immutable;
using System.Runtime.InteropServices;

namespace ApisOfDotNet.Shared;

public sealed class FilterText
{
    public static FilterText Empty { get; } = new(ImmutableArray<string>.Empty, ImmutableArray<string>.Empty);

    private FilterText(ImmutableArray<string> includedTerms, ImmutableArray<string> excludedTerms)
    {
        IncludedTerms = includedTerms;
        ExcludedTerms = excludedTerms;
    }

    public ImmutableArray<string> IncludedTerms { get; }
    public ImmutableArray<string> ExcludedTerms { get; }

    public static FilterText Create(string? text)
    {
        if (string.IsNullOrEmpty(text))
            return Empty;
        
        var terms = text.Split(' ', StringSplitOptions.TrimEntries | StringSplitOptions.RemoveEmptyEntries);
        var includedTermsBuilder = ImmutableArray.CreateBuilder<string>(terms.Length);
        ImmutableArray<string>.Builder? excludedTermsBuilder = null;

        foreach (var term in terms)
        {
            if (!term.StartsWith('-'))
            {
                includedTermsBuilder.Add(term);
            }
            else
            {
                var excludedTerm = term.Substring(1);
                excludedTermsBuilder ??= ImmutableArray.CreateBuilder<string>();
                excludedTermsBuilder.Add(excludedTerm);
            }
        }

        var includedTerms = includedTermsBuilder.ToImmutable();
        var excludedTerms = excludedTermsBuilder?.ToImmutable() ?? ImmutableArray<string>.Empty;
        return new FilterText(includedTerms, excludedTerms);
    }
}