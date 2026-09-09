// Copyright (c) All contributors. All rights reserved. Licensed under the MIT license.

using Arc.Unit;
using Lp;
using Lp.Services;
using Microsoft.Extensions.DependencyInjection;
using Netsphere;
using Xunit;
using xUnitTest.Lp;

namespace xUnitTest;

[Collection(LpFixtureCollection.Name)]
public class AuthorityControlTest
{
    private readonly VaultControl vaultControl;

    public AuthorityControlTest(LpFixture fixture)
    {
        this.vaultControl = fixture.ServiceProvider.GetRequiredService<VaultControl>();
    }

    [Fact]
    public async Task AnAuthorityIsRetrievedWithAndWithoutItsPassword()
    {
        var ui = new StubUserInterfaceService("pass");
        var control = new AuthorityControl(ui, this.vaultControl);
        var name = Guid.NewGuid().ToString("N");

        Assert.False(control.Exists(name));
        Assert.True(control.NewAuthority(name, "pass", new Authority(new byte[32], AuthorityLifecycle.Application, 0)));
        Assert.False(control.NewAuthority(name, "pass", new Authority(new byte[32], AuthorityLifecycle.Application, 0)));
        Assert.True(control.Exists(name));

        Assert.NotNull(await control.GetAuthority(name));
        Assert.NotNull(await control.GetAuthority(name, "pass"));
        Assert.Null(await control.GetAuthority(name, "wrong"));
        Assert.NotNull(await control.GetSeedKey(name));

        Assert.Contains(name, control.GetNames());

        Assert.True(control.RemoveAuthority(name));
        Assert.False(control.RemoveAuthority(name));
        Assert.Null(await control.GetAuthority(name));
        Assert.Null(await control.GetSeedKey(name));
    }

    [Fact]
    public async Task AnExpiredAuthorityIsRenewedAfterTheUserSuppliesThePassword()
    {
        var ui = new StubUserInterfaceService("pass");
        var control = new AuthorityControl(ui, this.vaultControl);
        var name = Guid.NewGuid().ToString("N");

        // A Duration authority starts expired until its expiration is reset.
        var authority = new Authority(new byte[32], AuthorityLifecycle.Duration, Mics.FromMinutes(10));
        Assert.True(control.NewAuthority(name, "pass", authority));
        Assert.True(authority.IsExpired);

        var retrieved = await control.GetAuthority(name);
        Assert.NotNull(retrieved);
        Assert.False(retrieved.IsExpired);
        Assert.Equal(1, ui.PasswordRequests);

        // The renewed authority is returned without another prompt.
        Assert.NotNull(await control.GetAuthority(name));
        Assert.Equal(1, ui.PasswordRequests);

        Assert.True(control.RemoveAuthority(name));
    }

    [Fact]
    public async Task AWrongPasswordIsRetriedUntilTheUserGivesUp()
    {
        var ui = new StubUserInterfaceService(null);
        var control = new AuthorityControl(ui, this.vaultControl);
        var name = Guid.NewGuid().ToString("N");

        var authority = new Authority(new byte[32], AuthorityLifecycle.Duration, Mics.FromMinutes(10));
        Assert.True(control.NewAuthority(name, "pass", authority));

        Assert.Null(await control.GetAuthority(name));
        Assert.Equal(1, ui.PasswordRequests);

        Assert.True(control.RemoveAuthority(name));
    }

    [Fact]
    public async Task AnUnknownNameYieldsNoAuthority()
    {
        var control = new AuthorityControl(new StubUserInterfaceService("pass"), this.vaultControl);
        var name = Guid.NewGuid().ToString("N");
        Assert.Null(await control.GetAuthority(name));
        Assert.Null(await control.GetAuthority(name, "pass"));
        Assert.DoesNotContain(name, control.GetNames());
    }

    private sealed class StubUserInterfaceService : IUserInterfaceService
    {
        private readonly string? password;

        public StubUserInterfaceService(string? password)
        {
            this.password = password;
        }

        public int PasswordRequests { get; private set; }

        public Task<InputResult> ReadPassword(bool cancelOnEscape, string? description, CancellationToken cancellationToken = default)
        {
            this.PasswordRequests++;
            return Task.FromResult(this.password is null ? new InputResult(InputResultKind.Canceled) : new InputResult(this.password));
        }

        public Task<InputResult> ReadLine(bool cancelOnEscape, string? description, CancellationToken cancellationToken = default)
            => Task.FromResult(new InputResult(InputResultKind.Canceled));

        public Task<InputResultKind> ReadYesNo(bool cancelOnEscape, string? description, CancellationToken cancellationToken = default)
            => Task.FromResult(InputResultKind.Canceled);

        public void EnqueueLine(string? message = null)
        {
        }

        public void WriteLine(LogLevel logLevel, string? message)
        {
        }

        public void Write(string? message = null, ConsoleColor color = ConsoleColor.Gray)
        {
        }

        public void Write(ReadOnlySpan<char> message, ConsoleColor color = ConsoleColor.Gray)
        {
        }

        public void WriteLine(string? message = null, ConsoleColor color = ConsoleColor.Gray)
        {
        }

        public void WriteLine(ReadOnlySpan<char> message, ConsoleColor color = ConsoleColor.Gray)
        {
        }

        public Task<InputResult> ReadLine(CancellationToken cancellationToken = default)
            => Task.FromResult(new InputResult(InputResultKind.Canceled));

        public ConsoleKeyInfo ReadKey(bool intercept) => default;

        public bool EnableColor { get; set; }

        public bool KeyAvailable => false;
    }
}
