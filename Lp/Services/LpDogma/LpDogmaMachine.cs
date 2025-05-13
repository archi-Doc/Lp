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
            var result = await this.ProcessCredential(x, CredentialKind.Merger);
            if (result == StateResult.Terminate)
            {
                return StateResult.Terminate;
            }
        }

        foreach (var x in this.lpDogma.Linkers)
        {
            var result = await this.ProcessCredential(x, CredentialKind.Linker);
            if (result == StateResult.Terminate)
            {
                return StateResult.Terminate;
            }
        }

        foreach (var x in this.lpDogma.Links)
        {
            var result = await this.ProcessLinkage(x);
            if (result == StateResult.Terminate)
            {
                return StateResult.Terminate;
            }
        }

        return StateResult.Continue;
    }

    private async Task<StateResult> ProcessCredential(LpDogma.Credential credentialNode, CredentialKind credentialKind)
    {
        if (this.CancellationToken.IsCancellationRequested ||
            this.lpSeedKey is null)
        {
            return StateResult.Terminate;
        }

        if (this.credentials.Nodes.TryGet(credentialNode.PublicKey, out _))
        {
            // this.userInterfaceService.WriteLine($"{credentialNode.MergerKey.ToString()} -> valid");
            return StateResult.Continue;
        }

        if (MicsRange.FromPastToFastCorrected(Mics.FromMinutes(10)).IsWithin(credentialNode.UpdatedMics))
        {
            return StateResult.Continue;
        }
        else
        {
            credentialNode.UpdatedMics = Mics.FastCorrected;
        }

        var netNode = credentialNode.NetNode;
        using (var connection = await this.netTerminal.Connect(netNode))
        {
            if (connection is null)
            {
                this.userInterfaceService.WriteLine($"Could not connect to {netNode.ToString()}");
                return StateResult.Continue;
            }

            var service = connection.GetService<LpDogmaNetService>();
            var auth = AuthenticationToken.CreateAndSign(this.lpSeedKey, connection);
            var r = await service.Authenticate(auth).ResponseAsync;

            var token = CertificateToken<Value>.CreateAndSign(new Value(credentialNode.PublicKey, 1, LpConstants.LpCredit), this.lpSeedKey, connection);
            var credentialProof = await service.CreateCredentialProof(token, credentialKind);
            if (credentialProof is null ||
                !credentialProof.ValidateAndVerify() ||
                !credentialProof.GetSignatureKey().Equals(credentialNode.PublicKey) ||
                !credentialProof.Value.Equals(token.Target))
            {
                return StateResult.Continue;
            }

            if (CredentialEvidence.TryCreate(credentialProof, this.lpSeedKey, out var evidence) &&
                this.credentials.Nodes.TryAdd(evidence))
            {
                _ = service.AddCredentialEvidence(evidence);
                this.logger.TryGet()?.Log($"Added: {evidence.ToString()}");
            }

            return StateResult.Continue;
        }
    }

    private async Task<StateResult> ProcessLinkage(LpDogma.Link link)
    {
        if (this.CancellationToken.IsCancellationRequested ||
            this.lpSeedKey is null)
        {
            return StateResult.Terminate;
        }

        /*if (this.credentials.LinkerCredentials.CredentialKeyChain.FindFirst(credentialNode.PublicKey) is not null)
        {
            // this.userInterfaceService.WriteLine($"{credentialNode.MergerKey.ToString()} -> valid");
            return StateResult.Continue;
        }*/

        if (!this.ValidateMergers(link.Credit1) || !this.ValidateMergers(link.Credit2) ||
            !link.LinkerPublicKey.Validate() || !this.credentials.Nodes.TryGet(link.LinkerPublicKey, out var credentialEvidence))
        {
            return StateResult.Continue;
        }

        if (MicsRange.FromPastToFastCorrected(Mics.FromMinutes(10)).IsWithin(link.UpdatedMics))
        {
            return StateResult.Continue;
        }
        else
        {
            //linkage.UpdatedMics = Mics.FastCorrected;
        }

        SignaturePublicKey[] publicKeys = [.. link.Credit1.Mergers, .. link.Credit2.Mergers, link.LinkerPublicKey,];
        if (publicKeys.Any(x => !x.Validate()))
        {
            return StateResult.Continue;
        }

        var value1 = new Value(LpConstants.LpPublicKey, 0, link.Credit1);
        var proof1 = new LinkProof(link.LinkerPublicKey, value1);
        var value2 = new Value(LpConstants.LpPublicKey, 0, link.Credit2);
        var proof2 = new LinkProof(link.LinkerPublicKey, value2);
        this.lpSeedKey.TrySign(proof1, CredentialProof.LpExpirationMics);
        this.lpSeedKey.TrySign(proof2, CredentialProof.LpExpirationMics);
        var linkage = new Linkage(proof1, proof2);

        var linkerState = credentialEvidence.CredentialProof.State;
        if (!linkerState.IsValid)
        {
            return StateResult.Continue;
        }

        /*var netNode = linkerState.NetNode;
        using (var connection = await this.netTerminal.Connect(netNode))
        {
            if (connection is null)
            {
                this.userInterfaceService.WriteLine($"Could not connect to {netNode.ToString()}");
                return StateResult.Continue;
            }

            var service = connection.GetService<LpDogmaNetService>();
            var auth = AuthenticationToken.CreateAndSign(this.lpSeedKey, connection);
            var r = await service.Authenticate(auth).ResponseAsync;

            var token = CertificateToken<Value>.CreateAndSign(new Value(credentialNode.PublicKey, 1, LpConstants.LpCredit), this.lpSeedKey, connection);
            var credentialProof = await service.CreateLinkerCredentialProof(token);
            if (credentialProof is null ||
                !credentialProof.ValidateAndVerify() ||
                !credentialProof.GetSignatureKey().Equals(credentialNode.PublicKey))
            {
                return StateResult.Continue;
            }

            if (CredentialEvidence.TryCreate(credentialProof, this.lpSeedKey, out var evidence) &&
                this.credentials.LinkerCredentials.TryAdd(evidence))
            {
                _ = service.AddLinkerCredentialEvidence(evidence);
                this.logger.TryGet()?.Log($"Linker credential for {credentialNode.PublicKey.ToString()} has been created and added.");
            }

            return StateResult.Continue;
        }
    }*/

        return StateResult.Continue;
    }

    private bool ValidateMergers(Credit credit)
    {
        foreach (var x in credit.Mergers)
        {
            if (!x.Validate() ||
                !this.credentials.Nodes.TryGet(x, out _))
            {
                return false;
            }
        }

        return true;
    }
}
