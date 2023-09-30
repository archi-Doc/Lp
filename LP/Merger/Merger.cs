// Copyright (c) All contributors. All rights reserved. Licensed under the MIT license.

using CrystalData.Datum;
using LP.Crystal;
using LP.NetServices;
using LP.NetServices.T3CS;
using LP.T3CS;
using Microsoft.Extensions.DependencyInjection;

namespace LP;

public partial class Merger : UnitBase, IUnitPreparable, IUnitExecutable
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
                context.ServiceProvider.GetRequiredService<IBigCrystal<MergerData>>(),
                context.ServiceProvider.GetRequiredService<MergerInformation>());

            return this.merger;
        }

        private Merger? merger;
    }

    public Merger(UnitContext context, ILogger<Merger> logger, LPBase lpBase, IBigCrystal<MergerData> crystal, MergerInformation mergerInformation)
        : base(context)
    {
        this.logger = logger;
        this.lpBase = lpBase;
        this.crystal = crystal;

        this.Information = mergerInformation;
    }

    public void Prepare(UnitMessage.Prepare message)
    {
        this.Check();
        this.logger.TryGet()?.Log(this.Information.ToString());
    }

    public async Task RunAsync(UnitMessage.RunAsync message)
    {
    }

    public async Task TerminateAsync(UnitMessage.TerminateAsync message)
    {
        this.logger.TryGet()?.Log("Merger terminated");
    }

    [TinyhandObject]
    public partial record CreateCreditParams(
        [property: Key(0)] CreateCreditProof Proof);

    public MergerResult CreateCredit(LPServerContext context, CreateCreditParams param)
    {
        if (!param.Proof.ValidateAndVerify())
        {
            return MergerResult.InvalidToken;
        }

        // Get LpData
        var root = this.crystal.Data;
        var identifier = param.Proof.PublicKey.ToIdentifier();
        var credit = root.TryGetChild(identifier);
        if (credit != null)
        {
            return MergerResult.AlreadyExists;
        }

        // Set CreditBlock
        credit = root.GetOrCreateChild(identifier);
        using (var op = credit.Lock<BlockDatum>())
        {
            if (op.Datum == null)
            {
                return MergerResult.NoData;
            }

            credit.DataId = LpData.LpDataId.Credit;
            op.Datum.SetObject(new CreditBlock(param.Proof.PublicKey));
        }

        return MergerResult.Success;
    }

    public MergerInformation Information { get; private set; }

    private void Check()
    {
        this.logger.TryGet()?.Log("Merger started");

        this.crystal.Data.TryGetChild(default); // tempcode (preload)
        var numberOfCredits = this.crystal.Data.Count(LpData.LpDataId.Credit);
        this.logger.TryGet()?.Log($"Credits: {numberOfCredits}");

        // this.logger.TryGet(LogLevel.Fatal)?.Log("Merger fatal");
        // throw new PanicException();
    }

    private ILogger logger;
    private LPBase lpBase;
    private IBigCrystal<MergerData> crystal;
}
