// Copyright (c) All contributors. All rights reserved. Licensed under the MIT license.

using System.ComponentModel;
using LP.NetServices.T3CS;

namespace LP;

[TinyhandObject(ImplicitKeyAsName = true, EnumAsString = true)]
public partial record MergerInformation
{
    public const string TinyhandName = "Merger.tinyhand";

    public enum Type
    {
        Multi,
        Single,
    }

    public MergerInformation()
    {
    }

    public MergerService.InformationResult ToInformationResult()
    {
        return new MergerService.InformationResult() with { Name = this.Name, };
    }

    [DefaultValue("MergerName")]
    public string Name { get; set; } = default!;

    public Type MergerType { get; set; }
}
