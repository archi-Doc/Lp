// Copyright (c) All contributors. All rights reserved. Licensed under the MIT license.

using System.Security.Cryptography.X509Certificates;
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

            var netNode = x.NetNode; // Alternative.NetNode;
            using (var connection = await this.netTerminal.Connect(netNode))
            {
                if (connection is null)
                {
                    this.userInterfaceService.WriteLine($"Could not connect to {netNode.ToString()}");
                    continue;
                }

                var service = connection.GetService<LpDogmaNetService>();
                var auth = AuthenticationToken.CreateAndSign(seedKey, connection);
                var r = await service.Authenticate(auth);

                var mergerKey = await service.GetMergerKey(); // x.MergerKey
                var token = CertificateToken<Value>.CreateAndSign(new Value(mergerKey, 1, LpConstants.LpCredit), seedKey, connection);
                var credentialProof = await service.NewCredentialProof(token);
                if (credentialProof is null ||
                    !credentialProof.ValidateAndVerify() ||
                    !credentialProof.GetSignatureKey().Equals(mergerKey))
                {
                    continue;
                }

                CredentialEvidence.TryCreate(credentialProof, seedKey, out var evidence);
                if (evidence?.ValidateAndVerify() != true)
                {
                    continue;
                }

                this.credentials.MergerCredentials.Add(evidence);
            }
        }

        // var list = this.lpDogma.CredentialNodes.ToList();
        // list.Add(new(Alternative.NetNode, SeedKey.NewSignature().GetSignaturePublicKey(), SeedKey.NewSignature().GetSignaturePublicKey()));
        // this.lpDogma.CredentialNodes = list.ToArray();

        // this.userInterfaceService.WriteLine($"Single: ({this.Identifier.ToString()}) - {this.Count++}");

        return StateResult.Continue;
    }
}
