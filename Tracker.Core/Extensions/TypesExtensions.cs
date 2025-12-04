using System.Security.Cryptography;
using System.Text;

namespace Tracker.Core.Extensions;

public static class TypesExtensions
{
    public static string GetTypeHashId(this Type type)
    {
        var typeName = type.FullName ?? throw new NullReferenceException();
        var hashBytes = SHA256.HashData(Encoding.UTF8.GetBytes(typeName));

        var builder = new StringBuilder();
        foreach (byte b in hashBytes)
            builder.Append(b.ToString("x2"));

        return builder.ToString();
    }
}
