// Copyright (c) All contributors. All rights reserved. Licensed under the MIT license.

using System.Runtime.CompilerServices;

namespace CrystalData;

public static class PathHelper
{
    public const string CheckExtension = "check";
    public const int CheckLength = 16;
    public const char Slash = '/';
    public const char Backslash = '\\';
    public const char Colon = ':';
    public const string SlashString = "/";
    public const string BackslashString = "\\";
    public const string ColonString = ":";

    public static string CombineWithBackslash(string path1, string path2)
        => CombineWith(Backslash, path1, path2);

    public static string CombineWithSlash(string path1, string path2)
        => CombineWith(Slash, path1, path2);

    public static string CombineWith(char separator, string path1, string path2)
    {
        var omitLast1 = false;
        if (path1.Length > 0)
        {
            var c1 = path1[path1.Length - 1];
            if (IsSeparator(c1))
            {
                omitLast1 = true;
            }
        }

        var omitFirst2 = false;
        if (path2.Length > 0)
        {
            var c2 = path2[01];
            if (IsSeparator(c2))
            {
                omitFirst2 = true;
            }
        }

        if (omitLast1)
        {
            if (omitFirst2)
            {// path1/ + /path2
                return path1 + path2.Substring(1);
            }
            else
            {// path1/ + path2
                return path1 + path2;
            }
        }
        else
        {
            if (omitFirst2)
            {// path1 + /path2
                return path1 + path2;
            }
            else
            {// path1 + path2
                return path1 + separator + path2;
            }
        }
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static bool IsSeparator(char c)
        => c == Slash || c == Backslash || c == Colon;

    public static async Task<(CrystalMemoryOwnerResult Result, ulong Waypoint)> LoadData(IFiler? filer)
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

    public static Task<CrystalResult> SaveData<T>(T? obj, IFiler? filer, ulong waypoint)
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
        return SaveData(data, filer, waypoint);
    }

    public static async Task<CrystalResult> SaveData(byte[]? data, IFiler? filer, ulong waypoint)
    {
        if (data == null)
        {
            return CrystalResult.NoData;
        }
        else if (filer == null)
        {
            return CrystalResult.NoFiler;
        }

        var result = await filer.WriteAsync(0, new(data)).ConfigureAwait(false);
        if (result != CrystalResult.Success)
        {
            return result;
        }

        var hashAndWaypoint = new byte[CheckLength];
        var hash = FarmHash.Hash64(data.AsSpan());
        BitConverter.TryWriteBytes(hashAndWaypoint.AsSpan(), hash);
        BitConverter.TryWriteBytes(hashAndWaypoint.AsSpan(sizeof(ulong)), waypoint);

        var chckFiler = filer.CloneWithExtension(CheckExtension);
        result = await chckFiler.WriteAsync(0, new(hashAndWaypoint)).ConfigureAwait(false);
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
