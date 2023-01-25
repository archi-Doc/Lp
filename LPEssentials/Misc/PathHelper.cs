// Copyright (c) All contributors. All rights reserved. Licensed under the MIT license.

namespace LPEssentials;
public static class PathHelper
{
    public static string GetRootedDirectory(string directory)
    {
        if (Path.IsPathRooted(directory) &&
            File.GetAttributes(directory).HasFlag(FileAttributes.Directory))
        {
            return directory;
        }
        else
        {
            return Path.Combine(Directory.GetCurrentDirectory(), directory);
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
