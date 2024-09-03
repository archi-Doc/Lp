// Copyright (c) All contributors. All rights reserved. Licensed under the MIT license.

using Netsphere.Crypto;

namespace Lp;

public static class LpConstants
{
    public const string LpAlias = "Lp";
    public const string LpPublicKeyString = "(BAL-lWmqHC4qMeW1fpNhLdXMMF2MDdR-yBGG_Ly6ehoiyJSX)";

    public static readonly SignaturePublicKey LpPublicKey;

    static LpConstants()
    {
        SignaturePublicKey.TryParse(LpPublicKeyString, out LpPublicKey);
        KeyAlias.AddAlias(LpPublicKey, LpAlias);
    }
}
