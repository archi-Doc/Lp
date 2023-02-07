// Copyright (c) All contributors. All rights reserved. Licensed under the MIT license.

using LP.NetServices.T3CS;

namespace LP;

public class Merger : UnitBase, IUnitPreparable, IUnitExecutable
{
    public record InformationResult(string Name);

    public Merger(UnitContext context, ILogger<Merger> logger)
        : base(context)
    {
        this.logger = logger;
    }

    public MergerService.InformationResult GetInformation()
    {
        return new("Merger");
    }

    public void Prepare(UnitMessage.Prepare message)
    {
        this.logger.TryGet()?.Log("Merger prepared");
    }

    public async Task RunAsync(UnitMessage.RunAsync message)
    {
        this.logger.TryGet()?.Log("Merger running");
    }

    public async Task TerminateAsync(UnitMessage.TerminateAsync message)
    {
        this.logger.TryGet()?.Log("Merger terminated");
    }

    private ILogger logger;
}
