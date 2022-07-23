// Copyright (c) All contributors. All rights reserved. Licensed under the MIT license.

using LP;
using SimpleCommandLine;

namespace LPConsole.Sample;

[SimpleCommand("sample")]
public class SampleSubcommand : ISimpleCommandAsync
{
    public SampleSubcommand(ILogger<SampleSubcommand> logger, Control control)
    {
        this.logger = logger;
        this.Control = control;
    }

    public async Task RunAsync(string[] args)
    {
        this.logger.TryGet()?.Log(SampleHashed.SampleUnit.Command);
    }

    public Control Control { get; set; }

    private ILogger<SampleSubcommand> logger;
}
