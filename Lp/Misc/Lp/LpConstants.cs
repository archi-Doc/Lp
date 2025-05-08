// Copyright (c) All contributors. All rights reserved. Licensed under the MIT license.

using Lp.Services;
using Lp.T3cs;
using Netsphere.Crypto;

namespace Lp;

public static class LpConstants
{
    public const int MaxNameLength = 32;
    public const string LpAlias = "Lp";
    public const string LpKeyAlias = "LpKey";
    public const string LpPublicKeyString = "(s:ki0czJKQj1yy1YEtzJErP2CVYj-LbuvnIwCwlfYtLT3Ri5U7)";

    public static readonly SignaturePublicKey LpPublicKey;
    public static readonly Identity LpIdentity;
    public static readonly Credit LpCredit;

    static LpConstants()
    {
        SignaturePublicKey.TryParse(LpPublicKeyString, out LpPublicKey, out _);
        Alias.Add(LpKeyAlias, LpPublicKey);
        LpIdentity = new(IdentityKind.Credit, LpPublicKey, [LpPublicKey]);
        Alias.Add(LpAlias, LpIdentity.GetIdentifier());
        Credit.TryCreate(LpIdentity, out LpCredit!);
    }

    public static void Initialize()
    {
    }
}
