// Copyright (c) All contributors. All rights reserved. Licensed under the MIT license.

namespace LP;

public static class PathHelper
{
    public static T? TryReadAndDeserialize<T>(string path)
        where T : ITinyhandSerialize<T>
    {
        byte[] data;
        try
        {
            data = File.ReadAllBytes(path);
        }
        catch
        {
            return default;
        }

        try
        {
            return TinyhandSerializer.DeserializeObjectFromUtf8<T>(data);
        }
        catch
        {
            return default;
        }
    }

    public static async Task<bool> TrySerializeAndWrite<T>(T obj, string path)
        where T : ITinyhandSerialize<T>
    {
        try
        {
            var bytes = TinyhandSerializer.SerializeToUtf8(obj);
            await File.WriteAllBytesAsync(path, bytes);
            return true;
        }
        catch
        {
            return false;
        }
    }

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

    public static string GetRootedDirectory(string root, string directory)
    {
        if (Path.IsPathRooted(directory) &&
            File.GetAttributes(directory).HasFlag(FileAttributes.Directory))
        {
            return directory;
        }
        else
        {
            return Path.Combine(root, directory);
        }
    }

    public static string GetRootedFile(string root, string file)
    {
        if (Path.IsPathRooted(file))
        {
            return file;
        }
        else
        {
            return Path.Combine(root, file);
        }
    }
}
