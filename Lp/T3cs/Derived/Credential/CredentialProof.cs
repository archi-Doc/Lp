// Copyright (c) All contributors. All rights reserved. Licensed under the MIT license.

namespace Lp.T3cs;

[TinyhandObject]
public sealed partial class CredentialProof : ProofWithSigner
{
    public CredentialProof(Value value, CredentialKind kind, CredentialState state)
    {
        this.Value = value;
        this.Kind = kind;
        this.State = state;
    }

    #region FieldAndProperty

    public override PermittedSigner PermittedSigner => PermittedSigner.Owner | PermittedSigner.Merger | PermittedSigner.LpKey;

    [Key(ProofWithSigner.ReservedKeyCount + 0)]
    public CredentialKind Kind { get; private set; }

    [Key(ProofWithSigner.ReservedKeyCount + 1)]
    public CredentialState State { get; private set; }

    public override long MaxValidMics => Mics.MicsPerDay * 1;

    #endregion

    public override bool Validate()
    {
        if (!base.Validate())
        {
            return false;
        }

        if (!this.State.IsValid)
        {
            return false;
        }

        return true;
    }

    public override string ToString() => this.ToString(default);

    public override string ToString(IConversionOptions? conversionOptions)
        => $"CredentialProof:{this.Kind} {this.SignedMics.MicsToDateTimeString()} {this.Value.ToString(conversionOptions)}, {this.State.ToString(conversionOptions)}";
}
