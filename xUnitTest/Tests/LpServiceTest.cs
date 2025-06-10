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
using static SimpleCommandLine.SimpleParser;

namespace xUnitTest;

[Collection(LpFixtureCollection.Name)]
public class LpServiceTest
{
    private const string TestAuthorityName = "auth1";
    private readonly IServiceProvider serviceProvider;
    private readonly Authority authority;

    public LpServiceTest(LpFixture fixture)
    {
        this.serviceProvider = fixture.ServiceProvider;

        var authorityControl = this.serviceProvider.GetRequiredService<AuthorityControl>();
        this.authority = new Authority(default, AuthorityLifecycle.Application, 0);
        authorityControl.NewAuthority(TestAuthorityName, string.Empty, this.authority);
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

        st = $"{TestAuthorityName}#222@{LpConstants.LpAlias}/{LpConstants.LpKeyAlias}";
        r = await lpService.ParseAuthorityAndCredit(st);
        r.IsSuccess.IsTrue();
        this.authority.GetSeedKey(LpConstants.LpCredit).Equals(r.SeedKey).IsTrue();
        r.Point.Is(222);
        r.Credit!.Identifier.Equals(LpConstants.LpIdentifier).IsTrue();
        r.Credit!.Mergers.SequenceEqual([LpConstants.LpPublicKey]).IsTrue();
    }
}
