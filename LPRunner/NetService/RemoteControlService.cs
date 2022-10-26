// Copyright (c) All contributors. All rights reserved. Licensed under the MIT license.

using Arc.Unit;
using BigMachines;
using LPRunner;
using Netsphere;

namespace LP.NetServices;

[NetServiceObject]
internal class RemoteControlService : IRemoteControlService
{// Remote -> LPRunner
    public RemoteControlService(ILogger<RemoteControlService> logger, BigMachine<Identifier> bigMachine, Terminal terminal, RunnerInformation information)
    {
        this.logger = logger;
        this.bigMachine = bigMachine;
        this.terminal = terminal;
        this.information = information;
    }

    public async NetTask RequestAuthorization(Token token)
    {
        if (token.ValidateAndVerify(this.information.RemotePublicKey))
        {
            this.token = token;
            CallContext.Current.Result = NetResult.Success;
            return;
        }

        CallContext.Current.Result = NetResult.NotAuthorized;
    }

    public async NetTask<NetResult> Restart()
    {
        if (this.token == null)
        {
            return NetResult.NotAuthorized;
        }

        var nodeAddress = this.information.TryGetNodeAddress();
        if (nodeAddress == null)
        {
            return NetResult.NoNodeInformation;
        }

        using (var terminal = this.terminal.Create(nodeAddress))
        {
            var remoteControl = terminal.GetService<IRemoteControlService>();
            var response = await remoteControl.RequestAuthorization(this.token).ResponseAsync;
            this.logger.TryGet()?.Log($"RequestAuthorization: {response.Result}");
            if (!response.IsSuccess)
            {
                return NetResult.NotAuthorized;
            }

            var result = await remoteControl.Restart();
            this.logger.TryGet()?.Log($"Restart: {result}");
            if (result == NetResult.Success)
            {
                var machine = this.bigMachine.TryGet<RunnerMachine.Interface>(Identifier.Zero);
                if (machine != null)
                {
                    _ = machine.CommandAsync(RunnerMachine.Command.Restart);
                }
            }

            return result;
        }
    }

    private ILogger<RemoteControlService> logger;
    private BigMachine<Identifier> bigMachine;
    private Terminal terminal;
    private RunnerInformation information;
    private Token? token;
}
