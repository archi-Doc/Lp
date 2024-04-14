// Copyright (c) All contributors. All rights reserved. Licensed under the MIT license.

using LP.T3CS;
using Microsoft.Extensions.DependencyInjection;
using Netsphere;
using Netsphere.Crypto;

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
                context.ServiceProvider.GetRequiredService<ICrystal<CreditData.GoshujinClass>>(),
                context.ServiceProvider.GetRequiredService<MergerInformation>());

            return this.merger;
        }

        private Merger? merger;
    }

    public Merger(UnitContext context, ILogger<Merger> logger, LPBase lpBase, ICrystal<CreditData.GoshujinClass> crystal, MergerInformation mergerInformation)
        : base(context)
    {
        this.logger = logger;
        this.lpBase = lpBase;
        this.crystal = crystal;
        this.Information = mergerInformation;
    }

    void IUnitPreparable.Prepare(UnitMessage.Prepare message)
    {
        this.logger.TryGet()?.Log(this.Information.ToString());

        if (this.Information.MergerType == MergerInformation.Type.Single)
        {// Single credit
            this.Information.SingleCredit = Credit.Default;
        }
        else
        {// Multi credit
        }

        if (this.Information.MergerPrivateKey is null)
        {
            this.logger.TryGet(LogLevel.Fatal)?.Log("No merger private key");
            Console.WriteLine($"Created signature key: {SignaturePrivateKey.Create().UnsafeToString()}");
            return;//
        }

        this.logger.TryGet()?.Log($"Credits: {this.crystal.Data.Count}");
    }

    async Task IUnitExecutable.StartAsync(UnitMessage.StartAsync message, CancellationToken cancellationToken)
    {
    }

    void IUnitExecutable.Stop(UnitMessage.Stop message)
    {
    }

    async Task IUnitExecutable.TerminateAsync(UnitMessage.TerminateAsync message, CancellationToken cancellationToken)
    {
        this.logger.TryGet()?.Log("Merger terminated");
    }

    [TinyhandObject]
    public partial record CreateCreditParams(
        [property: Key(0)] CreateCreditProof Proof);

    public async NetTask<T3CSResultAndValue<Credit>> CreateCredit(CreateCreditParams param)
    {
        if (!param.Proof.ValidateAndVerify())
        {
            return new(T3CSResult.InvalidProof);
        }

        // Get LpData
        var g = this.crystal.Data;
        // var identifier = param.Proof.PublicKey.ToIdentifier();

        CreditData? creditData;
        using (var w = g.TryLock(Credit.Default, ValueLink.TryLockMode.GetOrCreate))
        {
            if (w is null)
            {
                return new(T3CSResult.NoData);
            }

            creditData = w.Commit();
        }

        if (creditData is null)
        {
            return new(T3CSResult.NoData);
        }

        var mergerPublicKey = SignaturePrivateKey.Create().ToPublicKey();
        var credit = new Credit(param.Proof.PublicKey, SignaturePrivateKey.Create().ToPublicKey(), [mergerPublicKey,]);

        var borrowers = await creditData.Borrowers.Get();
        using (var w2 = borrowers.TryLock(param.Proof.PublicKey, ValueLink.TryLockMode.Create))
        {
            if (w2 is null)
            {
                return new(T3CSResult.AlreadyExists, credit);
            }

            w2.Commit();
            return new(credit);
        }
    }

    public MergerInformation Information { get; private set; }

    private ILogger logger;
    private LPBase lpBase;
    private ICrystal<CreditData.GoshujinClass> crystal;
}
