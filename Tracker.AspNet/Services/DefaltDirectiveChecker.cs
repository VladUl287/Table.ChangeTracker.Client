using Microsoft.Extensions.Primitives;
using System.Diagnostics.CodeAnalysis;
using Tracker.AspNet.Services.Contracts;

namespace Tracker.AspNet.Services;

public sealed class DefaltDirectiveChecker : IDirectiveChecker
{
    private static readonly string[] _invalidRequestDirectives = ["no-transform", "no-store"];
    private static readonly string[] _invalidResponseDirectives = ["no-transform", "no-store", "immutable"];

    public ReadOnlySpan<string> DefaultInvalidRequestDirectives => _invalidRequestDirectives;
    public ReadOnlySpan<string> DefaultInvalidResponseDirectives => _invalidResponseDirectives;

    public bool AnyInvalidDirective(StringValues headers, ReadOnlySpan<string> invalidDirectives, [NotNullWhen(true)] out string? directive)
    {
        directive = null;

        if (headers.Count == 0)
            return false;

        foreach (var header in headers)
        {
            if (header is null)
                continue;

            foreach (var invalidDirective in invalidDirectives)
            {
                if (header.Contains(invalidDirective, StringComparison.OrdinalIgnoreCase))
                {
                    directive = invalidDirective;
                    return true;
                }
            }
        }

        return false;
    }
}
