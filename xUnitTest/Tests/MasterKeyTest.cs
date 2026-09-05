// Copyright (c) All contributors. All rights reserved. Licensed under the MIT license.

using Arc;
using Arc.Crypto;
using Lp.Services;
using Xunit;

namespace xUnitTest;

public class MasterKeyTest
{
    [Fact]
    public void FormattingAndParsingRespectBufferBoundaries()
    {
        var original = MasterKey.New();
        var text = original.ConvertToString();
        Assert.True(MasterKey.TryParse(text, out var parsed, out var read));
        Assert.Equal(text.Length, read);
        Assert.Equal(text, parsed.ConvertToString());
        Assert.False(original.TryFormat(new char[text.Length - 1], out var written));
        Assert.Equal(0, written);
        Assert.False(MasterKey.TryParse(text.AsSpan(1), out _, out read));
        Assert.Equal(0, read);
        Assert.False(MasterKey.TryParse(new string('!', text.Length), out _, out read));
        Assert.Equal(0, read);
    }

    [Fact]
    public void ConcurrentDerivationIsDeterministicAndDoesNotModifyMasterKey()
    {
        var encoded = Base64Url.EncodeToString(Enumerable.Range(0, MasterKey.Size).Select(x => (byte)x).ToArray());
        Assert.True(MasterKey.TryParse(encoded, out var master, out _));
        var kinds = Enum.GetValues<MasterKey.Kind>();
        var expected = kinds.Select(kind => master.CreateSeedKey(kind).Seedphrase).ToArray();
        Parallel.For(0, 512, i =>
        {
            var index = i % kinds.Length;
            Assert.Equal(expected[index], master.CreateSeedKey(kinds[index]).Seedphrase);
            Assert.Equal(encoded, master.ConvertToString());
        });
        Assert.Equal(kinds.Length, expected.Distinct().Count());
    }
}
