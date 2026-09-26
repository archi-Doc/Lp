// Copyright (c) All contributors. All rights reserved. Licensed under the MIT license.

using Lp;
using Xunit;

namespace xUnitTest;

public class UserInterfaceExtensionTest
{
    [Fact]
    public async Task AMatchingConfirmationReturnsThePassword()
    {
        var ui = new ScriptedUserInterfaceService("pass", "pass");
        var result = await ui.ReadPasswordAndConfirm(false, Hashed.Vault.EnterPassword, Hashed.Dialog.Password.Confirm);
        Assert.True(result.IsSuccess);
        Assert.Equal("pass", result.Text);
        Assert.Equal(2, ui.PasswordRequests);
    }

    [Fact]
    public async Task AMismatchStartsOverFromTheFirstPassword()
    {// The first (masked) input may be the mistyped one, so asking only for the confirmation again could never succeed.
        var ui = new ScriptedUserInterfaceService("typo", "pass", "pass", "pass");
        var result = await ui.ReadPasswordAndConfirm(false, Hashed.Vault.EnterPassword, Hashed.Dialog.Password.Confirm);
        Assert.True(result.IsSuccess);
        Assert.Equal("pass", result.Text);
        Assert.Equal(4, ui.PasswordRequests);
    }

    [Fact]
    public async Task TerminationWhileConfirmingIsReturned()
    {
        var ui = new ScriptedUserInterfaceService("pass");
        var result = await ui.ReadPasswordAndConfirm(false, Hashed.Vault.EnterPassword, Hashed.Dialog.Password.Confirm);
        Assert.True(result.IsTerminated);
    }
}
