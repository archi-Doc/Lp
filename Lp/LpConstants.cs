// Copyright (c) All contributors. All rights reserved. Licensed under the MIT license.

using Netsphere.Crypto;

namespace Lp;

public static class LpConstants
{
    public static readonly SignaturePublicKey LpKey;

    static LpConstants()
    {
        SignaturePublicKey.TryParse("(BAL-lWmqHC4qMeW1fpNhLdXMMF2MDdR-yBGG_Ly6ehoi)", out LpKey);
    }
}
