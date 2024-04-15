// Copyright (c) All contributors. All rights reserved. Licensed under the MIT license.

using System.ComponentModel;
using LP.T3CS;
using Netsphere.Crypto;

namespace LP;

[TinyhandObject(ImplicitKeyAsName = true, EnumAsString = true)]
public partial record MergerInformation : ITinyhandSerializationCallback
{
    public const string Filename = "Merger.tinyhand";
    public const string DefaultName = "Test merger";
    public const int DefaultMaxCredit = 1_000_000;
    public const string MergerPrivateKeyName = "mergerprivatekey";

    public enum Type
    {
        Multi,
        Single,
    }

    public MergerInformation()
    {
    }

    public IMergerService.InformationResult ToInformationResult()
    {
        return new IMergerService.InformationResult() with { MergerName = this.MergerName, };
    }

    [DefaultValue(DefaultName)]
    public string MergerName { get; set; } = default!;

    public Type MergerType { get; set; }

    [KeyAsName(ConvertToString = true)]
    public SignaturePrivateKey MergerPrivateKey { get; set; } = default!;

    [KeyAsName(ConvertToString = true)]
    public Credit? SingleCredit { get; set; }

    [DefaultValue(DefaultMaxCredit)]
    public int MaxCredits { get; set; }

    public override string ToString()
        => $"{this.MergerName}: {this.MergerType}({this.MaxCredits})";

    public void OnBeforeSerialize()
    {
    }

    public void OnAfterDeserialize()
    {
        if (this.MergerType == Type.Single)
        {
            this.MaxCredits = 1;
        }
        else if (this.MaxCredits < 0)
        {
            this.MaxCredits = DefaultMaxCredit;
        }

        /*if (!string.IsNullOrEmpty(this.MergerPrivateKeyString) &&
            SignaturePrivateKey.TryParse(this.MergerPrivateKeyString, out var mergerPrivateKey))
        {// 1st: Vault, 2nd: EnvironmentVariable
            this.MergerPrivateKey = mergerPrivateKey;
        }*/
        if (this.MergerPrivateKey is null &&
            CryptoHelper.TryParseFromEnvironmentVariable<SignaturePrivateKey>(MergerPrivateKeyName, out var mergerPrivateKey))
        {
            this.MergerPrivateKey = mergerPrivateKey;
        }

        this.MergerPrivateKey = SignaturePrivateKey.Create();
    }
}
