namespace ouzel;

using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;


/// <summary></summary>
public static class Aes
{
    /// <summary>初期化ベクトル</summary>
    private static readonly byte[] _iv;

    static Aes()
    {
        _iv = Guid
            .Parse("81516217-729d-41ff-aaf0-fe972f15937e")
            .ToByteArray();
    }


    /// <summary>暗号化</summary>
    /// <param name="key"></param>
    /// <param name="inputStream"></param>
    /// <param name="cancellationToken"></param>
    public static async Task<byte[]> EncryptAsync(
            string            key,
            Stream            inputStream,
            CancellationToken cancellationToken = default(CancellationToken))
    {
        using var transform = CreateTransform(
                iv:        _iv,
                key:       StringToKey(key, Encoding.UTF8),
                isEncrypt: true);

        using var mem        = new MemoryStream();
        using(var strmCrypto = new System.Security.Cryptography.CryptoStream(
                    stream:    mem,
                    transform: transform,
                    mode:      System.Security.Cryptography.CryptoStreamMode.Write))
        {
            await inputStream.CopyToAsync(strmCrypto, 81920, cancellationToken);
            await strmCrypto.FlushAsync(cancellationToken);
        }

        return mem.ToArray();
    }


    /// <summary>暗号化</summary>
    /// <param name="key"></param>
    /// <param name="inputStream"></param>
    /// <param name="cancellationToken"></param>
    public static async Task<byte[]> EncryptAsync(
            string            key,
            byte[]            value,
            CancellationToken cancellationToken = default(CancellationToken))
    {
        using var m = new MemoryStream(value);
        return await EncryptAsync(key, m, cancellationToken);
    }


    /// <summary>複合化</summary>
    /// <param name="key"></param>
    /// <param name="inputStream"></param>
    /// <param name="cancellationToken"></param>
    public static async Task<byte[]> DecryptAsync(
            string            key,
            Stream            inputStream,
            CancellationToken cancellationToken = default(CancellationToken))
    {
        using var transform = CreateTransform(
                iv:        _iv,
                key:       StringToKey(key, Encoding.UTF8),
                isEncrypt: false);

        using var mem = new MemoryStream();
        using(var strmCrypto = new System.Security.Cryptography.CryptoStream(
                    stream:    inputStream,
                    transform: transform,
                    mode:      System.Security.Cryptography.CryptoStreamMode.Read))
        {
            await strmCrypto.CopyToAsync(mem, 81920, cancellationToken);
            await mem.FlushAsync(cancellationToken);
        }

        return mem.ToArray();
    }


    /// <summary>複合化</summary>
    /// <param name="key"></param>
    /// <param name="inputStream"></param>
    /// <param name="cancellationToken"></param>
    public static async Task<byte[]> DecryptAsync(
            string key,
            byte[] buffer,
            CancellationToken cancellationToken = default(CancellationToken))
    {
        using(var m = new MemoryStream(buffer))
        {
            return await DecryptAsync(
                    key:               key,
                    inputStream:       m,
                    cancellationToken: cancellationToken);
        }
    }


    /// <summary></summary>
    private static System.Security.Cryptography.ICryptoTransform CreateTransform(
            byte[] iv,
            byte[] key,
            bool   isEncrypt)
    {
        using(var aes = System.Security.Cryptography.Aes.Create())
        {
            aes.KeySize   = 256;
            aes.BlockSize = 128;
            aes.IV        = iv;
            aes.Key       = key;
            aes.Mode      = System.Security.Cryptography.CipherMode.CBC;
            aes.Padding   = System.Security.Cryptography.PaddingMode.PKCS7;

            return isEncrypt
                ? aes.CreateEncryptor(rgbKey: aes.Key, rgbIV: aes.IV)
                : aes.CreateDecryptor(rgbKey: aes.Key, rgbIV: aes.IV);
        }
    }


    /// <summary></summary>
    private static byte[] StringToKey(
            string   key,
            Encoding encoding,
            int      size = 32)
    {
        var bufKey = encoding.GetBytes(key);
        var retKey = new byte[size];

        if(size < bufKey.Length)
            throw new InvalidDataException();

        for(var i = 0; i < size; ++i)
        {
            retKey[i] = 0x0;
        }

        // 右揃え
        Array.Copy(
                sourceArray:      bufKey,
                sourceIndex:      0,
                destinationArray: retKey,
                destinationIndex: size - bufKey.Length,
                length:           bufKey.Length);

#if DEBUG
        Debug.WriteLine($"[Debug]  IV(Hex)={string.Join("-", _iv.Select(b => $"{b:X2}"))}");
        Debug.WriteLine($"[Debug] Key(Hex)={string.Join("-", retKey.Select(b => $"{b:X2}"))}");
#endif

        return retKey;
    }


#if DEBUG
    /// <summary>サンプルで使用するキーの作成</summary>
    public static Guid CreateKey() => Guid.NewGuid();
#endif
}
