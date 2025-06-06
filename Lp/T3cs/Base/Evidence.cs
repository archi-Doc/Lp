﻿// Copyright (c) All contributors. All rights reserved. Licensed under the MIT license.

using Tinyhand.IO;

namespace Lp.T3cs;

/// <summary>
/// Immutable evidence object (authentication within merger).
/// </summary>
[TinyhandUnion(0, typeof(CredentialEvidence))]
[TinyhandObject(ReservedKeyCount = Proof.ReservedKeyCount)]
public abstract partial class Evidence : IValidatable
{
    /// <summary>
    /// The number of reserved keys.
    /// </summary>
    public const int ReservedKeyCount = 4;

    #region FieldAndProperty

    public abstract Proof BaseProof { get; } // Owned by derived classes

    [Key(0, Level = TinyhandWriter.DefaultSignatureLevel + 1)]
    public byte[]? MergerSignature0 { get; protected set; }

    [Key(1, Level = TinyhandWriter.DefaultSignatureLevel + 2)]
    public byte[]? MergerSignature1 { get; protected set; }

    [Key(2, Level = TinyhandWriter.DefaultSignatureLevel + 3)]
    public byte[]? MergerSignature2 { get; protected set; }

    #endregion

    public virtual bool Validate() => this.BaseProof.Validate();

    public virtual bool ValidateAndVerify(int mergerIndex = LpConstants.MaxMergers)
    {
        if (!this.BaseProof.TryGetCredit(out var credit) ||
            !this.BaseProof.ValidateAndVerify())
        {
            return false;
        }

        return this.ValidateAndVerifyExceptProof(mergerIndex);
    }

    public bool ValidateAndVerifyExceptProof(int mergerIndex = LpConstants.MaxMergers)
    {
        if (!this.Validate())
        {
            return false;
        }

        if (!this.BaseProof.TryGetCredit(out var credit))
        {
            return false;
        }

        mergerIndex = Math.Min(mergerIndex, credit.MergerCount);
        if (mergerIndex > 0 && !Verify(0, this.MergerSignature0))
        {
            return false;
        }

        if (mergerIndex > 1 && !Verify(1, this.MergerSignature1))
        {
            return false;
        }

        if (mergerIndex > 2 && !Verify(2, this.MergerSignature2))
        {
            return false;
        }

        return true;

        bool Verify(int mergerIndex, byte[]? signature)
        {
            if (signature is null)
            {
                return false;
            }

            var writer = TinyhandWriter.CreateFromBytePool();
            writer.Level = TinyhandWriter.DefaultSignatureLevel + mergerIndex;
            try
            {
                ((ITinyhandSerializable)this).Serialize(ref writer, TinyhandSerializerOptions.Signature);
                var rentMemory = writer.FlushAndGetRentMemory();
                var result = credit.Mergers[mergerIndex].Verify(rentMemory.Span, signature);
                rentMemory.Return();
                return result;
            }
            catch
            {
                return false;
            }
            finally
            {
                writer.Dispose();
            }
        }
    }

    public byte[]? GetSignature(int index)
        => index switch
        {
            0 => this.MergerSignature0,
            1 => this.MergerSignature1,
            2 => this.MergerSignature2,
            _ => null,
        };

    public override string ToString() => this.ToString(default);

    public string ToString(IConversionOptions? conversionOptions)
    {
        if (this.BaseProof.TryGetCredit(out var credit))
        {
            return $"{{{this.BaseProof.ToString(conversionOptions)}}}{credit.Mergers.ToMergerString(conversionOptions)}";
        }
        else
        {
            return $"{{ {this.BaseProof.ToString(conversionOptions)} }}";
        }
    }

    internal bool SetSignInternal(int mergerIndex, byte[] signature)
    {
        switch (mergerIndex)
        {
            case 0:
                this.MergerSignature0 = signature;
                return true;

            case 1:
                this.MergerSignature1 = signature;
                return true;

            case 2:
                this.MergerSignature2 = signature;
                return true;

            default:
                return false;
        }
    }
}
