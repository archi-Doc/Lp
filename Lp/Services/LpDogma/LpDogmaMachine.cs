// Copyright (c) All contributors. All rights reserved. Licensed under the MIT license.

using Lp.Logging;
using Lp.T3cs;
using Netsphere.Crypto;

namespace Lp.Services;

[MachineObject(UseServiceProvider = true)]
public partial class LpDogmaMachine : Machine
{// Control: context.AddSingleton<Machines.RelayPeerMachine>();
    private readonly IUserInterfaceService userInterfaceService;
    private readonly ModestLogger modestLogger;
    private readonly ILogger logger;
    private readonly NetTerminal netTerminal;
    private readonly AuthorityControl authorityControl;
    private readonly LpDogma lpDogma;
    private readonly Credentials credentials;

    public LpDogmaMachine(IUserInterfaceService consoleSeuserInterfaceServicevice, ILogger<LpDogmaMachine> logger, NetTerminal netTerminal, AuthorityControl authorityControl, LpDogma lpDogma, Credentials credentials)
    {
        this.userInterfaceService = consoleSeuserInterfaceServicevice;
        this.logger = logger;
        this.netTerminal = netTerminal;
        this.modestLogger = new(this.logger);
        this.authorityControl = authorityControl;
        this.lpDogma = lpDogma;
        this.credentials = credentials;

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

        foreach (var x in this.lpDogma.CredentialNodes)
        {
            if (this.CancellationToken.IsCancellationRequested)
            {
                return StateResult.Terminate;
            }

            if (this.credentials.MergerCredentials.TryGet(x.MergerKey, out _))
            {
                this.userInterfaceService.WriteLine($"{x.MergerKey.ToString()} -> valid");
                continue;
            }

            //x.NetNode
            using (var connection = await this.netTerminal.Connect(Alternative.NetNode))
            {
                if (connection is null)
                {
                    this.userInterfaceService.WriteLine($"Could not connect to {x.NetNode.ToString()}");
                    continue;
                }

                var service = connection.GetService<LpDogmaNetService>();
                var mk = await service.GetMergerKey();
                var token = AuthenticationToken.CreateAndSign(seedKey, connection);
                var r = await service.Authenticate(token);
                mk = await service.GetMergerKey();
            }
        }

        // var list = this.lpDogma.CredentialNodes.ToList();
        // list.Add(new(Alternative.NetNode, SeedKey.NewSignature().GetSignaturePublicKey(), SeedKey.NewSignature().GetSignaturePublicKey()));
        // this.lpDogma.CredentialNodes = list.ToArray();

        // this.userInterfaceService.WriteLine($"Single: ({this.Identifier.ToString()}) - {this.Count++}");

        return StateResult.Continue;
    }
}
