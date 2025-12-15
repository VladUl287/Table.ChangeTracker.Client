using Microsoft.Extensions.Primitives;
using System.Diagnostics.CodeAnalysis;

namespace Tracker.AspNet.Services.Contracts;

public interface IDirectiveChecker
{
    ReadOnlySpan<string> DefaultInvalidRequestDirectives { get; }
    ReadOnlySpan<string> DefaultInvalidResponseDirectives { get; }
    bool AnyInvalidDirective(StringValues headers, ReadOnlySpan<string> invalidDirectives, [NotNullWhen(true)] out string? directive);
}
