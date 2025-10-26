// Copyright (c) All contributors. All rights reserved. Licensed under the MIT license.

using Netsphere.Crypto;

namespace Lp.T3cs;

[TinyhandObject]
[ValueLinkObject(Isolation = IsolationLevel.Serializable)]
public partial class PeerProof : ProofWithPublicKey
{
    private const int MaxRandom = 100;

    [TinyhandObject(External = true)]
    public partial class GoshujinClass
    {
        public PeerProof? GetRandomInternal()
        {// LockObject
            var node = this.LinkedListChain.First;
            var count = RandomVault.Default.NextInt32(Math.Max(this.LinkedListChain.Count, MaxRandom));
            while (count-- > 0)
            {
                node = node!.LinkedListLink.Next;
            }

            return node;
        }
    }

    [Link(Primary = true, Type = ChainType.LinkedList, Name = "LinkedList")]
    [Link(Unique = true, Type = ChainType.Unordered, TargetMember = nameof(PublicKey))]
    public PeerProof(SignaturePublicKey publicKey)
        : base(publicKey)
    {
    }

    public SignaturePublicKey PeerPublicKey => this.PublicKey;

    public override int MaxValiditySeconds => Seconds.FromDays(1);

    public override bool Validate(ValidationOptions validationOptions)
    {
        if (!base.Validate(validationOptions))
        {
            return false;
        }

        return true;
    }
}
