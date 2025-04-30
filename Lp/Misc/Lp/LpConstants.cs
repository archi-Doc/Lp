// Copyright (c) All contributors. All rights reserved. Licensed under the MIT license.

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
    public static readonly CreditIdentity LpIdentity;
    public static readonly Credit LpCredit;

    static LpConstants()
    {
        SignaturePublicKey.TryParse(LpPublicKeyString, out LpPublicKey, out _);
        Alias.Add(LpPublicKey, LpKeyAlias);
        LpIdentity = new(CreditKind.Full, LpPublicKey, [LpPublicKey]);
        Alias.Add(LpIdentity.GetIdentifier(), LpAlias);
        Credit.TryCreate(LpIdentity, out LpCredit!);
    }

    public static void Initialize()
    {
    }
}
