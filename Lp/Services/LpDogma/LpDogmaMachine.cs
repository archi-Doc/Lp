// Copyright (c) All contributors. All rights reserved. Licensed under the MIT license.

using Lp.Logging;
using Lp.T3cs;
using Netsphere.Crypto;

namespace Lp.Services;

[MachineObject(UseServiceProvider = true)]
public partial class LpDogmaMachine : Machine
{// Control: context.AddSingleton<Machines.RelayPeerMachine>();
    private const int BasalServiceThreshold = 3;

    private readonly IUserInterfaceService userInterfaceService;
    private readonly ModestLogger modestLogger;
    private readonly ILogger logger;
    private readonly LpBase lpBase;
    private readonly NetTerminal netTerminal;
    private readonly AuthorityControl authorityControl;
    private readonly LpDogma lpDogma;
    private readonly Credentials credentials;
    private SeedKey? lpSeedKey = default;

    public LpDogmaMachine(IUserInterfaceService consoleSeuserInterfaceServicevice, ILogger<LpDogmaMachine> logger, LpBase lpBase, NetTerminal netTerminal, AuthorityControl authorityControl, LpDogma lpDogma, Credentials credentials)
    {
        this.userInterfaceService = consoleSeuserInterfaceServicevice;
        this.logger = logger;
        this.lpBase = lpBase;
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
        if (this.lpBase.BasalServiceCount < BasalServiceThreshold)
        {
            return StateResult.Continue;
        }

        if (await this.authorityControl.GetLpSeedKey(null) is not { } seedKey)
        {
            return StateResult.Continue;
        }

        this.lpSeedKey = seedKey;
        // this.modestLogger.Interval(TimeSpan.FromHours(1), Hashed.Dogma.KeyConfirmed, LogLevel.Information)?.Log(Hashed.Dogma.KeyConfirmed);
        this.logger.TryGet(LogLevel.Fatal)?.Log(Hashed.Dogma.KeyConfirmed);

        this.ChangeState(State.Maintain, true);
        return StateResult.Continue;
    }

    [StateMethod(1)]
    protected async Task<StateResult> Maintain(StateParameter parameter)
    {
        if (this.lpSeedKey is null)
        {
            return StateResult.Continue;
        }

        foreach (var x in this.lpDogma.Mergers)
        {
            if (this.CancellationToken.IsCancellationRequested)
            {
                return StateResult.Terminate;
            }

            if (this.credentials.MergerCredentials.TryGet(x.MergerKey, out _))
            {
                // this.userInterfaceService.WriteLine($"{x.MergerKey.ToString()} -> valid");
                continue;
            }

            if (MicsRange.FromPastToFastCorrected(Mics.FromMinutes(10)).IsWithin(x.UpdatedMics))
            {
                continue;
            }
            else
            {
                x.UpdatedMics = Mics.FastCorrected;
            }

            var netNode = x.NetNode;
            using (var connection = await this.netTerminal.Connect(netNode))
            {
                if (connection is null)
                {
                    this.userInterfaceService.WriteLine($"Could not connect to {netNode.ToString()}");
                    continue;
                }

                var service = connection.GetService<LpDogmaNetService>();
                var auth = AuthenticationToken.CreateAndSign(this.lpSeedKey, connection);
                var r = await service.Authenticate(auth).ResponseAsync;

                var token = CertificateToken<Value>.CreateAndSign(new Value(x.MergerKey, 1, LpConstants.LpCredit), this.lpSeedKey, connection);
                var credentialProof = await service.CreateMergerCredentialProof(token);
                if (credentialProof is null ||
                    !credentialProof.ValidateAndVerify() ||
                    !credentialProof.GetSignatureKey().Equals(x.MergerKey))
                {
                    continue;
                }

                if (CredentialEvidence.TryCreate(credentialProof, this.lpSeedKey, out var evidence) &&
                    this.credentials.MergerCredentials.TryAdd(evidence))
                {
                    _ = service.AddMergerCredentialEvidence(evidence);
                    this.logger.TryGet()?.Log($"The credential for {x.MergerKey.ToString()} has been created and added.");
                }
            }
        }

        return StateResult.Continue;
    }
}
