using System.Text;

namespace Tracker.AspNet.Utils;

public sealed class CacheControlBuilder
{
    private readonly List<string> _directives = [];

    private byte _flags;

    private const byte NoCacheFlag = 1 << 0;
    private const byte NoStoreFlag = 1 << 1;
    private const byte NoTransformFlag = 1 << 2;
    private const byte MustRevalidateFlag = 1 << 3;
    private const byte ProxyRevalidateFlag = 1 << 4;
    private const byte MustUnderstandFlag = 1 << 5;
    private const byte PrivateFlag = 1 << 6;
    private const byte PublicFlag = 1 << 7;

    private const byte ImmutableFlag = 1 << 0;
    private const byte StaleWhileRevalidate = 1 << 1;
    private const byte StaleIfError = 1 << 2;

    private int? _maxAge;

    public CacheControlBuilder WithDirective(string directive)
    {
        _directives.Add(directive);
        return this;
    }

    public CacheControlBuilder WithNoCache()
    {
        _flags |= NoCacheFlag;
        return this;
    }

    public CacheControlBuilder WithNoStore()
    {
        _flags |= NoStoreFlag;
        return this;
    }

    public CacheControlBuilder WithMaxAge(TimeSpan duration)
    {
        _maxAge = (int)duration.TotalSeconds;
        return this;
    }

    private void AppendNumericDirectives(StringBuilder sb)
    {
        if (_maxAge.HasValue)
            sb.Append($"max-age={_maxAge.Value}");
    }

    private void AppendBooleanDirectives(StringBuilder sb)
    {
        if ((_flags & NoCacheFlag) != 0)
            sb.Append("no-cache");

        if ((_flags & NoStoreFlag) != 0)
            sb.Append("no-store");

        if ((_flags & PrivateFlag) != 0)
            sb.Append("private");

        if ((_flags & PublicFlag) != 0)
            sb.Append("public");

        if ((_flags & MustRevalidateFlag) != 0)
            sb.Append("must-revalidate");

        if ((_flags & ProxyRevalidateFlag) != 0)
            sb.Append("proxy-revalidate");

        if ((_flags & NoTransformFlag) != 0)
            sb.Append("no-transform");

        if ((_flags & ImmutableFlag) != 0)
            sb.Append("immutable");
    }

    private void AppendCustomDirectives(StringBuilder sb)
    {
        foreach (var directive in _directives)
            sb.Append(directive);
    }

    public string Build()
    {
        var sb = new StringBuilder("Cache-Control: ");

        AppendBooleanDirectives(sb);
        AppendNumericDirectives(sb);
        AppendCustomDirectives(sb);

        return sb.ToString();
    }
}
