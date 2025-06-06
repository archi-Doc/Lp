// Copyright (c) All contributors. All rights reserved. Licensed under the MIT license.

using System.Diagnostics.CodeAnalysis;
using Lp.Logging;
using Lp.T3cs;
using Netsphere.Crypto;
using Netsphere.Stats;

#pragma warning disable SA1401

namespace Lp;

public partial class Merger : UnitBase, IUnitPreparable, IUnitExecutable
{
    private const string NameSuffix = "_M";

    #region FieldAndProperty

    [MemberNotNullWhen(true, nameof(Configuration))]
    [MemberNotNullWhen(true, nameof(creditDataCrystal))]
    [MemberNotNullWhen(true, nameof(creditData))]
    public virtual bool Initialized { get; protected set; }

    public SignaturePublicKey PublicKey { get; protected set; }

    public MergerConfiguration? Configuration { get; protected set; }

    public MergerState State { get; protected set; } = new();

    protected ILogger logger;
    protected ModestLogger modestLogger;
    protected NetBase netBase;
    protected LpBase lpBase;
    protected NetStats netStats;
    protected ICrystal<FullCredit.GoshujinClass>? creditDataCrystal;
    protected FullCredit.GoshujinClass? creditData;
    protected SeedKey seedKey = SeedKey.Invalid;

    #endregion

    public Merger(UnitContext context, UnitLogger unitLogger, NetBase netBase, LpBase lpBase, NetStats netStats)
        : base(context)
    {
        this.logger = unitLogger.GetLogger<Merger>();
        this.modestLogger = new(this.logger);
        this.netBase = netBase;
        this.lpBase = lpBase;
        this.netStats = netStats;
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

        if (string.IsNullOrEmpty(this.Configuration.MergerName))
        {
            this.Configuration.MergerName = $"{this.netBase.NetOptions.NodeName}{NameSuffix}";
        }

        this.creditData = this.creditDataCrystal.Data;
        this.seedKey = seedKey;
        this.PublicKey = this.seedKey.GetSignaturePublicKey();

        this.InitializeLogger();

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

        this.logger.TryGet()?.Log($"{this.Configuration.MergerName}: {this.PublicKey.ToString()}, Credits: {this.creditDataCrystal.Data.Count}/{this.Configuration.MaxCredits}");
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
        using (var w = this.creditData.TryLock(Credit.Default, ValueLink.TryLockMode.GetOrCreate))
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
        var creditIdentity = new CreditIdentity(IdentityKind.Credit, param.Proof.PublicKey, [mergerPublicKey]);
        if (!Credit.TryCreate(creditIdentity, out var credit))
        {
            return new(T3csResult.UnknownError);
        }

        var borrowers = await creditData.Owners.Get();
        using (var w2 = borrowers.TryLock(param.Proof.PublicKey, ValueLink.TryLockMode.Create))
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

    public void UpdateState()
    {
        if (!this.Initialized)
        {
            return;
        }

        // Check net node
        this.State.NetNode = this.netStats.OwnNetNode;
        this.State.Name = this.Configuration.MergerName;
        if (this.State.NetNode is null)
        {
            this.modestLogger.NonConsecutive(Hashed.Error.NoFixedNode, LogLevel.Error)?.Log(Hashed.Error.NoFixedNode);
            return;
        }

        // Check node type
        if (this.netStats.OwnNodeType != NodeType.Direct)
        {
            this.modestLogger.NonConsecutive(Hashed.Error.NoDirectConnection, LogLevel.Error)?.Log(Hashed.Error.NoDirectConnection);
            return;
        }

        // Active
        if (!this.State.IsActive)
        {
            this.State.IsActive = true;
            this.logger.TryGet(LogLevel.Information)?.Log("Activated");
        }
    }

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

    protected void InitializeLogger()
    {
        this.modestLogger.SetLogger(this.logger);
        this.modestLogger.SetSuppressionTime(TimeSpan.FromSeconds(5));
    }
}
