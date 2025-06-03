// Copyright (c) All contributors. All rights reserved. Licensed under the MIT license.

using System.Runtime.CompilerServices;
using Lp.T3cs;
using Netsphere.Crypto;

namespace Lp;

public static class LpConstants
{
    public const char PointSymbol = '#';
    public const int MaxPointLength = 19; // 1_000_000_000_000_000_000
    public const Point MaxPoint = 1_000_000_000_000_000_000; // k, m, g, t, p, e, 1z
    public const Point MinPoint = 1; // -MaxPoint;

    public const char CreditSymbol = '@';
    public const char MergerSymbol = '/';
    public const char MergerSeparatorSymbol = '+';
    public const int MaxMergers = 3; // MaxMergersCode

    public const long DefaultProofMaxValidMics = Mics.MicsPerDay * 30;

    public const string LpAlias = "Lp";
    public const string LpKeyAlias = "LpKey";
    public const string LpPublicKeyString = "(s:ki0czJKQj1yy1YEtzJErP2CVYj-LbuvnIwCwlfYtLT3Ri5U7)";
    public const long LpExpirationMics = Mics.MicsPerDay * 1;

    public static readonly SignaturePublicKey LpPublicKey;
    public static readonly Identity LpIdentity;
    public static readonly Identifier LpIdentifier;
    public static readonly Credit LpCredit;

    static LpConstants()
    {
        SignaturePublicKey.TryParse(LpPublicKeyString, out LpPublicKey, out _);
        Alias.Instance.Add(LpKeyAlias, LpPublicKey);
        LpIdentity = new(IdentityKind.Credit, LpPublicKey, [LpPublicKey]);
        LpIdentifier = LpIdentity.GetIdentifier();
        Alias.Instance.Add(LpAlias, LpIdentifier);
        Credit.TryCreate(LpIdentity, out LpCredit!);
    }

#pragma warning disable CA2255 // The 'ModuleInitializer' attribute should not be used in libraries
    [ModuleInitializer]
#pragma warning restore CA2255 // The 'ModuleInitializer' attribute should not be used in libraries
    public static void Initialize()
    {
    }
}
