// Copyright (c) All contributors. All rights reserved. Licensed under the MIT license.

using Netsphere.Crypto;

namespace Lp.T3cs;

[TinyhandObject]
public partial class OrderProof : ProofWithPublicKey
{
    public enum OrderType : byte
    {
        NoLimit,
        LimitRatio,
    }

    public OrderProof()
    {
    }

    [Key(ProofWithPublicKey.ReservedKeyCount)]
    public OrderType Type { get; private set; }

    [Key(ProofWithPublicKey.ReservedKeyCount + 1)]
    public Point Point { get; private set; }

    [Key(ProofWithPublicKey.ReservedKeyCount + 2)]
    public Credit SourceCredit { get; private set; } = Credit.UnsafeConstructor();

    [Key(ProofWithPublicKey.ReservedKeyCount + 3)]
    public Credit DestinationCredit { get; private set; } = Credit.UnsafeConstructor();

    [Key(ProofWithPublicKey.ReservedKeyCount + 4)]
    public double Ratio { get; private set; }
}
