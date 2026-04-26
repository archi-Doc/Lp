// Copyright (c) All contributors. All rights reserved. Licensed under the MIT license.

using SimpleCommandLine;

namespace LpConsole.Example;

[SimpleCommand("example")]
public class ExampleSubcommand : ISimpleCommand
{
    private readonly LpUnit lpUnit;
    private readonly ILogger logger;

    public ExampleSubcommand(ILogger<ExampleSubcommand> logger, LpUnit lpUnit)
    {
        this.lpUnit = lpUnit;
        this.logger = logger;
    }

    public async Task Execute(string[] args, CancellationToken cancellationToken)
    {
        this.logger.GetWriter()?.Write(ExampleHashed.ExampleUnit.Command);
    }
}
