﻿// Copyright (c) All contributors. All rights reserved. Licensed under the MIT license.

using System.Diagnostics.CodeAnalysis;
using Lp.T3cs;
using Netsphere.Crypto;

#pragma warning disable SA1401

namespace Lp;

public partial class Merger : UnitBase, IUnitPreparable, IUnitExecutable
{
    public Merger(UnitContext context, UnitLogger unitLogger, LpBase lpBase)
        : base(context)
    {
        this.logger = unitLogger.GetLogger<Merger>();
        this.lpBase = lpBase;
    }

    public virtual void Initialize(Crystalizer crystalizer, SeedKey mergerSeedKey)
    {
        this.Information = crystalizer.CreateCrystal<MergerConfiguration>(new()
        {
            NumberOfFileHistories = 3,
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

        this.creditData = this.creditDataCrystal.Data;
        this.mergerSeedKey = mergerSeedKey;
        this.MergerPublicKey = this.mergerSeedKey.GetSignaturePublicKey();

        this.Initialized = true;
    }

    void IUnitPreparable.Prepare(UnitMessage.Prepare message)
    {
        if (!this.Initialized)
        {
            return;
        }

        // this.logger.TryGet()?.Log(this.Information.ToString());

        if (this.Information.MergerType == MergerConfiguration.Type.Single)
        {// Single credit
            this.Information.SingleCredit = Credit.Default;
        }
        else
        {// Multi credit
        }

        this.logger.TryGet()?.Log($"{this.Information.MergerName}: {this.MergerPublicKey.ToString()}, Credits: {this.creditDataCrystal.Data.Count}/{this.Information.MaxCredits}");
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
        var g = this.creditDataCrystal.Data;
        // var identifier = param.Proof.PublicKey.ToIdentifier();

        FullCredit? creditData;
        using (var w = g.TryLock(Credit.Default, ValueLink.TryLockMode.GetOrCreate))
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
        if (!Credit.TryCreate(param.Proof.PublicKey, [mergerPublicKey,], out var credit))
        {
            return new(T3csResult.UnknownError);
        }

        var borrowers = await creditData.Borrowers.Get();
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

    public SignaturePublicKey GetPublicKey()
        => this.MergerPublicKey;

    public bool TrySignProof(Proof proof, long validMics)
    {
        if (this.mergerSeedKey is null)
        {
            return false;
        }

        return this.mergerSeedKey.TrySignProof(proof, validMics);
    }

    #region FieldAndProperty

    [MemberNotNullWhen(true, nameof(Information))]
    [MemberNotNullWhen(true, nameof(creditDataCrystal))]
    [MemberNotNullWhen(true, nameof(creditData))]
    // [MemberNotNullWhen(true, nameof(mergerPrivateKey))]
    public virtual bool Initialized { get; protected set; }

    public SignaturePublicKey MergerPublicKey { get; protected set; }

    public MergerConfiguration? Information { get; protected set; }

    protected ILogger logger;
    protected LpBase lpBase;
    protected ICrystal<FullCredit.GoshujinClass>? creditDataCrystal;
    protected FullCredit.GoshujinClass? creditData;
    protected SeedKey? mergerSeedKey;
    protected MergerState mergerState = new();

    #endregion
}
