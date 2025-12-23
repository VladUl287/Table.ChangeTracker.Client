using Tracker.AspNet.Models;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using Tracker.Core.Services.Contracts;
using Tracker.AspNet.Services.Contracts;
using Microsoft.Extensions.DependencyInjection;

namespace Tracker.AspNet.Services;

public sealed class DefaultProviderResolver(ILogger<DefaultProviderResolver> logger) : IProviderResolver
{
    public ISourceProvider ResolveProvider(HttpContext ctx, ImmutableGlobalOptions options, out bool shouldDispose)
    {
        ArgumentNullException.ThrowIfNull(ctx, nameof(ctx));
        ArgumentNullException.ThrowIfNull(options, nameof(options));

        var traceId = new TraceId(ctx);
        try
        {
            shouldDispose = false;

            if (options.Source is not null)
            {
                logger.LogDebug("Resolving keyed provider: {Source}. TraceId: {TraceId}", options.Source, traceId);
                return ctx.RequestServices.GetRequiredKeyedService<ISourceProvider>(options.Source);
            }

            if (options.SourceProvider is not null)
            {
                logger.LogDebug("Using direct provider instance. TraceId: {TraceId}", traceId);
                return options.SourceProvider;
            }

            if (options.SourceProviderFactory is not null)
            {
                logger.LogDebug("Creating provider via factory. TraceId: {TraceId}", traceId);
                shouldDispose = true;
                return options.SourceProviderFactory(ctx);
            }

            throw new InvalidOperationException(
                $"Unable to resolve source provider. No source configuration provided. TraceId: {ctx.TraceIdentifier}");
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed to resolve source provider. TraceId: {TraceId}", traceId);
            throw new InvalidOperationException(
                $"Failed to resolve source provider. TraceId: {ctx.TraceIdentifier}", ex);
        }
    }
}
