// Copyright (c) All contributors. All rights reserved. Licensed under the MIT license.

using FastExpressionCompiler;
using Lp.T3cs;
using Netsphere.Crypto;

namespace Lp.Net;

[TinyhandObject]
internal partial class DomainData
{
    /// <summary>
    /// Represents the role within a domain.
    /// </summary>
    public enum Role : byte
    {
        /// <summary>
        /// Participates as a general user.
        /// </summary>
        User,

        /// <summary>
        /// Located directly under the Root; shares and distributes domain information.
        /// </summary>
        Peer,

        /// <summary>
        /// Serves as the foundation of trust for the domain and manages and distributes domain information.
        /// </summary>
        Root,
    }

    [Key(0)]
    private Credit domainCredit;

    [Key(1)]
    private PeerProof.GoshujinClass peerProofs = new();

    private Role domainRole;
    private SeedKey? domainSeedKey;

    public DomainData(Credit domainCredit)
    {
        this.domainCredit = domainCredit;
    }

    public void Update(Role domainRole, SeedKey? domainSeedKey)
    {//
        if (domainRole == Role.Root)
        {
            if (domainSeedKey?.GetSignaturePublicKey().Equals(this.domainCredit.PrimaryMerger) != true)
            {
                domainRole = Role.User;
                domainSeedKey = default;
            }
        }

        this.domainRole = domainRole;
        this.domainSeedKey = domainSeedKey;
    }

    public DomainOverview GetOverview()
    {
        int count;
        PeerProof? peerProof;
        using (this.peerProofs.LockObject.EnterScope())
        {
            count = this.peerProofs.Count;
            peerProof = this.peerProofs.GetRandomInternal();
        }

        return new(count, peerProof);
    }
}
