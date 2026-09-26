// Copyright (c) All contributors. All rights reserved. Licensed under the MIT license.

using Lp;
using Microsoft.Extensions.DependencyInjection;
using Xunit;
using xUnitTest.Lp;

namespace xUnitTest;

[Collection(LpFixtureCollection.Name)]
public class MachineRegistrationTest
{
    private readonly BigMachine bigMachine;

    public MachineRegistrationTest(LpFixture fixture)
    {
        this.bigMachine = fixture.ServiceProvider.GetRequiredService<BigMachine>();
    }

    [Fact]
    public void ATerminatedMachineCanBeCreatedAgain()
    {// BigMachines creates every machine instance through the service provider, so a singleton registration makes re-creation throw.
        // (The other machines, including DomainMachine with one instance per domain, depend on crystals that the fixture does not prepare.)
        var machine = this.bigMachine.RelayPeerMachine.GetOrCreate();
        Assert.True(machine.Terminate());

        var recreated = this.bigMachine.RelayPeerMachine.GetOrCreate();
        Assert.NotSame(machine, recreated);
        Assert.True(recreated.Terminate());
    }
}
