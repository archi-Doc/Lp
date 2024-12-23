﻿// Copyright (c) All contributors. All rights reserved. Licensed under the MIT license.

using Lp.T3cs;
using Netsphere.Crypto;

namespace Lp;

public static class LpConstants
{
    public const int MaxNameLength = 32;
    public const string LpAlias = "Lp";
    public const string LpPublicKeyString = "(BAL-lWmqHC4qMeW1fpNhLdXMMF2MDdR-yBGG_Ly6ehoiyJSX)";

    public static readonly SignaturePublicKey LpPublicKey;
    public static readonly Credit LpCredit;

    static LpConstants()
    {
        SignaturePublicKey.TryParse(LpPublicKeyString, out LpPublicKey, out _);
        KeyAlias.AddAlias(LpPublicKey, LpAlias);
        Credit.TryCreate(LpPublicKey, [LpPublicKey], out LpCredit!);
    }
}
