// Copyright (c) All contributors. All rights reserved. Licensed under the MIT license.

using LP;
using SimpleCommandLine;

namespace LPConsole.Sample;

[SimpleCommand("sample")]
public class SampleSubcommand : ISimpleCommandAsync
{
    public SampleSubcommand(Control control)
    {
        this.Control = control;
    }

    public async Task RunAsync(string[] args)
    {
        Logger.Default.Information(SampleHashed.SampleUnit.Command);
    }

    public Control Control { get; set; }
}
