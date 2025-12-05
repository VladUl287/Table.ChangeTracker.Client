using System.Text;

namespace Tracker.AspNet.Utils;

public sealed class CacheControlBuilder
{
    private readonly List<string> _directives = [];

    private byte _flags;
    private const byte NoCacheFlag = 1 << 0;
    private const byte NoStoreFlag = 1 << 1;

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
