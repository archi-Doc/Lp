// Copyright (c) All contributors. All rights reserved. Licensed under the MIT license.

using Lp.T3cs;
using Netsphere.Crypto;

namespace Lp.T3cs.Domain;

[MachineObject(UseServiceProvider = true, Control = MachineControlKind.Unordered)]
public partial class DomainMachine : Machine<byte>
{
    private readonly ILogger logger;
    private readonly IUserInterfaceService userInterfaceService;
    private readonly LpService lpService;
    private readonly NetUnit netUnit;
    private DomainIdentifier? domainIdentifier;
    private SeedKey? seedKey;

    public DomainMachine(ILogger<DomainMachine> logger, IUserInterfaceService userInterfaceService, LpService lpService, NetUnit netUnit)
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
            DomainIdentifier? domainIdentifier = default;
            try
            {
                domainIdentifier = TinyhandSerializer.DeserializeFromString<DomainIdentifier>(codeAndCredit);
            }
            catch
            {
            }

            if (domainIdentifier is not null &&
                domainIdentifier.Validate())
            {
                this.domainIdentifier = domainIdentifier;
            }
            else
            {
                this.logger.TryGet(LogLevel.Error)?.Log(Hashed.Peer.ParseFailure, codeAndCredit);
            }
        }
    }

    [StateMethod(0)]
    protected async Task<StateResult> Initial(StateParameter parameter)
    {
        if (this.domainIdentifier is null)
        {
            return StateResult.Terminate;
        }

        this.seedKey = await this.lpService.LoadSeedKey(this.logger, this.domainIdentifier.Code);
        if (this.seedKey is null)
        {
            // this.logger.TryGet(LogLevel.Fatal)?.Log(Hashed.Peer.CodeFailure, this.codeAndCredit);
            return StateResult.Terminate;
        }

        this.logger.TryGet(LogLevel.Information)?.Log(this.domainIdentifier.ToString());

        this.ChangeState(State.Check);
        return StateResult.Continue;
    }

    [StateMethod]
    protected async Task<StateResult> Check(StateParameter parameter)
    {
        return StateResult.Continue;
    }

    [CommandMethod(WithLock = false)]
    protected CommandResult Show()
    {
        if (this.domainIdentifier is { } domainIdentifier)
        {
            this.logger.TryGet(LogLevel.Information)?.Log(domainIdentifier.ToString());
        }

        return CommandResult.Success;
    }
}
