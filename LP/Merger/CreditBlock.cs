// Copyright (c) All contributors. All rights reserved. Licensed under the MIT license.

namespace LP.T3CS;

[TinyhandObject]
public partial class CreditBlock
{
    public CreditBlock()
    {
    }

    public CreditBlock(PublicKey publicKey)
    {
        this.PublicKey = publicKey;
        this.CreatedMics = Mics.GetCorrected();
    }

    [Key(0)]
    public PublicKey PublicKey { get; private set; }

    [Key(1)]
    public long CreatedMics { get; private set; }
}
