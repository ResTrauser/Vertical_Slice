using System.Security.Cryptography;
using System.Text;

namespace Api.Shared.Security;

public sealed class TokenHasher
{
    public string Hash(string token)
    {
        var bytes = Encoding.UTF8.GetBytes(token);
        var hash = SHA256.HashData(bytes);
        return Convert.ToBase64String(hash);
    }
}
