// Copyright (c) All contributors. All rights reserved. Licensed under the MIT license.

using CrystalData;
using LP.Crystal;
using LP.NetServices.T3CS;
using LP.T3CS;
using Microsoft.Extensions.DependencyInjection;

namespace LP;

public class Merger : UnitBase, IUnitPreparable, IUnitExecutable, IUnitSerializable
{
    public class Provider
    {
        public Merger? TryGet()
            => this.merger;

        public Merger GetOrException()
            => this.merger ?? throw new InvalidOperationException();

        internal Merger Create(UnitContext context)
        {
            this.merger = new(
                context,
                context.ServiceProvider.GetRequiredService<ILogger<Merger>>(),
                context.ServiceProvider.GetRequiredService<LPBase>(),
                context.ServiceProvider.GetRequiredService<MergerCrystal>());

            return this.merger;
        }

        private Merger? merger;
    }

    public Merger(UnitContext context, ILogger<Merger> logger, LPBase lpBase, MergerCrystal crystal)
        : base(context)
    {
        this.logger = logger;
        this.lpBase = lpBase;
        this.crystal = crystal;

        this.Information = TinyhandSerializer.Reconstruct<MergerInformation>();
    }

    public void Prepare(UnitMessage.Prepare message)
    {
        // this.logger.TryGet()?.Log("Merger prepared");
    }

    public async Task RunAsync(UnitMessage.RunAsync message)
    {
        this.logger.TryGet()?.Log("Merger running");
        this.logger.TryGet()?.Log(this.Information.ToString());
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

        await this.Check();
    }

    public async Task SaveAsync(UnitMessage.SaveAsync message)
    {
        await PathHelper.TrySerializeAndWrite(this.Information, Path.Combine(this.lpBase.DataDirectory, MergerInformation.TinyhandName));
    }

    public MergerResult CreateCredit(MergerService.CreateCreditParams param)
    {
        var root = this.crystal.Root;
        var identifier = param.publicKey.ToIdentifier();
        var credit = root.TryGetChild(identifier);
        if (credit != null)
        {
            return MergerResult.AlreadyExists;
        }

        credit = root.GetOrCreateChild(identifier);
        using (var op = credit.Lock<BlockDatum>())
        {
            if (op.Datum == null)
            {
                return MergerResult.Success;
            }

            op.Datum.SetObject(new CreditBlock());
        }

        return MergerResult.Success;
    }

    public MergerInformation Information { get; private set; }

    private async Task Check()
    {
        this.logger.TryGet()?.Log("Merger checking");

        var numberOfCredits = this.crystal.Root.Count(LpData.LpDataId.Credit);
        this.logger.TryGet()?.Log($"Credits: {numberOfCredits}");

        // this.logger.TryGet(LogLevel.Fatal)?.Log("Merger fatal");
        // throw new PanicException();
    }

    private ILogger logger;
    private LPBase lpBase;
    private MergerCrystal crystal;
    // private LpData root;
}
