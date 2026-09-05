// Copyright (c) All contributors. All rights reserved. Licensed under the MIT license.

using System.Xml.Linq;
using Lp;
using Lp.Services;
using Lp.T3cs;
using Microsoft.Extensions.DependencyInjection;
using Netsphere;
using Netsphere.Crypto;
using Xunit;
using xUnitTest.Lp;

namespace xUnitTest;

[Collection(LpFixtureCollection.Name)]
public class LpServiceTest
{
    private readonly string testAuthorityName = Guid.NewGuid().ToString("N");
    private readonly IServiceProvider serviceProvider;
    private readonly Authority authority;

    public LpServiceTest(LpFixture fixture)
    {
        this.serviceProvider = fixture.ServiceProvider;

        var authorityControl = this.serviceProvider.GetRequiredService<AuthorityControl>();
        this.authority = new Authority(default, AuthorityLifecycle.Application, 0);
        authorityControl.NewAuthority(this.testAuthorityName, string.Empty, this.authority);
    }

    [Fact]
    public async Task MalformedPointsAreRejectedInsteadOfSilentlyBecomingZero()
    {
        var service = this.serviceProvider.GetRequiredService<LpService>();
        var seed = SeedKey.NewSignature();
        foreach (var source in new[] { seed.UnsafeToString(), this.testAuthorityName })
        {
            foreach (var point in new[] { string.Empty, "abc", "9223372036854775808", "1#2" })
            {
                var result = await service.ParseAuthorityAndCredit($"{source}#{point}{LpConstants.LpCredit}");
                Assert.False(result.IsSuccess);
                Assert.Equal(LpService.ParseResultCode.InvalidFormat, result.Code);
            }
        }

        Assert.Null(service.ResolveMerger(Credit.Default));
    }

    [Fact]
    public async Task Test1()
    {
        var lpService = this.serviceProvider.GetRequiredService<LpService>();

        var seedKey = SeedKey.NewSignature();
        var publicKey = seedKey.GetSignaturePublicKey();
        var mergerSeedKey = SeedKey.NewSignature();
        var mergerPublicKey = mergerSeedKey.GetSignaturePublicKey();
        var identity = new CreditIdentity(default, publicKey, [mergerPublicKey]);
        var identifier = identity.GetIdentifier();

        var st = $"{seedKey.UnsafeToString()}@{identifier}/{mergerPublicKey}";
        var r = await lpService.ParseAuthorityAndCredit(st);
        r.IsSuccess.IsTrue();
        seedKey.Equals(r.SeedKey).IsTrue();
        r.Credit!.Identifier.Equals(identifier).IsTrue();
        r.Credit!.Mergers.SequenceEqual([mergerPublicKey]).IsTrue();

        st = $"{seedKey.UnsafeToString()}#999@{identifier}/{mergerPublicKey}";
        r = await lpService.ParseAuthorityAndCredit(st);
        r.IsSuccess.IsTrue();
        seedKey.Equals(r.SeedKey).IsTrue();
        r.Point.Is(999);
        r.Credit!.Identifier.Equals(identifier).IsTrue();
        r.Credit!.Mergers.SequenceEqual([mergerPublicKey]).IsTrue();

        st = $"{seedKey.UnsafeToString()}#111@{LpConstants.LpAlias}/{LpConstants.LpKeyAlias}";
        r = await lpService.ParseAuthorityAndCredit(st);
        r.IsSuccess.IsTrue();
        seedKey.Equals(r.SeedKey).IsTrue();
        r.Point.Is(111);
        r.Credit!.Identifier.Equals(LpConstants.LpIdentifier).IsTrue();
        r.Credit!.Mergers.SequenceEqual([LpConstants.LpPublicKey]).IsTrue();

        st = $"{this.testAuthorityName}#222@{LpConstants.LpAlias}/{LpConstants.LpKeyAlias}";
        r = await lpService.ParseAuthorityAndCredit(st);
        r.IsSuccess.IsTrue();
        this.authority.GetSeedKey(LpConstants.LpCredit).Equals(r.SeedKey).IsTrue();
        r.Point.Is(222);
        r.Credit!.Identifier.Equals(LpConstants.LpIdentifier).IsTrue();
        r.Credit!.Mergers.SequenceEqual([LpConstants.LpPublicKey]).IsTrue();
    }
}
