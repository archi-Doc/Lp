// Copyright (c) All contributors. All rights reserved. Licensed under the MIT license.

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Arc.Crypto;
using LP;
using SimpleCommandLine;
using Tinyhand;

namespace LPConsole;

[SimpleCommand("temp", description: "Template command")]
public class TempCommand : ISimpleCommandAsync<TempOptions>
{
    public async Task Run(TempOptions options, string[] args)
    {
        Console.WriteLine("Template command:");
        Console.WriteLine($"{options.ToString()}");
    }
}

public record TempOptions
{
    [SimpleOption("name", description: "Name")]
    public string Name { get; init; } = string.Empty;
}
