// Copyright (c) All contributors. All rights reserved. Licensed under the MIT license.

using SimpleCommandLine;

namespace LpConsole.Example;

[SimpleCommand("example")]
public class ExampleSubcommand : ISimpleCommandAsync
{
    public ExampleSubcommand(ILogger<ExampleSubcommand> logger, Control control)
    {
        this.logger = logger;
        this.Control = control;
    }

    public async Task RunAsync(string[] args)
    {
        this.logger.TryGet()?.Log(ExampleHashed.ExampleUnit.Command);
    }

    public Control Control { get; set; }

    private ILogger<ExampleSubcommand> logger;
}
