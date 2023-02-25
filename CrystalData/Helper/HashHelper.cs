// Copyright (c) All contributors. All rights reserved. Licensed under the MIT license.

namespace CrystalData;

internal static class HashHelper
{
    /// <summary>
    /// Checks the hash value (first 8 bytes) and returns the data (9-) if the hash value is correct.
    /// </summary>
    /// <param name="source">Source.</param>
    /// <param name="data">Extracted data.</param>
    /// <returns><see langword="true"/>; Success.</returns>
    public static bool CheckFarmHashAndGetData(ReadOnlyMemory<byte> source, out ReadOnlyMemory<byte> data)
    {
        data = default;
        if (source.Length < 8)
        {
            return false;
        }

        var s = source.Slice(8);
        if (Arc.Crypto.FarmHash.Hash64(s.Span) != BitConverter.ToUInt64(source.Span))
        {
            return false;
        }

        data = s;
        return true;
    }

    /// <summary>
    /// Checks the hash value (first 8 bytes) and returns the data (9-) if the hash value is correct.
    /// </summary>
    /// <param name="source">Source.</param>
    /// <param name="data">Extracted data.</param>
    /// <returns><see langword="true"/>; Success.</returns>
    public static bool CheckFarmHashAndGetData(ReadOnlySpan<byte> source, out ReadOnlySpan<byte> data)
    {
        data = default;
        if (source.Length < 8)
        {
            return false;
        }

        var s = source.Slice(8);
        if (Arc.Crypto.FarmHash.Hash64(s) != BitConverter.ToUInt64(source))
        {
            return false;
        }

        data = s;
        return true;
    }

    /// <summary>
    /// Calculates a hash value of the data and save the 8-byte hash value and data to a file.
    /// </summary>
    /// <param name="data">Data.</param>
    /// <param name="path">Output path.</param>
    /// <param name="backupPath">Backup path.</param>
    /// <returns><see langword="true"/>; Success.</returns>
    public static async Task<bool> GetFarmHashAndSaveAsync(ReadOnlyMemory<byte> data, string path, string? backupPath)
    {
        var hash = new byte[8];
        var result = false;
        BitConverter.TryWriteBytes(hash, Arc.Crypto.FarmHash.Hash64(data.Span));
        try
        {
            using (var handle = File.OpenHandle(path, mode: FileMode.Create, access: FileAccess.ReadWrite))
            {
                await RandomAccess.WriteAsync(handle, hash, 0);
                await RandomAccess.WriteAsync(handle, data, hash.Length);
                result = true;
            }
        }
        catch
        {
            return false;
        }

        if (backupPath != null)
        {
            try
            {
                using (var handle = File.OpenHandle(backupPath, mode: FileMode.Create, access: FileAccess.ReadWrite))
                {
                    await RandomAccess.WriteAsync(handle, hash, 0).ConfigureAwait(false);
                    await RandomAccess.WriteAsync(handle, data, hash.Length).ConfigureAwait(false);
                }
            }
            catch
            {
            }
        }

        return result;
    }
}
