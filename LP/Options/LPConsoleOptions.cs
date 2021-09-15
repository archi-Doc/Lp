// Copyright (c) All contributors. All rights reserved. Licensed under the MIT license.

using System;
using SimpleCommandLine;

namespace LP;

public class LPConsoleOptions
{
    [SimpleOption("number", "n")]
    public int Number { get; set; } = 2000;
}
