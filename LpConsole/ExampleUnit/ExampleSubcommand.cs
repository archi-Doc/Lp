// Copyright (c) All contributors. All rights reserved. Licensed under the MIT license.

using SimpleCommandLine;

namespace LpConsole.Example;

[SimpleCommand("example")]
public class ExampleSubcommand : ISimpleCommandAsync
{
    public ExampleSubcommand(ILogger<ExampleSubcommand> logger, LpUnit lpUnit)
    {
        this.logger = logger;
        this.LpUnit = lpUnit;
    }

    public async Task RunAsync(string[] args)
    {
        this.logger.TryGet()?.Log(ExampleHashed.ExampleUnit.Command);
    }

    public LpUnit LpUnit { get; set; }

    private ILogger<ExampleSubcommand> logger;
}
