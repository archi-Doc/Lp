// Copyright (c) All contributors. All rights reserved. Licensed under the MIT license.

using System.Diagnostics.CodeAnalysis;
using Lp.T3cs;
using Netsphere.Crypto;

namespace Lp.T3cs.Domain;

[MachineObject(UseServiceProvider = true)]
public partial class DomainMachine : Machine<ulong>
{
    private readonly ILogger logger;
    private readonly IUserInterfaceService userInterfaceService;
    private readonly LpService lpService;
    private readonly NetUnit netUnit;
    private readonly DomainControl domainControl;
    private DomainData? domainData;

    public ulong DomainHash => this.Identifier;

    public DomainMachine(ILogger<DomainMachine> logger, IUserInterfaceService userInterfaceService, LpService lpService, NetUnit netUnit, DomainControl domainControl)
    {
        this.logger = logger;
        this.userInterfaceService = userInterfaceService;
        this.lpService = lpService;
        this.netUnit = netUnit;
        this.domainControl = domainControl;

        this.DefaultTimeout = TimeSpan.FromSeconds(1);
    }

    protected override void OnCreate(object? createParam)
    {
    }

    [StateMethod(0)]
    protected async Task<StateResult> Initial(StateParameter parameter)
    {
        if (!this.EnsureDomainData())
        {
            return StateResult.Terminate;
        }

        this.domainData.Initial();

        return StateResult.Continue;
    }

    /*[StateMethod]
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
    }*/

    [MemberNotNullWhen(true, nameof(domainData))]
    private bool EnsureDomainData()
    {
        this.domainData = this.domainControl.GetDomainData(this.DomainHash);
        return this.domainData is not null;
    }
}
