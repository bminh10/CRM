using System.Security.Cryptography;
using System.Text;

namespace CRM.Api.Security;

public static class TotpService
{
    private const int SecretLength = 20;
    private const int CodeLength = 6;
    private const long PeriodSeconds = 30;

    public static string GenerateSecret() => EncodeBase32(RandomNumberGenerator.GetBytes(SecretLength));

    public static string CreateProvisioningUri(string email, string secret)
    {
        var label = Uri.EscapeDataString($"CRM:{email}");
        var issuer = Uri.EscapeDataString("CRM");
        return $"otpauth://totp/{label}?secret={secret}&issuer={issuer}&algorithm=SHA1&digits={CodeLength}&period={PeriodSeconds}";
    }

    public static bool TryVerify(string secret, string code, DateTimeOffset now, out long matchedStep)
    {
        matchedStep = -1;
        if (string.IsNullOrEmpty(code) || code.Length != CodeLength
            || code.Any(character => character is < '0' or > '9'))
            return false;

        byte[] key;
        try
        {
            key = DecodeBase32(secret);
        }
        catch (FormatException)
        {
            return false;
        }

        var currentStep = now.ToUnixTimeSeconds() / PeriodSeconds;
        foreach (var step in new[] { currentStep, currentStep - 1, currentStep + 1 })
        {
            var expected = CreateCode(key, step);
            if (CryptographicOperations.FixedTimeEquals(
                Encoding.ASCII.GetBytes(expected), Encoding.ASCII.GetBytes(code)))
            {
                matchedStep = step;
                return true;
            }
        }

        return false;
    }

    private static string CreateCode(byte[] key, long step)
    {
        Span<byte> counter = stackalloc byte[sizeof(long)];
        System.Buffers.Binary.BinaryPrimitives.WriteInt64BigEndian(counter, step);
        var hash = HMACSHA1.HashData(key, counter);
        var offset = hash[^1] & 0x0f;
        var binary = ((hash[offset] & 0x7f) << 24)
            | ((hash[offset + 1] & 0xff) << 16)
            | ((hash[offset + 2] & 0xff) << 8)
            | (hash[offset + 3] & 0xff);
        return (binary % 1_000_000).ToString($"D{CodeLength}", System.Globalization.CultureInfo.InvariantCulture);
    }

    private static string EncodeBase32(ReadOnlySpan<byte> data)
    {
        const string alphabet = "ABCDEFGHIJKLMNOPQRSTUVWXYZ234567";
        var result = new StringBuilder((data.Length * 8 + 4) / 5);
        var buffer = 0;
        var bitsLeft = 0;
        foreach (var value in data)
        {
            buffer = (buffer << 8) | value;
            bitsLeft += 8;
            while (bitsLeft >= 5)
            {
                result.Append(alphabet[(buffer >> (bitsLeft - 5)) & 31]);
                bitsLeft -= 5;
            }
        }
        if (bitsLeft > 0)
            result.Append(alphabet[(buffer << (5 - bitsLeft)) & 31]);
        return result.ToString();
    }

    private static byte[] DecodeBase32(string value)
    {
        const string alphabet = "ABCDEFGHIJKLMNOPQRSTUVWXYZ234567";
        var normalized = value.TrimEnd('=').ToUpperInvariant();
        var result = new List<byte>(normalized.Length * 5 / 8);
        var buffer = 0;
        var bitsLeft = 0;
        foreach (var character in normalized)
        {
            var index = alphabet.IndexOf(character);
            if (index < 0)
                throw new FormatException("Invalid Base32 value.");
            buffer = (buffer << 5) | index;
            bitsLeft += 5;
            if (bitsLeft >= 8)
            {
                result.Add((byte)(buffer >> (bitsLeft - 8)));
                bitsLeft -= 8;
            }
        }
        return result.ToArray();
    }
}
