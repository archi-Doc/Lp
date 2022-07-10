﻿// Copyright (c) All contributors. All rights reserved. Licensed under the MIT license.

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
    public const string KeyVaultFilename = "KeyVault.tinyhand";

    public LPBase()
    {
        TimeCorrection.Start();
    }

    public bool IsConsole { get; private set; }

    public string RootDirectory { get; private set; } = default!;

    public string DataDirectory { get; private set; } = default!;

    public LPMode Mode { get; private set; }

    public string NodeName { get; private set; } = default!;

    public LPOptions LPOptions { get; private set; } = default!;

    public string KeyVaultPath { get; private set; } = default!;

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
        this.DataDirectory = Path.Combine(this.RootDirectory, "LP");

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

        // KeyVault path
        if (Path.IsPathRooted(this.LPOptions.KeyVault))
        {
            this.KeyVaultPath = this.LPOptions.KeyVault;
        }
        else
        {
            this.KeyVaultPath = Path.Combine(this.DataDirectory, string.IsNullOrEmpty(this.LPOptions.KeyVault) ? KeyVaultFilename : this.LPOptions.KeyVault);
        }
    }

    public override string ToString()
    {
        return $"Mode: {this.Mode}, {this.LPOptions.ToString()}";
    }
}
