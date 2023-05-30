// Copyright (c) All contributors. All rights reserved. Licensed under the MIT license.

using CrystalData;
using Tinyhand;
using ValueLink;
using Xunit;

namespace xUnitTest.CrystalDataTest;

[TinyhandObject]
[ValueLinkObject]
public partial class SimpleData : BaseData
{
    public SimpleData(IBigCrystal crystal, BaseData? parent, string name)
        : base(crystal, parent)
    {
    }

    public SimpleData()
    {
    }

    [Key(4, AddProperty = "Id")]
    private int id;
}

public class BigCrystalTest
{
    [Fact]
    public async Task Test_NoPreparation()
    {
        var crystal = await TestHelper.CreateAndStartSimple(false);
        crystal.IsNotNull();
        crystal.Data.IsNotNull();
        crystal.Data.BigCrystal.IsNotNull();

        await TestHelper.UnloadAndDeleteAll(crystal);
    }
}
