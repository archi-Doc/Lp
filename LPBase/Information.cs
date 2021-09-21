﻿// Copyright (c) All contributors. All rights reserved. Licensed under the MIT license.

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LP;

public enum LPMode
{
    Merger,
    User,
}

public class Information
{
    public Information()
    {
    }

    public bool IsConsole { get; set; }

    public string RootDirectory { get; set; } = default!;

    public LPMode Mode { get; set; }

    public LPConsoleOptions ConsoleOptions { get; set; } = default!;

    public void Configure(LPConsoleOptions options, bool isConsole)
    {
        this.ConsoleOptions = options;
        this.IsConsole = isConsole;

        // Root directory
        if (Path.IsPathRooted(this.ConsoleOptions.Directory) &&
            File.GetAttributes(this.ConsoleOptions.Directory).HasFlag(FileAttributes.Directory))
        {
            this.RootDirectory = this.ConsoleOptions.Directory;
        }
        else
        {
            this.RootDirectory = Path.Combine(Directory.GetCurrentDirectory(), this.ConsoleOptions.Directory);
        }

        Directory.CreateDirectory(this.RootDirectory);

        // Mode
        LPMode mode;
        if (!Enum.TryParse<LPMode>(this.ConsoleOptions.Mode, out mode))
        {
            mode = LPMode.Merger;
        }

        this.Mode = mode;
    }

    public override string ToString()
    {
        return $"Mode: {this.Mode}, {this.ConsoleOptions.ToString()}";
    }
}
