// Copyright (c) All contributors. All rights reserved. Licensed under the MIT license.

using Netsphere.Crypto;

namespace Lp.T3cs;

/// <summary>
/// The isolation level of the OwnerData class is RepeatableRead.<br/>
/// Call TryLock() when making changes.
/// For changes to Evidences, TryLock() is unnecessary since the instance remains the same.<br/>
/// Instead, acquire a lock with lock (this.Evidence.SyncObject).
/// </summary>
[TinyhandObject(Structural = true)]
[ValueLinkObject(Isolation = IsolationLevel.RepeatableRead, Restricted = true)]
public sealed partial record OwnerData // : ITinyhandCustomJournal
{
    public OwnerData()
    {
    }

    [Key(0)]
    [Link(Unique = true, Primary = true, Type = ChainType.Unordered)]
    public SignaturePublicKey PublicKey { get; private set; }

    [Key(1)]
    public MergeableEvidence.GoshujinClass Evidences { get; private set; } = new();

    [Key(2)]
    public AccountableLinkage.GoshujinClass Linkages { get; private set; } = new();

    /*
    void ITinyhandCustomJournal.WriteCustomLocator(ref TinyhandWriter writer)
    {
    }

    bool ITinyhandCustomJournal.ReadCustomRecord(ref TinyhandReader reader)
    {
        return false;
    }*/
}
