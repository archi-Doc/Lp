// Copyright (c) All contributors. All rights reserved. Licensed under the MIT license.

using Arc.Crypto;
using Arc.Threading;
using Arc.Unit;
using BigMachines;
using Netsphere;
using Netsphere.Crypto;
using Netsphere.Interfaces;
using SimpleCommandLine;

namespace Netsphere.Runner;

[SimpleCommand("run", Default = true)]
public class RunCommand : ISimpleCommandAsync<RunOptions>
{
    public RunCommand(ILogger<RunCommand> logger, BigMachine bigMachine, NetControl netControl)
    {
        this.logger = logger;
        this.bigMachine = bigMachine;
        this.netControl = netControl;

        this.netControl.Services.Register<IRemoteControl>();
    }

    public async Task RunAsync(RunOptions options, string[] args)
    {
        this.PrepareRunOptions(options);

        var runner = this.bigMachine.RunnerMachine.GetOrCreate(options);
        this.bigMachine.Start(ThreadCore.Root);

        while (!((IBigMachine)this.bigMachine).Core.IsTerminated)
        {
            if (!((IBigMachine)this.bigMachine).CheckActiveMachine())
            {
                break;
            }
            else
            {
                await ((IBigMachine)this.bigMachine).Core.WaitForTerminationAsync(1000);
            }
        }

        // await this.bigMachine.Core.WaitForTerminationAsync(-1);
        // await this.runner.Run();
    }

    private void PrepareRunOptions(RunOptions options)
    {
        options.Prepare();

        if (options.NodePrivateKey is null)
        {// 1st Argument, 2nd: Environment variable
            if (CryptoHelper.TryParseFromEnvironmentVariable<NodePrivateKey>(RunOptions.NodePrivateKeyName, out var privateKey))
            {
                options.NodePrivateKey = privateKey;
            }
        }

        if (options.RemotePublicKey.Equals(SignaturePublicKey.Default))
        {// 1st Argument, 2nd: Environment variable
            if (CryptoHelper.TryParseFromEnvironmentVariable<SignaturePublicKey>(RunOptions.RemotePublicKeyName, out var publicKey))
            {
                options.RemotePublicKey = publicKey;
            }
        }
    }

    private ILogger<RunCommand> logger;
    private BigMachine bigMachine;
    private NetControl netControl;
}
