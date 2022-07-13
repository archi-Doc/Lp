// Copyright (c) All contributors. All rights reserved. Licensed under the MIT license.

#pragma warning disable SA1210 // Using directives should be ordered alphabetically by namespace
#pragma warning disable SA1208 // System using directives should be placed before other using directives
global using System;
global using System.IO;
global using Arc.Collections;
global using Arc.Crypto;
global using CrossChannel;
global using Tinyhand;

using LP.Options;
using System.Security.Cryptography;

namespace LP;

public enum LPMode
{
    Relay,
    Merger,
    User,
}

public class LPBase
{
    public const string DataDirectoryName = "Data";

    public LPBase()
    {
        TimeCorrection.Start();
    }

    public bool IsFirstRun { get; private set; }

    public bool IsConsole { get; private set; }

    public string RootDirectory { get; private set; } = default!;

    public string DataDirectory { get; private set; } = default!;

    public LPMode Mode { get; private set; }

    public string NodeName { get; private set; } = default!;

    public LPOptions LPOptions { get; private set; } = default!;

    // public string GetRootPath(string path, string defaultFilename) => this.GetPath(this.RootDirectory, path, defaultFilename);

    public string CombineDataPath(string path, string defaultFilename) => this.CombinePath(this.DataDirectory, path, defaultFilename);

    public string CombineDataPathAndPrepareDirectory(string path, string defaultFilename)
    {
        var file = this.CombineDataPath(path, defaultFilename);

        try
        {
            if (Path.GetDirectoryName(file) is { } directory)
            {
                Directory.CreateDirectory(directory);
            }
        }
        catch
        {
        }

        return file;
    }

    public void SetParameter(LPOptions options, bool isConsole, string defaultMode)
    {
        this.LPOptions = options;
        this.IsConsole = isConsole;

        // Root directory
        if (Path.IsPathRooted(this.LPOptions.Directory) &&
            File.GetAttributes(this.LPOptions.Directory).HasFlag(FileAttributes.Directory))
        {
            this.RootDirectory = this.LPOptions.Directory;
        }
        else
        {
            this.RootDirectory = Path.Combine(Directory.GetCurrentDirectory(), this.LPOptions.Directory);
        }

        Directory.CreateDirectory(this.RootDirectory);
        this.DataDirectory = Path.Combine(this.RootDirectory, DataDirectoryName);
        this.IsFirstRun = !Directory.Exists(this.DataDirectory);

        // Mode
        LPMode mode;
        if (!Enum.TryParse<LPMode>(this.LPOptions.Mode, true, out mode))
        {
            if (!Enum.TryParse<LPMode>(defaultMode, true, out mode))
            {
                mode = LPMode.Merger;
            }
        }

        this.Mode = mode;

        this.NodeName = this.LPOptions.NodeName;
        if (string.IsNullOrEmpty(this.NodeName))
        {
            this.NodeName = System.Environment.OSVersion.ToString();
        }
    }

    public override string ToString()
    {
        return $"Mode: {this.Mode}, {this.LPOptions.ToString()}";
    }

    private string CombinePath(string directory, string path, string defaultFilename)
    {
        if (Path.IsPathRooted(path))
        {
            return path;
        }
        else
        {
            return Path.Combine(directory, string.IsNullOrEmpty(path) ? defaultFilename : path);
        }
    }
}
