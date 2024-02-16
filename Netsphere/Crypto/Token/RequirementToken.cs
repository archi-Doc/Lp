// Copyright (c) All contributors. All rights reserved. Licensed under the MIT license.

using System.Diagnostics.CodeAnalysis;
using Netsphere.Misc;

namespace Netsphere.Crypto;

/// <summary>
/// Represents an authentication token.
/// </summary>
[TinyhandObject]
public sealed partial class RequirementToken : ISignAndVerify, IEquatable<RequirementToken>, IStringConvertible<RequirementToken>
{
    private const char Identifier = 'R';

    public RequirementToken()
    {
    }

    public static int MaxStringLength => 256;

    #region FieldAndProperty

    [Key(0)]
    public char TokenIdentifier { get; private set; } = Identifier;

    [Key(1)]
    public SignaturePublicKey PublicKey { get; set; }

    [Key(2, Level = 1)]
    public byte[] Signature { get; set; } = Array.Empty<byte>();

    [Key(3)]
    public long SignedMics { get; set; }

    [Key(4)]
    public uint MaxTransmissions { get; set; }

    [Key(5)]
    public int MaxBlockSize
    {
        get => this.maxBlockSize;
        set
        {
            this.maxBlockSize = value;
            var info = NetHelper.CalculateGene(this.maxBlockSize);
            this.MaxBlockGenes = info.NumberOfGenes;
        }
    }

    [Key(6)]
    public long MaxStreamLength
    {
        get => this.maxStreamLength;
        set
        {
            this.maxStreamLength = value;
            var info = NetHelper.CalculateGene(this.maxStreamLength);
            // this.MaxStreamGenes = info.NumberOfGenes;
        }
    }

    [Key(7)]
    public int StreamBufferSize
    {
        get => this.streamBufferSize;
        set
        {
            this.streamBufferSize = value;
            var info = NetHelper.CalculateGene(this.streamBufferSize);
            this.StreamBufferGenes = info.NumberOfGenes;
        }
    }

    [Key(8)]
    public bool AllowBidirectionalConnection { get; set; }

    [IgnoreMember]
    public int MaxBlockGenes { get; private set; }

    [IgnoreMember]
    public int StreamBufferGenes { get; private set; }

    private int maxBlockSize;
    private long maxStreamLength;
    private int streamBufferSize;

    #endregion

    public static bool TryParse(ReadOnlySpan<char> source, [MaybeNullWhen(false)] out RequirementToken token)
        => TokenHelper.TryParse(Identifier, source, out token);

    public bool Validate()
    {
        if (this.TokenIdentifier != Identifier)
        {
            return false;
        }
        else if (this.SignedMics == 0)
        {
            return false;
        }

        return true;
    }

    public bool Equals(RequirementToken? other)
    {
        if (other == null)
        {
            return false;
        }

        return this.PublicKey.Equals(other.PublicKey) &&
            this.Signature.SequenceEqual(other.Signature) &&
            this.SignedMics == other.SignedMics;
    }

    public override string ToString()
        => TokenHelper.ToBase64(this, Identifier);

    public int GetStringLength()
        => throw new NotImplementedException();

    public bool TryFormat(Span<char> destination, out int written)
        => TokenHelper.TryFormat(this, Identifier, destination, out written);
}
