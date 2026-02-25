// Copyright (c) All contributors. All rights reserved. Licensed under the MIT license.

using System.Runtime.CompilerServices;
using Lp.T3cs;
using Netsphere.Crypto;

namespace Lp;

#pragma warning disable SA1203 // Constants should appear before fields

public static class LpConstants
{
    public const string PromptString = "> ";
    public const string MultilineIndeitifierString = "\"\"\"";
    public const string MultilinePromptString = "# ";
    public const char PointSymbol = '#';
    public const int MaxPointLength = 19; // 1_000_000_000_000_000_000
    public const Point MaxPoint = 1_000_000_000_000_000_000; // k, m, g, t, p, e, 1z
    public const Point MinPoint = 0; // -MaxPoint;
    public const char CreditSymbol = '@';
    public const char MergerSymbol = '/';
    public const char MergerSeparatorSymbol = '+';
    public const int MaxMergers = 3; // MaxMergersCode
    public const char SeparatorSymbol = ' ';
    public const int MaxUrlLength = 500;
    public const int MaxCodeLength = 106; // SeedKeyHelper.MaxPrivateKeyLengthInBase64

    public const int DefaultProofMaxValiditySeconds = 3600 * 24 * 30;

    public const string LpAlias = "LpId";
    public const string LpKeyAlias = "LpKey";
    public const string LpPublicKeyString = "(s:ki0czJKQj1yy1YEtzJErP2CVYj-LbuvnIwCwlfYtLT3Ri5U7)";
    public const long LpExpirationMics = Mics.MicsPerDay * 1;
    public const string DomainKeyAlias = "DomainKey";

    public static readonly SignaturePublicKey LpPublicKey;
    public static readonly CreditIdentity LpIdentity;
    public static readonly Identifier LpIdentifier;
    public static readonly Credit LpCredit;

    public const string TestAlias = "TestId";
    public const string TestKeyAlias = "TestKey";
    public const string TestSecretKeyString = "!!!DejdC4mUaFFIn4OMN56ySzI_fs3K3sixDUxu4AghL3PUudfx!!!(s!1Vh2JRGJJt7B-Q0wMN-s0P5zfq8hH47L1yXFk8LseVDiPCjp))";
    public const string CodeDescription = "Authority, Vault, MasterKey(Merger...),Raw key";

    public static readonly SignaturePublicKey TestPublicKey;
    public static readonly SeedKey TestSeedKey;
    public static readonly CreditIdentity TestIdentity;
    public static readonly Identifier TestIdentifier;

    static LpConstants()
    {
        SignaturePublicKey.TryParse(LpPublicKeyString, out LpPublicKey, out _);
        Alias.Instance.Add(LpKeyAlias, LpPublicKey);
        LpIdentity = new CreditIdentity(default, LpPublicKey, [LpPublicKey]);
        LpIdentifier = LpIdentity.GetIdentifier();
        Alias.Instance.Add(LpAlias, LpIdentifier);
        Credit.TryCreate(LpIdentity, out LpCredit!);

        SeedKey.TryParse(TestSecretKeyString, out TestSeedKey!);
        TestPublicKey = TestSeedKey.GetSignaturePublicKey();
        Alias.Instance.Add(TestKeyAlias, TestPublicKey);
        TestIdentity = new CreditIdentity(default, TestPublicKey, [TestPublicKey]);
        TestIdentifier = TestIdentity.GetIdentifier();
        Alias.Instance.Add(TestAlias, TestIdentifier);
    }

#pragma warning disable CA2255 // The 'ModuleInitializer' attribute should not be used in libraries
    [ModuleInitializer]
#pragma warning restore CA2255 // The 'ModuleInitializer' attribute should not be used in libraries
    public static void Initialize()
    {
    }
}
