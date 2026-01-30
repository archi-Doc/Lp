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
    [MemberNotNullWhen(true, nameof(equityCreditCrystal))]
    [MemberNotNullWhen(true, nameof(equityCreditPoints))]
    public override bool Initialized { get; protected set; }

    public MergerConfiguration? Configuration { get; protected set; }

    public MergerState State { get; protected set; } = new();

    public override string GetName() => this.Configuration?.Name ?? string.Empty;

    public override CredentialState GetState() => this.State;

    protected ICrystal<FullCredit.GoshujinClass>? creditDataCrystal;
    protected ICrystal<EquityCreditPoint.GoshujinClass>? equityCreditCrystal;
    protected FullCredit.GoshujinClass? creditData;
    protected EquityCreditPoint.GoshujinClass? equityCreditPoints;

    #endregion

    public Merger(UnitContext context, UnitLogger unitLogger, NetBase netBase, LpBase lpBase, NetStats netStats, DomainControl domainControl)
        : base(context, unitLogger, netBase, lpBase, netStats, domainControl)
    {
    }

    public virtual void Initialize(CrystalControl crystalControl, SeedKey seedKey)
    {
        var mergerStorage = new SimpleStorageConfiguration(new GlobalDirectoryConfiguration("Merger/Storage"));

        this.Configuration = crystalControl.CreateCrystal<MergerConfiguration>(new()
        {
            NumberOfFileHistories = 0, // 3
            FileConfiguration = new GlobalFileConfiguration(MergerConfiguration.MergerFilename),
            RequiredForLoading = true,
        }).Data;

        this.creditDataCrystal = crystalControl.CreateCrystal<FullCredit.GoshujinClass>(new()
        {
            SaveFormat = SaveFormat.Binary,
            NumberOfFileHistories = 3,
            FileConfiguration = new GlobalFileConfiguration("Merger/Credits"),
            StorageConfiguration = mergerStorage,
        });

        this.equityCreditCrystal = crystalControl.CreateCrystal<EquityCreditPoint.GoshujinClass>(new()
        {
            SaveFormat = SaveFormat.Binary,
            NumberOfFileHistories = 3,
            FileConfiguration = new GlobalFileConfiguration("Merger/EquityCredits"),
            StorageConfiguration = mergerStorage,
        });

        if (string.IsNullOrEmpty(this.Configuration.Name))
        {
            this.Configuration.Name = $"{this.netBase.NetOptions.NodeName}{NameSuffix}";
        }

        this.creditData = this.creditDataCrystal.Data;
        this.equityCreditPoints = this.equityCreditCrystal.Data;
        this.seedKey = seedKey;
        this.PublicKey = this.seedKey.GetSignaturePublicKey();

        this.Initialized = true;
    }

    async Task IUnitPreparable.Prepare(UnitMessage.Prepare message)
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

        this.logger.TryGet()?.Log($"{this.Configuration.Name}: {this.PublicKey.ToString()}, Credits: {this.creditDataCrystal.Data.Count}+{this.equityCreditCrystal.Data.Count}/{this.Configuration.MaxCredits}");
    }

    async Task IUnitExecutable.StartAsync(UnitMessage.StartAsync message, CancellationToken cancellationToken)
    {
    }

    async Task IUnitExecutable.Stop(UnitMessage.Stop message)
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
        if (credit is null)
        {
            return default;
        }

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

    public async Task<T3csResult> CreateCredit(CreditIdentity creditIdentity)
    {
        if (!this.Initialized)
        {
            return T3csResult.UnknownError;
        }

        var credit = creditIdentity.ToCredit();
        if (credit is null)
        {
            return T3csResult.InvalidData;
        }

        if (credit.Mergers.Length != 1 ||
            !credit.Mergers[0].Equals(this.PublicKey))
        {
            return T3csResult.NotSupported;
        }

        using (var dataScope = await this.equityCreditPoints.TryLock(credit, AcquisitionMode.Create).ConfigureAwait(false))
        {
            if (dataScope.IsValid)
            {
                dataScope.Data.Initialize(credit, creditIdentity);
                return T3csResult.Success;
            }
            else
            {
                return T3csResult.AlreadyExists;
            }
        }
    }

    public async NetTask<T3csResultAndValue<Credit>> CreateCredit(CreateCreditParams param)
    {
        if (!this.Initialized)
        {
            return new(T3csResult.NoData);
        }
        else if (!param.Proof.ValidateAndVerify())
        {
            return new(T3csResult.InvalidData);
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

        var borrowers = await creditData.Owners.TryGet();
        if (borrowers is null)
        {
            return new(T3csResult.NoData);
        }

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

        var owners = await creditData.Owners.TryGet().ConfigureAwait(false);
        if (owners is null)
        {
            return null;
        }

        return owners.TryGet(token.PublicKey);
    }
}
