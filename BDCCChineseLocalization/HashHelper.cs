using System.Security.Cryptography;
using System.Text;

namespace BDCCChineseLocalization;

public static class HashHelper
{
    private static readonly StringBuilder StringBuilder = new StringBuilder(512);
    public static string GetSha512(string inputString)
    {
        var bytes = Encoding.UTF8.GetBytes(inputString);
        var hash = SHA512.HashData(bytes);
        return GetStringFromHash(hash);
    }
    public static string GetStringFromHash(byte[] hash)
    {
        StringBuilder.Clear();
        foreach (var t in hash)
        {
            StringBuilder.Append(t.ToString("X2"));
        }
        return StringBuilder.ToString();
    }

}