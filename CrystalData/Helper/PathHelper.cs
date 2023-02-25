// Copyright (c) All contributors. All rights reserved. Licensed under the MIT license.

namespace CrystalData;

internal static class PathHelper
{
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
    /// <param name="directory">Directory path</param>
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
    /// <returns>Rooted directory path</returns>
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
