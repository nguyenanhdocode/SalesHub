using System.Security.Cryptography;
using System.Text;
using Konscious.Security.Cryptography;

namespace Infrastructure.Security;

public class ArgonPasswordHasher
{
    public string Hash(string password)
    {
        byte[] salt = RandomNumberGenerator.GetBytes(16);

        var argon2 = new Argon2id(Encoding.UTF8.GetBytes(password))
        {
            Salt = salt,

            DegreeOfParallelism = 4,
            MemorySize = 65536,
            Iterations = 3
        };

        byte[] hash = argon2.GetBytes(32);

        return string.Join('.',
            Convert.ToBase64String(salt),
            Convert.ToBase64String(hash));
    }

    public async Task<bool> Verify(string password, string storedHash)
    {
        var parts = storedHash.Split('.');

        var salt = Convert.FromBase64String(parts[0]);
        var expectedHash = Convert.FromBase64String(parts[1]);

        var argon2 = new Argon2id(Encoding.UTF8.GetBytes(password))
        {
            Salt = salt,
            DegreeOfParallelism = 4,
            MemorySize = 65536,
            Iterations = 3
        };

        var actualHash = await argon2.GetBytesAsync(32);

        return CryptographicOperations.FixedTimeEquals(
            actualHash,
            expectedHash);
    }
}
