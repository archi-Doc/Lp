// Copyright (c) All contributors. All rights reserved. Licensed under the MIT license.

using Arc.Crypto;
using LP;
using SimpleCommandLine;
using ZenItz;

namespace LP.Subcommands;

[SimpleCommand("template")]
public class ZenSubcommandTemplate : ISimpleCommandAsync<ZenOptionsTemplate>
{
    public ZenSubcommandTemplate(ZenControl control)
    {
        this.Control = control;
    }

    public async Task Run(ZenOptionsTemplate option, string[] args)
    {
        Console.WriteLine($"Template: {option.Name}");
    }

    public ZenControl Control { get; set; }
}

public record ZenOptionsTemplate
{
    [SimpleOption("name", Required = false)]
    public string Name { get; init; } = string.Empty;
}
