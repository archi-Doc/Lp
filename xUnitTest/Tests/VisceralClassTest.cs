// Copyright (c) All contributors. All rights reserved. Licensed under the MIT license.

using Lp.Data;
using Xunit;

namespace xUnitTest;

public class VisceralClassTest
{
    [Fact]
    public void FieldsPropertiesAndAliasesCanBeReadAndWritten()
    {
        var target = new Target();
        var operation = VisceralClass.TryGet(target)!;
        Assert.True(operation.TrySet("Field", 42));
        Assert.True(operation.TryGet<int>("Field", out var field));
        Assert.Equal(42, field);
        Assert.Equal(42, target.Field);
        Assert.True(operation.TrySet("p", "changed"));
        Assert.True(operation.TryGet<string>("p", out var property));
        Assert.Equal("changed", property);
        Assert.False(operation.TrySet("ReadOnly", 2));
        Assert.False(operation.TrySet("missing", 2));
        Assert.False(operation.TrySet("Field", "wrong type"));
        Assert.False(operation.TryGet<int>("p", out _));
        Assert.False(operation.TryGet<int>("missing", out _));
        Assert.Contains("Field", operation.GetNames());
        Assert.Contains("p", operation.GetNames());
    }

    private sealed class Target
    {
#pragma warning disable SA1401 // Fields should be private
        public int Field = 0;
#pragma warning restore SA1401 // Fields should be private

        [ShortName("p")]
        public string Property { get; set; } = string.Empty;

        public int ReadOnly => 1;
    }
}
