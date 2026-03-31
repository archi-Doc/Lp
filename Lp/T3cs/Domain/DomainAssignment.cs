// Copyright (c) All contributors. All rights reserved. Licensed under the MIT license.

namespace Lp.T3cs;

[TinyhandObject(AddAlternateKey = true, EnumAsString = true)]
public partial record class DomainAssignment
{
    #region FieldAndProperty

    [Key(0)]
    public string Name { get; init; } = string.Empty;

    [Key(1)]
    public string Code { get; init; } = string.Empty;

    [Key(2)]
    public CertificateProof CertificateProof { get; init; } = CertificateProof.UnsafeConstructor();

    public ulong GetDomainHash()
        => this.CertificateProof.GetIdentifier().Id1;

    // [Key(2)]
    // [MaxLength(LpConstants.MaxUrlLength)]
    // public partial string Url { get; init; } = string.Empty;

    #endregion

    public DomainAssignment(string name, string code, CertificateProof certificateProof)
    {
        this.Name = name;
        this.Code = code;
        this.CertificateProof = certificateProof;
    }

    public bool Validate(ValidationOption validationOption)
    {
        return this.CertificateProof.Validate(validationOption);
    }

    public override string ToString()
        => StringHelper.SerializeToString(this);
}
