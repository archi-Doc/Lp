// Copyright (c) All contributors. All rights reserved. Licensed under the MIT license.

using Netsphere.Crypto;

namespace Lp;

public static class LpConstants
{
    public const string LpAlias = "Lp";

    public static readonly SignaturePublicKey LpPublicKey;

    static LpConstants()
    {
        SignaturePublicKey.TryParse("(BAL-lWmqHC4qMeW1fpNhLdXMMF2MDdR-yBGG_Ly6ehoiyJSX)", out LpPublicKey);
    }
}
