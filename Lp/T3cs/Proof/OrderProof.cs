// Copyright (c) All contributors. All rights reserved. Licensed under the MIT license.

using Netsphere.Crypto;

namespace Lp.T3cs;

[TinyhandObject]
public partial class OrderProof : ProofAndPublicKey
{
    public enum OrderType : byte
    {
        NoLimit,
        LimitRatio,
    }

    public OrderProof()
    {
    }

    [Key(Proof.ReservedKeyCount)]
    public OrderType Type { get; private set; }

    [Key(Proof.ReservedKeyCount + 1)]
    public Point Point { get; private set; }

    [Key(Proof.ReservedKeyCount + 2)]
    public Credit SourceCredit { get; private set; } = new();

    [Key(Proof.ReservedKeyCount + 3)]
    public Credit DestinationCredit { get; private set; } = new();

    [Key(Proof.ReservedKeyCount + 4)]
    public double Ratio { get; private set; }
}
