// Copyright (c) All contributors. All rights reserved. Licensed under the MIT license.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LP;

public class LPInfo
{
    public bool IsConsole { get; set; }

    public string Directory { get; set; } = string.Empty;

    public LPConsoleOptions ConsoleOptions { get; set; } = new();
}
