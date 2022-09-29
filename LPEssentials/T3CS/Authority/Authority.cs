// Copyright (c) All contributors. All rights reserved. Licensed under the MIT license.

using System.Security.Cryptography;

namespace LP;

public static class Authority
{
    public const string KeyVaultPrefix = "Authority\\";
    public const string ECCurveName = "secp256r1";
    public const int PublicKeyLength = 64;
    public const int PublicKeyHalfLength = PublicKeyLength / 2;
    public const int PrivateKeyLength = 32;
    public const int SignLength = 64;

    public static ECCurve ECCurve { get; }

    public static HashAlgorithmName HashAlgorithmName { get; }

    static Authority()
    {
        ECCurve = ECCurve.CreateFromFriendlyName(ECCurveName);
        HashAlgorithmName = HashAlgorithmName.SHA256;
    }
}
