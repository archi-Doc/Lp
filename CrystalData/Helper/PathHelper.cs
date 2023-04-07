// Copyright (c) All contributors. All rights reserved. Licensed under the MIT license.

using System.Runtime.CompilerServices;

namespace CrystalData;

public static class PathHelper
{
    public const string CheckExtension = "check";
    public const int CheckLength = 16;

    public static async Task<(CrystalMemoryOwnerResult Result, ulong Location)> LoadData(IFiler? filer)
    {
        if (filer == null)
        {
            return (new(CrystalResult.NoFiler), 0);
        }

        var result = await filer.ReadAsync(0, -1).ConfigureAwait(false);
        if (result.IsFailure)
        {
            return (new(result.Result), 0);
        }

        // Load check file (hash/location)
        var checkFiler = filer.CloneWithExtension(CheckExtension);
        var checkResult = await checkFiler.ReadAsync(0, -1).ConfigureAwait(false);
        if (checkResult.IsFailure || checkResult.Data.Memory.Length != CheckLength)
        {// No check file
            return (result, 0);
        }

        ulong hash;
        ulong location;
        try
        {
            hash = BitConverter.ToUInt64(checkResult.Data.Memory.Span);
            location = BitConverter.ToUInt64(checkResult.Data.Memory.Span.Slice(sizeof(ulong)));
        }
        catch
        {
            return (result, 0);
        }

        if (FarmHash.Hash64(result.Data.Memory.Span) != hash)
        {// Hash does not match
            return (new(CrystalResult.CorruptedData), 0);
        }

        return (result, location);
    }

    public static Task<CrystalResult> SaveData<T>(T? obj, IFiler? filer, ulong position)
        where T : ITinyhandSerialize<T>
    {
        if (obj == null)
        {
            return Task.FromResult(CrystalResult.NoData);
        }
        else if (filer == null)
        {
            return Task.FromResult(CrystalResult.NoFiler);
        }

        var data = TinyhandSerializer.SerializeObject(obj);
        return SaveData(data, filer, position);
    }

    public static async Task<CrystalResult> SaveData(byte[]? data, IFiler? filer, ulong position)
    {
        if (data == null)
        {
            return CrystalResult.NoData;
        }
        else if (filer == null)
        {
            return CrystalResult.NoFiler;
        }

        var result = await filer.WriteAsync(0, new(data));
        if (result != CrystalResult.Success)
        {
            return result;
        }

        var hashAndPosition = new byte[CheckLength];
        var hash = FarmHash.Hash64(data.AsSpan());
        BitConverter.TryWriteBytes(hashAndPosition.AsSpan(), hash);
        BitConverter.TryWriteBytes(hashAndPosition.AsSpan(sizeof(ulong)), position);

        var chckFiler = filer.CloneWithExtension(CheckExtension);
        result = await chckFiler.WriteAsync(0, new(hashAndPosition));
        return result;
    }

    /// <summary>
    /// Deletes the specified file (no exception will be thrown).
    /// </summary>
    /// <param name="file">File path.</param>
    /// <returns><see langword="true"/>; File is successfully deleted.</returns>
    public static bool TryDeleteFile(string file)
    {
        try
        {
            File.Delete(file);
            return true;
        }
        catch
        {
            return false;
        }
    }

    /// <summary>
    /// Deletes the specified directory recursively (no exception will be thrown).
    /// </summary>
    /// <param name="directory">Directory path.</param>
    /// <returns><see langword="true"/>; Directory is successfully deleted.</returns>
    public static bool TryDeleteDirectory(string directory)
    {
        try
        {
            Directory.Delete(directory, true);
            return true;
        }
        catch
        {
            return false;
        }
    }

    /// <summary>
    /// Gets the rooted directory path.<br/>
    /// If the directory is rooted, it is returned as is; if it is not the root path, root and directory path are combined.
    /// </summary>
    /// <param name="root">Root path.</param>
    /// <param name="directory">Directory path.</param>
    /// <returns>Rooted directory path.</returns>
    public static string GetRootedDirectory(string root, string directory)
    {
        try
        {
            if (Path.IsPathRooted(directory))
            {// File.GetAttributes(directory).HasFlag(FileAttributes.Directory)
                return directory;
            }
            else
            {
                return Path.Combine(root, directory);
            }
        }
        catch
        {
            return Path.Combine(root, directory);
        }
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static string GetRootedFile(string rootDirectory, string file)
    {
        if (Path.IsPathRooted(file))
        {
            return file;
        }
        else
        {
            return Path.Combine(rootDirectory, file);
        }
    }
}
