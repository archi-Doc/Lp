// Copyright (c) All contributors. All rights reserved. Licensed under the MIT license.

#pragma warning disable SA1210 // Using directives should be ordered alphabetically by namespace
#pragma warning disable SA1208 // System using directives should be placed before other using directives
global using System;
global using System.IO;
global using Arc.Collections;
global using Arc.Crypto;
global using Arc.Unit;
global using CrossChannel;
global using Tinyhand;

using LP.Data;
using System.Security.Cryptography;
using System.Collections.Generic;

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
        this.Settings = TinyhandSerializer.Reconstruct<LPSettings>();
    }

    public bool IsFirstRun { get; private set; }

    public bool IsConsole { get; private set; }

    public string RootDirectory { get; private set; } = default!;

    public string DataDirectory { get; private set; } = default!;

    public LPMode Mode { get; private set; }

    public string NodeName { get; private set; } = default!;

    public LPOptions Options { get; private set; } = default!;

    public LPSettings Settings { get; set; }

    public bool ConsoleMode { get; set; } = false;

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

    public void Initialize(LPOptions options, bool isConsole, string defaultMode)
    {
        this.Options = options;
        this.IsConsole = isConsole;

        // Root directory
        if (Path.IsPathRooted(this.Options.Directory) &&
            File.GetAttributes(this.Options.Directory).HasFlag(FileAttributes.Directory))
        {
            this.RootDirectory = this.Options.Directory;
        }
        else
        {
            this.RootDirectory = Path.Combine(Directory.GetCurrentDirectory(), this.Options.Directory);
        }

        Directory.CreateDirectory(this.RootDirectory);
        this.DataDirectory = Path.Combine(this.RootDirectory, DataDirectoryName);
        this.IsFirstRun = !Directory.Exists(this.DataDirectory);

        // Mode
        LPMode mode;
        if (!Enum.TryParse<LPMode>(this.Options.Mode, true, out mode))
        {
            if (!Enum.TryParse<LPMode>(defaultMode, true, out mode))
            {
                mode = LPMode.Merger;
            }
        }

        this.Mode = mode;

        this.NodeName = this.Options.NodeName;
        if (string.IsNullOrEmpty(this.NodeName))
        {
            this.NodeName = System.Environment.OSVersion.ToString();
        }
    }

    public override string ToString()
    {
        return $"Mode: {this.Mode}, {this.Options.ToString()}";
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
