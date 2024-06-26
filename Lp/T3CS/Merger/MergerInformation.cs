﻿// Copyright (c) All contributors. All rights reserved. Licensed under the MIT license.

using System.ComponentModel;
using Lp.T3cs;
using Netsphere.Crypto;

namespace Lp;

[TinyhandObject(ImplicitKeyAsName = true, EnumAsString = true)]
public partial record MergerInformation : ITinyhandSerializationCallback
{
    public const string MergerFilename = "MergerInformation.tinyhand";
    public const string RelayMergerFilename = "RelayMergerInformation.tinyhand";
    public const string DefaultName = "Test merger";
    public const int DefaultMaxCredit = 1_000_000;

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
    public Credit? SingleCredit { get; set; }

    [DefaultValue(DefaultMaxCredit)]
    public int MaxCredits { get; set; }

    public void OnAfterReconstruct()
    {
    }

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
    }
}
