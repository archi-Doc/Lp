// Copyright (c) All contributors. All rights reserved. Licensed under the MIT license.

using Lp.Logging;
using Lp.Services;
using Lp.T3cs;
using Netsphere.Crypto;

namespace Lp.Machines;

[MachineObject(UseServiceProvider = true)]
public partial class LpDogmaMachine : Machine
{// Control: context.AddSingleton<Machines.RelayPeerMachine>();
    public LpDogmaMachine(IUserInterfaceService consoleSeuserInterfaceServicevice, ILogger<LpDogmaMachine> logger, AuthorityControl authorityControl, LpDogma lpDogma)
    {
        this.userInterfaceService = consoleSeuserInterfaceServicevice;
        this.logger = logger;
        this.modestLogger = new(this.logger);
        this.authorityControl = authorityControl;
        this.lpDogma = lpDogma;

        this.DefaultTimeout = TimeSpan.FromSeconds(3);
    }

    public int Count { get; set; }

    [StateMethod(0)]
    protected async Task<StateResult> Initial(StateParameter parameter)
    {
        if (await this.authorityControl.GetLpSeedKey(null) is not { } seedKey)
        {
            return StateResult.Continue;
        }
        else
        {
            this.modestLogger.Interval(TimeSpan.FromHours(1), Hashed.Dogma.KeyConfirmed, LogLevel.Information)?.Log(Hashed.Dogma.KeyConfirmed);
        }

        var list = this.lpDogma.CredentialNodes.ToList();
        list.Add(new(Alternative.NetNode, SeedKey.NewSignature().GetSignaturePublicKey(), SeedKey.NewSignature().GetSignaturePublicKey()));
        // this.lpKeyData.CredentialNodes = list.ToArray();

        // this.userInterfaceService.WriteLine($"Single: ({this.Identifier.ToString()}) - {this.Count++}");

        return StateResult.Continue;
    }

    private readonly IUserInterfaceService userInterfaceService;
    private readonly ModestLogger modestLogger;
    private readonly ILogger logger;
    private readonly AuthorityControl authorityControl;
    private readonly LpDogma lpDogma;
}
