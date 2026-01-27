// Copyright (c) All contributors. All rights reserved. Licensed under the MIT license.

using Lp.T3cs;
using Netsphere.Crypto;

namespace Lp.T3cs.Domain;

/*[MachineObject(UseServiceProvider = true)]
public partial class DomainMachine : Machine<DomainMachineKind>
{
    private readonly ILogger logger;
    private readonly IUserInterfaceService userInterfaceService;
    private readonly LpService lpService;
    private readonly NetUnit netUnit;
    // private DomainIdentifier? domainIdentifier;
    private SeedKey? seedKey;
    private bool isMerger;

    public DomainMachine(ILogger<DomainMachine> logger, IUserInterfaceService userInterfaceService, LpService lpService, NetUnit netUnit, IConsoleService con)
    {
        this.logger = logger;
        this.userInterfaceService = userInterfaceService;
        this.lpService = lpService;
        this.netUnit = netUnit;

        this.DefaultTimeout = TimeSpan.FromSeconds(1);
    }

    protected override void OnCreate(object? createParam)
    {
        if (createParam is string domainIdentifierString)
        {
            DomainIdentifier? domainIdentifier = default;
            try
            {
                domainIdentifier = TinyhandSerializer.DeserializeFromString<DomainIdentifier>(domainIdentifierString);
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
                this.logger.TryGet(LogLevel.Error)?.Log(Hashed.Peer.ParseFailure, domainIdentifierString);
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

        this.isMerger = this.seedKey.GetSignaturePublicKey().Equals(this.domainIdentifier.Credit.PrimaryMerger);
        this.Show();

        this.ChangeState(State.Connect);
        return StateResult.Continue;
    }

    [StateMethod]
    protected async Task<StateResult> Connect(StateParameter parameter)
    {
        if (this.domainIdentifier is null)
        {
            return StateResult.Terminate;
        }

        var netNode = this.domainIdentifier.NetNode;
        netNode = Alternative.NetNode;
        using (var connection = await this.netUnit.NetTerminal.Connect(netNode))
        {
            if (connection is null)
            {
                this.logger.TryGet(LogLevel.Error)?.Log(Hashed.Error.Connect, this.domainIdentifier.NetNode.ToString());
                return StateResult.Terminate;
            }

            this.logger.TryGet(LogLevel.Information)?.Log("Connected");
            return StateResult.Terminate;
        }

        return StateResult.Continue;
    }

    [CommandMethod(WithLock = false)]
    protected CommandResult Show()
    {
        if (this.domainIdentifier is { } domainIdentifier)
        {
            this.logger.TryGet(LogLevel.Information)?.Log(this.GetInformation());
        }

        return CommandResult.Success;
    }

    private string GetInformation()
    {
        if (this.domainIdentifier is not { } domainIdentifier)
        {
            return string.Empty;
        }

        return $"{(this.isMerger ? "Merger" : "Peer")}: {domainIdentifier.ToString()}";
    }
}*/
