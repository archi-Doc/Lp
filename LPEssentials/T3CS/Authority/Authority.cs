// Copyright (c) All contributors. All rights reserved. Licensed under the MIT license.

using System.Security.Cryptography;

namespace LP;

/// <summary>
/// Immutable authority (name + public key).
/// </summary>
public static class Authority
{
    public const int NameLength = 16;
    public const string ECCurveName = "secp256r1";
    public const int PublicKeyLength = 64;
    public const int PrivateKeyLength = 32;
    public const int PublicKeyHalfLength = PublicKeyLength / 2;

    public static ECCurve ECCurve { get; }

    static Authority()
    {
        ECCurve = ECCurve.CreateFromFriendlyName(ECCurveName);
    }
}
