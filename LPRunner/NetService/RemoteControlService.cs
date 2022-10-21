// Copyright (c) All contributors. All rights reserved. Licensed under the MIT license.

using BigMachines;
using LPRunner;
using Netsphere;

namespace LP.NetServices;

[NetServiceObject]
internal class RemoteControlService : IRemoteControlService
{// Remote -> LPRunner
    public RemoteControlService(BigMachine<Identifier> bigMachine, Terminal terminal, RunnerInformation information)
    {
        this.bigMachine = bigMachine;
        this.terminal = terminal;
        this.information = information;
    }

    public async NetTask RequestAuthorization(Token token)
    {
        if (this.information.RemotePublicKey.IsValid() &&
            token.PublicKey.Equals(this.information.RemotePublicKey) &&
            token.ValidateAndVerify())
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
            var remoteControl = terminal.GetService<RemoteControlService>();
            var response = await remoteControl.RequestAuthorization(this.token).ResponseAsync;
            if (!response.IsSuccess)
            {
                return NetResult.NotAuthorized;
            }

            var result = await remoteControl.Restart();
            if (result == NetResult.Success)
            {
                var machine = this.bigMachine.TryGet<RunnerMachine.Interface>(Identifier.Zero);
                if (machine != null)
                {
                    // await machine.ChangeStateAsync(RunnerMachine.State.Check);
                    _ = machine.CommandAndReceiveAsync<object?, NetResult>(RunnerMachine.Command.Restart, null); // tempcode
                }
            }

            return result;
        }
    }

    private BigMachine<Identifier> bigMachine;
    private Terminal terminal;
    private RunnerInformation information;
    private Token? token;
}
