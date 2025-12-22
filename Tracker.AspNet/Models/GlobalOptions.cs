using Tracker.AspNet.Utils;
using Microsoft.AspNetCore.Http;
using System.Collections.Immutable;
using Tracker.Core.Services.Contracts;

namespace Tracker.AspNet.Models;

public sealed class GlobalOptions
{
    public string? Source { get; set; }
    public ISourceProvider? SourceProvider { get; set; }
    public Func<HttpContext, ISourceProvider>? SourceProviderFactory { get; set; }

    public Func<HttpContext, bool> Filter { get; set; } = (_) => true;
    public HashSet<string> InvalidResponseDirectives { get; init; } = ["no-transform", "no-store", "immutable"];
    public HashSet<string> InvalidRequestDirectives { get; init; } = ["no-transform", "no-store"];

    public string[] Tables { get; set; } = [];
    public Type[] Entities { get; set; } = [];

    public string? CacheControl { get; set; }
    public CacheControlBuilder? CacheControlBuilder { get; set; }

    public Func<HttpContext, string> Suffix { get; set; } = (_) => string.Empty;
}

public sealed record ImmutableGlobalOptions
{
    public ISourceProvider? SourceProvider { get; init; }
    public Func<HttpContext, ISourceProvider>? SourceProviderFactory { get; init; }

    public Func<HttpContext, bool> Filter { get; init; } = (_) => true;
    public ImmutableArray<string> InvalidResponseDirectives { get; init; } = [];
    public ImmutableArray<string> InvalidRequestDirectives { get; init; } = [];

    public ImmutableArray<string> Tables { get; init; } = [];

    public string CacheControl { get; init; } = string.Empty;

    public Func<HttpContext, string> Suffix { get; init; } = (_) => string.Empty;
}