// Copyright (c) All contributors. All rights reserved. Licensed under the MIT license.

using System.Security.Cryptography.X509Certificates;
using Arc.Unit;
using BigMachines;
using LPRunner;
using Netsphere;
using Netsphere.Crypto;
using Netsphere.Server;

namespace LP.NetServices;

[NetServiceObject]
internal class RemoteControlService : IRemoteControlService
{// Remote -> LPRunner
    public RemoteControlService(ILogger<RemoteControlService> logger, NetControl netControl, BigMachine bigMachine, RunnerInformation information)
    {
        this.logger = logger;
        this.netControl = netControl;
        this.bigMachine = bigMachine;
        this.information = information;
    }

    public async NetTask Authenticate(AuthenticationToken token)
    {
        if (TransmissionContext.Current.ServerConnection.ValidateAndVerify(token) &&
            token.PublicKey.Equals(this.information.RemotePublicKey))
        {
            this.token = token;
            TransmissionContext.Current.Result = NetResult.Success;
            return;
        }

        TransmissionContext.Current.Result = NetResult.NotAuthorized;
    }

    public async NetTask<NetResult> Restart()
    {
        if (this.token == null)
        {
            return NetResult.NotAuthorized;
        }

        var address = this.information.TryGetDualAddress();
        if (!address.IsValid)
        {
            return NetResult.NoNodeInformation;
        }

        var netTerminal = this.netControl.NetTerminal;
        var netNode = await netTerminal.UnsafeGetNetNodeAsync(address);
        if (netNode is null)
        {
            return NetResult.NoNodeInformation;
        }

        using (var terminal = await netTerminal.TryConnect(netNode))
        {
            if (terminal is null)
            {
                return NetResult.NoNetwork;
            }

            var remoteControl = terminal.GetService<IRemoteControlService>();
            var response = await remoteControl.Authenticate(this.token).ResponseAsync;
            this.logger.TryGet()?.Log($"RequestAuthorization: {response.Result}");
            if (!response.IsSuccess)
            {
                return NetResult.NotAuthorized;
            }

            var result = await remoteControl.Restart();
            this.logger.TryGet()?.Log($"Restart: {result}");
            if (result == NetResult.Success)
            {
                var machine = this.bigMachine.RunnerMachine.Get();
                if (machine != null)
                {
                    _ = machine.Command.Restart();
                }
            }

            return result;
        }
    }

    private ILogger<RemoteControlService> logger;
    private NetControl netControl;
    private BigMachine bigMachine;
    private RunnerInformation information;
    private AuthenticationToken? token;
}
