using System.Security.Cryptography;
using System.Text;
using System.Text.Json;

namespace Shared.Security;

public static class CryptoService
{
    public static (string cipherText, string iv) AesEncrypt(object payload, byte[] key)
    {
        var json = JsonSerializer.Serialize(payload);
        using var aes = Aes.Create();
        aes.Key = key;
        aes.GenerateIV();

        using var ms = new MemoryStream();
        using (var cs = new CryptoStream(ms, aes.CreateEncryptor(), CryptoStreamMode.Write))
        {
            var bytes = Encoding.UTF8.GetBytes(json);
            cs.Write(bytes, 0, bytes.Length);
        }

        return (Convert.ToBase64String(ms.ToArray()), Convert.ToBase64String(aes.IV));
    }

    public static T AesDecrypt<T>(string cipherText, string iv, byte[] key)
    {
        using var aes = Aes.Create();
        aes.Key = key;
        aes.IV = Convert.FromBase64String(iv);

        using var ms = new MemoryStream(Convert.FromBase64String(cipherText));
        using var cs = new CryptoStream(ms, aes.CreateDecryptor(), CryptoStreamMode.Read);
        using var reader = new StreamReader(cs, Encoding.UTF8);
        var json = reader.ReadToEnd();
        return JsonSerializer.Deserialize<T>(json)!;
    }

    public static string RsaSign(string data, RSA privateKey)
    {
        var bytes = Encoding.UTF8.GetBytes(data);
        var signature = privateKey.SignData(bytes, HashAlgorithmName.SHA256, RSASignaturePadding.Pkcs1);
        return Convert.ToBase64String(signature);
    }

    public static bool RsaVerify(string data, string signature, string publicKeyPem)
    {
        using var rsa = RSA.Create();
        rsa.ImportFromPem(publicKeyPem);
        var bytes = Encoding.UTF8.GetBytes(data);
        var sigBytes = Convert.FromBase64String(signature);
        return rsa.VerifyData(bytes, sigBytes, HashAlgorithmName.SHA256, RSASignaturePadding.Pkcs1);
    }
}
