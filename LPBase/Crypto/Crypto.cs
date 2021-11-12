// Copyright (c) All contributors. All rights reserved. Licensed under the MIT license.

namespace LP;

public static class Crypto
{
    public const string ECCurveName = "secp256r1";
    public const string ECCurveNodeKey = "secp256r1";

    public static NodePrivateKey AlternativePrivateKey
    {
        get
        {
            if (alternativePrivateKey == null)
            {
                alternativePrivateKey = NodePrivateKey.Create("Alternative");
            }

            return alternativePrivateKey;
        }
    }

    private static NodePrivateKey? alternativePrivateKey;
}
