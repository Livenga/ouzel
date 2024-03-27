namespace ouzel.Test;

using Xunit;
using Xunit.Abstractions;
using System;
using System.Text;
using System.Threading;
using System.Threading.Tasks;


/// <summary></summary>
public class AesTest
{
    private readonly ITestOutputHelper _outputHelper;


    /// <summary></summary>
    public AesTest(ITestOutputHelper outputHelper)
    {
        _outputHelper = outputHelper;
    }


    /// <summary></summary>
    [Theory]
    [InlineData("ouzel.xxx", "Hello, World!")]
    public async Task EncryptAsyncTest(string key, string value)
    {
        var encryptedValue = await ouzel.Aes.EncryptAsync(
                key:               key,
                value:             Encoding.UTF8.GetBytes(value),
                cancellationToken: CancellationToken.None);

        _outputHelper.WriteLine($"{encryptedValue.Length} {string.Join("-", encryptedValue.Select(b => $"{b:X2}"))}");
        var b64 = System.Convert.ToBase64String(encryptedValue);

        _outputHelper.WriteLine(b64);
    }


    /// <summary></summary>
    [Theory]
    [InlineData("ouzel.xxx", @"..\..\..\AesTest.cs")]
    public async Task EncryptFromPathAsync(string key, string path)
    {
        using var stream = System.IO.File.Open(path, System.IO.FileMode.Open, System.IO.FileAccess.Read);
        var buffer = await ouzel.Aes.EncryptAsync(
                key:               key,
                inputStream:       stream,
                cancellationToken: CancellationToken.None);

        var s = Convert.ToBase64String(buffer);

        var rowCount = (s.Length % 64) == 0
            ? s.Length / 64
            : (s.Length / 64) + 1;

        for(var i = 0; i < rowCount; ++i)
        {
            var nextOffset = (1 + i) * 64;

            var split = (s.Length < nextOffset)
                ? s.Substring(i * 64, s.Length - (i * 64))
                : s.Substring(i * 64, 64);

            _outputHelper.WriteLine($"{1 + i}\t{split}");
        }
    }


    /// <summary></summary>
    [Theory]
    [InlineData("ouzel.xxx", "25yvjOzTRPpFOjb/GxCg1g==")]
    public async Task DecryptAsyncTest(string key, string encryptedValue)
    {
        var buffer = await ouzel.Aes.DecryptAsync(
                key:               key,
                buffer:            Convert.FromBase64String(s: encryptedValue),
                cancellationToken: CancellationToken.None);

        _outputHelper.WriteLine(Encoding.UTF8.GetString(buffer));
    }
}
