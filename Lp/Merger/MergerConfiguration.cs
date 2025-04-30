// Copyright (c) All contributors. All rights reserved. Licensed under the MIT license.

using System.ComponentModel;
using Lp.T3cs;
using Netsphere.Crypto;

namespace Lp;

[TinyhandObject(ImplicitKeyAsName = true, EnumAsString = true)]
public partial record MergerConfiguration
{
    public const string MergerFilename = "MergerConfiguration.tinyhand";
    public const string RelayMergerFilename = "RelayMergerConfiguration.tinyhand";
    public const string DefaultName = "Test1";
    public const int DefaultMaxCredit = 1_000_000;

    public enum Type
    {
        Multi,
        Single,
    }

    public MergerConfiguration()
    {
    }

    public IMergerClient.InformationResult ToInformationResult()
    {
        return new IMergerClient.InformationResult() with { MergerName = this.MergerName, };
    }

    [DefaultValue(DefaultName)]
    [MaxLength(Alias.MaxAliasLength)]
    public partial string MergerName { get; private set; } = string.Empty;

    public Type MergerType { get; set; }

    [KeyAsName(ConvertToString = true)]
    public Credit? SingleCredit { get; set; }

    [DefaultValue(DefaultMaxCredit)]
    public int MaxCredits { get; set; }

    [TinyhandOnDeserialized]
    public void OnDeserialized()
    {
        if (this.MergerType == Type.Single)
        {
            this.MaxCredits = 1;
        }
        else if (this.MaxCredits < 0)
        {
            this.MaxCredits = DefaultMaxCredit;
        }
    }
}
