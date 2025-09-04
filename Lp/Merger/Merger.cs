// Copyright (c) All contributors. All rights reserved. Licensed under the MIT license.

using System.Diagnostics.CodeAnalysis;
using Arc.Collections;
using Lp.Logging;
using Lp.T3cs;
using Netsphere.Crypto;
using Netsphere.Stats;

#pragma warning disable SA1401

namespace Lp;

public partial class Merger : MergerBase, IUnitPreparable, IUnitExecutable
{
    private const string NameSuffix = "_M";

    #region FieldAndProperty

    [MemberNotNullWhen(true, nameof(Configuration))]
    [MemberNotNullWhen(true, nameof(creditDataCrystal))]
    [MemberNotNullWhen(true, nameof(creditData))]
    public override bool Initialized { get; protected set; }

    public MergerConfiguration? Configuration { get; protected set; }

    public MergerState State { get; protected set; } = new();

    public override string GetName() => this.Configuration?.Name ?? string.Empty;

    public override CredentialState GetState() => this.State;

    protected ICrystal<FullCredit.GoshujinClass>? creditDataCrystal;
    protected FullCredit.GoshujinClass? creditData;

    #endregion

    public Merger(UnitContext context, UnitLogger unitLogger, NetBase netBase, LpBase lpBase, NetStats netStats, DomainControl domainControl)
        : base(context, unitLogger, netBase, lpBase, netStats, domainControl)
    {
    }

    public virtual void Initialize(Crystalizer crystalizer, SeedKey seedKey)
    {
        this.Configuration = crystalizer.CreateCrystal<MergerConfiguration>(new()
        {
            NumberOfFileHistories = 0, // 3
            FileConfiguration = new GlobalFileConfiguration(MergerConfiguration.MergerFilename),
            RequiredForLoading = true,
        }).Data;

        this.creditDataCrystal = crystalizer.CreateCrystal<FullCredit.GoshujinClass>(new()
        {
            SaveFormat = SaveFormat.Binary,
            NumberOfFileHistories = 3,
            FileConfiguration = new GlobalFileConfiguration("Merger/Credits"),
            StorageConfiguration = new SimpleStorageConfiguration(
                new GlobalDirectoryConfiguration("Merger/Storage")),
        });

        if (string.IsNullOrEmpty(this.Configuration.Name))
        {
            this.Configuration.Name = $"{this.netBase.NetOptions.NodeName}{NameSuffix}";
        }

        this.creditData = this.creditDataCrystal.Data;
        this.seedKey = seedKey;
        this.PublicKey = this.seedKey.GetSignaturePublicKey();

        this.Initialized = true;
    }

    void IUnitPreparable.Prepare(UnitMessage.Prepare message)
    {
        if (!this.Initialized)
        {
            return;
        }

        // this.logger.TryGet()?.Log(this.Information.ToString());

        if (this.Configuration.MergerType == MergerConfiguration.Type.Single)
        {// Single credit
            this.Configuration.SingleCredit = Credit.Default;
        }
        else
        {// Multi credit
        }

        this.logger.TryGet()?.Log($"{this.Configuration.Name}: {this.PublicKey.ToString()}, Credits: {this.creditDataCrystal.Data.Count}/{this.Configuration.MaxCredits}");
    }

    async Task IUnitExecutable.StartAsync(UnitMessage.StartAsync message, CancellationToken cancellationToken)
    {
    }

    void IUnitExecutable.Stop(UnitMessage.Stop message)
    {
    }

    async Task IUnitExecutable.TerminateAsync(UnitMessage.TerminateAsync message, CancellationToken cancellationToken)
    {
        this.logger.TryGet()?.Log("Terminated");
    }

    [TinyhandObject]
    public partial record CreateCreditParams(
        [property: Key(0)] CreateCreditProof Proof);

    public async Task<(FullCredit? FullCredit, bool Created)> GetOrCreateCredit(CreditIdentity creditIdentity)
    {
        if (!this.Initialized)
        {
            return default;
        }

        var credit = creditIdentity.ToCredit();
        var fullCredit = this.creditData.TryGet(credit);
        if (fullCredit is not null)
        {
            return new(fullCredit, false);
        }

        using (var w = this.creditData.TryLock(credit, AcquisitionMode.GetOrCreate))
        {
            if (w is null)
            {
                return default;
            }

            fullCredit = w.Commit();
        }

        return new(fullCredit, false);
    }

    public async NetTask<T3csResultAndValue<Credit>> CreateCredit(CreateCreditParams param)
    {
        if (!this.Initialized)
        {
            return new(T3csResult.NoData);
        }
        else if (!param.Proof.ValidateAndVerify())
        {
            return new(T3csResult.InvalidProof);
        }

        // Get LpData
        // var identifier = param.Proof.PublicKey.ToIdentifier();

        FullCredit? creditData;
        using (var w = this.creditData.TryLock(Credit.Default, AcquisitionMode.GetOrCreate))
        {
            if (w is null)
            {
                return new(T3csResult.NoData);
            }

            creditData = w.Commit();
        }

        if (creditData is null)
        {
            return new(T3csResult.NoData);
        }

        var mergerPublicKey = SeedKey.New(KeyOrientation.Signature).GetSignaturePublicKey();
        var creditIdentity = new CreditIdentity(default, param.Proof.PublicKey, [mergerPublicKey]);
        if (!Credit.TryCreate(creditIdentity, out var credit))
        {
            return new(T3csResult.UnknownError);
        }

        var borrowers = await creditData.Owners.Get();
        using (var w2 = borrowers.TryLock(param.Proof.PublicKey, AcquisitionMode.Create))
        {
            if (w2 is null)
            {
                return new(T3csResult.AlreadyExists, credit);
            }

            w2.Commit();
            return new(credit);
        }
    }

    public SeedKey SeedKey => this.seedKey;

    public FullCredit? GetCredit(Credit credit)
    {
        if (!this.Initialized)
        {
            return default;
        }

        return this.creditData.TryGet(credit);
    }

    public async ValueTask<OwnerData?> FindOwnerData(OwnerToken token)
    {
        if (!this.Initialized || token.Credit is null)
        {
            return null;
        }

        if (this.creditData.TryGet(token.Credit) is not { } creditData)
        {
            return null;
        }

        var owners = await creditData.Owners.Get().ConfigureAwait(false);
        return owners.TryGet(token.PublicKey);
    }
}
