// Copyright (c) All contributors. All rights reserved. Licensed under the MIT license.

using System.Text;
using Arc.Crypto;
using Lp;
using Xunit;

namespace xUnitTest;

public class SeedphraseTest
{
    [Fact]
    public void KnownSeedRemainsCompatible()
    {
        var phrase = Seedphrase.Create(Enumerable.Range(0, 23).Select(x => (ushort)x).ToArray());
        Assert.Equal("7D1850E16F1364F48A5E49008472F394BF32C634BC99A9DE7931F61509BAB0C0", Convert.ToHexString(Seedphrase.TryGetSeed(phrase)!));
        var upper = phrase.ToUpperInvariant();
        Assert.Equal(Sha3Helper.Get256_ByteArray(Encoding.UTF8.GetBytes(upper)), Seedphrase.TryGetSeed(upper));
    }

    [Theory]
    [InlineData(15)]
    [InlineData(23)]
    [InlineData(127)]
    [InlineData(128)]
    [InlineData(4096)]
    public void ValidPhrasesUseBothStackAndPoolPaths(int count)
    {
        var phrase = Seedphrase.Create(new ushort[count]);
        Assert.Equal(Sha3Helper.Get256_ByteArray(Encoding.UTF8.GetBytes(phrase)), Seedphrase.TryGetSeed(phrase));
    }

    [Fact]
    public void InvalidPhrasesAreRejected()
    {
        Assert.Null(Seedphrase.TryGetSeed(string.Empty));
        Assert.Null(Seedphrase.TryGetSeed(Seedphrase.Create(new ushort[14])));
        var phrase = Seedphrase.Create(new ushort[23]);
        Assert.Null(Seedphrase.TryGetSeed(" " + phrase));
        Assert.Null(Seedphrase.TryGetSeed(phrase + " "));
        Assert.Null(Seedphrase.TryGetSeed(phrase.Replace(" ", "  ")));
        Assert.Null(Seedphrase.TryGetSeed(phrase + " unknown-word"));
        var words = phrase.Split(' ');
        words[0] = Seedphrase.Create([1]).Split(' ')[0];
        Assert.Null(Seedphrase.TryGetSeed(string.Join(' ', words)));
        Assert.Throws<ArgumentOutOfRangeException>(() => Seedphrase.Create([ushort.MaxValue]));
    }

    [Fact]
    public void GeneratedPhraseHasExpectedLengthAndValidChecksum()
    {
        var phrase = Seedphrase.Create();
        Assert.Equal(Seedphrase.DefaultNumberOfWords, phrase.Split(' ').Length);
        Assert.Equal(32, Seedphrase.TryGetSeed(phrase)!.Length);
    }
}
