namespace ouzel.IO;

using System;
using System.IO;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;


/// <summary></summary>
public static class Json
{
    private static readonly JsonSerializerOptions _options;

    static Json()
    {
        _options = new JsonSerializerOptions()
        {
            WriteIndented        = true,
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        };
    }


    /// <summary></summary>
    public static async Task<T> ReadAsync<T>(
            Stream            stream,
            CancellationToken cancellationToken = default(CancellationToken))
    {
        var o = await JsonSerializer.DeserializeAsync<T>(
                utf8Json: stream,
                options: _options,
                cancellationToken: cancellationToken);

        return o ?? throw new NullReferenceException();
    }


    /// <summary></summary>
    public static async Task<T> ReadAsync<T>(
            string path,
            CancellationToken cancellationToken = default(CancellationToken))
    {
        using var stream = File.Open(path, FileMode.Open, FileAccess.Read);

        return await ReadAsync<T>(
                stream:            stream,
                cancellationToken: cancellationToken);
    }


    /// <summary></summary>
    public static async Task WriteAsync<T>(
            Stream            stream,
            T                 value,
            CancellationToken cancellationToken = default(CancellationToken))
    {
        await JsonSerializer.SerializeAsync<T>(
                utf8Json: stream,
                value: value,
                cancellationToken: cancellationToken);
    }


    /// <summary></summary>
    public static async Task WriteAsync<T>(
            string            path,
            T                 value,
            CancellationToken cancellationToken = default(CancellationToken))
    {
        using var stream = File.Open(
                path,
                File.Exists(path) ? FileMode.Truncate : FileMode.CreateNew,
                FileAccess.Write);

        await WriteAsync<T>(
                stream:            stream,
                value:             value,
                cancellationToken: cancellationToken);
    }
}
