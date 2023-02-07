// Copyright (c) All contributors. All rights reserved. Licensed under the MIT license.

namespace LP;

public class Merger : UnitBase, IUnitPreparable, IUnitExecutable
{
    public Merger(UnitContext context, ILogger<Merger> logger, LPBase lpBase)
        : base(context)
    {
        this.logger = logger;
        this.lpBase = lpBase;

        this.Information = TinyhandSerializer.Reconstruct<MergerInformation>();
    }

    public void Prepare(UnitMessage.Prepare message)
    {
        if (PathHelper.TryReadAndDeserialize<MergerInformation>(Path.Combine(this.lpBase.DataDirectory, MergerInformation.TinyhandName)) is { } information)
        {
            this.Information = information;
        }

        this.logger.TryGet()?.Log("Merger prepared");
    }

    public async Task RunAsync(UnitMessage.RunAsync message)
    {
        this.logger.TryGet()?.Log("Merger running");
    }

    public async Task TerminateAsync(UnitMessage.TerminateAsync message)
    {
        await PathHelper.TrySerializeAndWrite(this.Information, Path.Combine(this.lpBase.DataDirectory, MergerInformation.TinyhandName));
        this.logger.TryGet()?.Log("Merger terminated");
    }

    public MergerInformation Information { get; private set; }

    private ILogger logger;
    private LPBase lpBase;
}
