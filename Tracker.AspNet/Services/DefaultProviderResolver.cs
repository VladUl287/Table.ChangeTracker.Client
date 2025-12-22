using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using System.Collections.Frozen;
using Tracker.AspNet.Models;
using Tracker.AspNet.Services.Contracts;
using Tracker.Core.Services.Contracts;

namespace Tracker.AspNet.Services;

public sealed class DefaultProviderResolver(
    IEnumerable<ISourceProvider> providers, IProviderIdGenerator idGenerator, ILogger<DefaultProviderResolver> logger) : IProviderResolver
{
    private readonly FrozenDictionary<string, ISourceProvider> _store = providers.ToFrozenDictionary(c => c.Id);
    private readonly ISourceProvider _default = providers.First();

    public ISourceProvider? SelectProvider(string? providerId, ImmutableGlobalOptions options)
    {
        if (providerId is not null)
        {
            if (_store.TryGetValue(providerId, out var provider))
            {
                logger.LogInformation("Provider '{ProviderId}' successfully resolved from explicit provider ID.", providerId);
                return provider;
            }

            throw new InvalidOperationException($"Fail to resolve provider for excplicit provider ID - '{providerId}'");
        }

        if (options is { SourceProvider: null, SourceProviderFactory: null })
        {
            logger.LogInformation(
                "No explicit provider ID provided and no source operations configured. Returning default provider '{DefaultProviderId}'.", _default.Id);
            return _default;
        }

        logger.LogInformation("No explicit source ID provided. Returning source operations from options.");
        return options.SourceProvider;
    }

    public ISourceProvider? SelectProvider(GlobalOptions options)
    {
        var sourceId = options.Source;

        if (sourceId is not null)
        {
            if (_store.TryGetValue(sourceId, out var provider))
            {
                logger.LogInformation("Provider '{ProviderId}' successfully resolved from GlobalOptions source.", sourceId);
                return provider;
            }

            throw new InvalidOperationException(
                $"Failed to resolve source provider from GlobalOptions. Provider with ID '{sourceId}' was not found. " +
                $"Available provider IDs: {string.Join(", ", _store.Keys)}");
        }

        if (options is { SourceProvider: null, SourceProviderFactory: null })
        {
            logger.LogInformation(
                "No source ID in GlobalOptions and no source operations configured. " +
                "Returning default provider '{DefaultProviderId}'.", _default.Id);
            return _default;
        }

        logger.LogInformation("No provider ID in GlobalOptions. Returning source operations from GlobalOptions.");
        return options.SourceProvider;
    }

    public ISourceProvider? SelectProvider<TContext>(string? sourceId, ImmutableGlobalOptions options) where TContext : DbContext
    {
        if (sourceId is not null)
        {
            if (_store.TryGetValue(sourceId, out var provider))
            {
                logger.LogInformation(
                    "Provider '{ProviderId}' successfully resolved from explicit source ID for context '{ContextType}'.",
                    sourceId, typeof(TContext).Name);
                return provider;
            }

            throw new InvalidOperationException(
                $"Failed to resolve source provider for context '{typeof(TContext).Name}'. " +
                $"Provider with ID '{sourceId}' was not found. " +
                $"Available provider IDs: {string.Join(", ", _store.Keys)}");
        }

        logger.LogInformation(
            "No explicit source ID provided for context '{ContextType}'. Attempting to generate provider ID based on context type.",
            typeof(TContext).Name);

        sourceId = idGenerator.GenerateId<TContext>();

        if (_store.TryGetValue(sourceId, out var contextProvider))
        {
            logger.LogInformation(
                "Provider '{ProviderId}' successfully resolved from generated ID for context '{ContextType}'.",
                sourceId, typeof(TContext).Name);
            return contextProvider;
        }

        if (options is { SourceProvider: null, SourceProviderFactory: null })
        {
            logger.LogInformation(
                "Generated provider ID '{GeneratedId}' not found for context '{ContextType}' and no source operations configured. Returning default provider '{DefaultProviderId}'.",
                sourceId, typeof(TContext).Name, _default.Id);
            return _default;
        }

        logger.LogInformation(
            "Generated provider ID '{GeneratedId}' not found for context '{ContextType}'. Returning source operations from options.",
            sourceId, typeof(TContext).Name);
        return options.SourceProvider;
    }

    public ISourceProvider? SelectProvider<TContext>(GlobalOptions options) where TContext : DbContext
    {
        var sourceId = options.Source;

        if (sourceId is not null)
        {
            if (_store.TryGetValue(sourceId, out var provider))
            {
                logger.LogInformation(
                    "Provider '{ProviderId}' successfully resolved from GlobalOptions source for context '{ContextType}'.",
                    sourceId, typeof(TContext).Name);
                return provider;
            }

            throw new InvalidOperationException(
                $"Failed to resolve source provider for context '{typeof(TContext).Name}' from GlobalOptions. " +
                $"Provider with ID '{sourceId}' was not found. " +
                $"Available provider IDs: {string.Join(", ", _store.Keys)}");
        }

        logger.LogInformation(
            "No source ID in GlobalOptions for context '{ContextType}'. " +
            "Attempting to generate provider ID based on context type.",
            typeof(TContext).Name);

        sourceId = idGenerator.GenerateId<TContext>();

        logger.LogInformation(
            "Generated provider ID '{GeneratedId}' for context '{ContextType}'.",
            sourceId, typeof(TContext).Name);

        if (_store.TryGetValue(sourceId, out var contextProvider))
        {
            logger.LogInformation(
                "Provider '{ProviderId}' successfully resolved from generated ID for context '{ContextType}'.",
                sourceId, typeof(TContext).Name);
            return contextProvider;
        }

        if (options is { SourceProvider: null, SourceProviderFactory: null })
        {
            logger.LogInformation(
                "Generated provider ID '{GeneratedId}' not found for context '{ContextType}' and no source operations configured in GlobalOptions. " +
                "Returning default provider '{DefaultProviderId}'.",
                sourceId, typeof(TContext).Name, _default.Id);
            return _default;
        }

        logger.LogInformation(
            "Generated provider ID '{GeneratedId}' not found for context '{ContextType}'. " +
            "Returning source operations from GlobalOptions.",
            sourceId, typeof(TContext).Name);
        return options.SourceProvider;
    }
}
