// Copyright (c) All contributors. All rights reserved. Licensed under the MIT license.

using Lp.T3cs;
using Netsphere.Crypto;

namespace Lp.Machines;

[MachineObject(UseServiceProvider = true)]
public partial class PeerMachine : Machine
{
    private readonly ILogger logger;
    private readonly IUserInterfaceService userInterfaceService;
    private readonly LpService lpService;
    private readonly NetUnit netUnit;
    private string codeAndCredit = string.Empty;
    private SeedKey? seedKey;

    public PeerMachine(ILogger<PeerMachine> logger, IUserInterfaceService userInterfaceService, LpService lpService, NetUnit netUnit)
    {
        this.logger = logger;
        this.userInterfaceService = userInterfaceService;
        this.lpService = lpService;
        this.netUnit = netUnit;

        this.DefaultTimeout = TimeSpan.FromSeconds(1);
    }

    protected override void OnCreate(object? createParam)
    {
        if (createParam is string codeAndCredit)
        {
            this.codeAndCredit = codeAndCredit;
        }
    }

    [StateMethod(0)]
    protected async Task<StateResult> Initial(StateParameter parameter)
    {
        if (!CodeAndCredit.TryParse(this.codeAndCredit, out var codeAndCredit, out _))
        {
            this.logger.TryGet(LogLevel.Fatal)?.Log(Hashed.Peer.ParseFailure, this.codeAndCredit);
            return StateResult.Terminate;
        }

        this.seedKey = await this.lpService.LoadSeedKey(this.logger, codeAndCredit.Code);
        if (this.seedKey is null)
        {
            // this.logger.TryGet(LogLevel.Fatal)?.Log(Hashed.Peer.CodeFailure, this.codeAndCredit);
            return StateResult.Terminate;
        }

        this.logger.TryGet(LogLevel.Information)?.Log(Hashed.Peer.Confirmation, this.codeAndCredit);

        return StateResult.Continue;
    }

    [StateMethod(0)]
    protected async Task<StateResult> Check(StateParameter parameter)
    {
        return StateResult.Continue;
    }
}
