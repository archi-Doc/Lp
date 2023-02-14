// Copyright (c) All contributors. All rights reserved. Licensed under the MIT license.

namespace LP;

public class Merger : UnitBase, IUnitPreparable, IUnitExecutable, IUnitSerializable
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

    public async Task LoadAsync(UnitMessage.LoadAsync message)
    {
        if (PathHelper.TryReadAndDeserialize<MergerInformation>(Path.Combine(this.lpBase.DataDirectory, MergerInformation.TinyhandName)) is { } information)
        {
            this.Information = information;
        }
    }

    public async Task SaveAsync(UnitMessage.SaveAsync message)
    {
        await PathHelper.TrySerializeAndWrite(this.Information, Path.Combine(this.lpBase.DataDirectory, MergerInformation.TinyhandName));
    }

    public MergerInformation Information { get; private set; }

    private ILogger logger;
    private LPBase lpBase;
}
