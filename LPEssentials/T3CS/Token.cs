// Copyright (c) All contributors. All rights reserved. Licensed under the MIT license.

namespace LP;

/// <summary>
/// Immutable token object.
/// </summary>
[TinyhandObject]
public sealed partial class Token : IValidatable // , IEquatable<Token>
{
    public Token()
    {
    }

    public bool Validate()
    {
        return true;
    }

    [Key(0)]
    public long ExpirationMics { get; private set; }

    [Key(1)]
    public AuthorityPublicKey Authority { get; private set; } = default!;

    [Key(2)]
    public Identifier TargetIdentifier { get; private set; }


    [Key(3, PropertyName = "Mergers")]
    [MaxLength(MaxMergers)]
    private AuthorityPublicKey[] mergers = default!;

    [Key(4, Marker = true, PropertyName = "Signs")]
    [MaxLength(MaxMergers + 1, Authority.PublicKeyLength)]
    private byte[][] signs = default!;
}
