// Copyright (c) All contributors. All rights reserved. Licensed under the MIT license.

using Arc.Crypto;
using LP;
using SimpleCommandLine;
using Tinyhand;

namespace LP.Subcommands;

[SimpleCommand("new")]
public class TemplateSubcommandNew : ISimpleCommandAsync<TemplateOptionsNew>
{
    public TemplateSubcommandNew(Control control)
    {
        this.Control = control;
    }

    public async Task Run(TemplateOptionsNew option, string[] args)
    {
        Console.WriteLine($"Template New: {option.Name}");
    }

    public Control Control { get; set; }
}

public record TemplateOptionsNew
{
    [SimpleOption("name", Required = true)]
    public string Name { get; init; } = string.Empty;
}
