// Copyright (c) All contributors. All rights reserved. Licensed under the MIT license.

using System.Net;
using Lp.Logging;
using Lp.T3cs;
using Netsphere.Crypto;
using Netsphere.Packet;

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
            var result = await this.ProcessLink(x);
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

        if (this.credentials.Nodes.TryGet(credentialNode.PublicKey, out var credentialEvidence) &&
            credentialEvidence.Proof.Value.Point == credentialNode.Point)
        {
            // this.userInterfaceService.WriteLine($"{credentialNode.MergerKey.ToString()} -> valid");
            return StateResult.Continue;
        }

        if (!this.lpBase.Options.TestFeatures &&
            MicsRange.FromPastToFastCorrected(Mics.FromMinutes(10)).IsWithin(credentialNode.UpdatedMics))
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
            var authToken = AuthenticationToken.CreateAndSign(this.lpSeedKey, connection);
            var r = await service.Authenticate(authToken).ResponseAsync;

            var value = new Value(credentialNode.PublicKey, credentialNode.Point, LpConstants.LpCredit);
            var credentialProof = await service.CreateCredentialProof(value, credentialKind);
            if (credentialProof is null ||
                !credentialProof.ValidateAndVerify() ||
                !credentialProof.GetSignatureKey().Equals(credentialNode.PublicKey) ||
                !credentialProof.Value.Equals(value))
            {
                return StateResult.Continue;
            }

            credentialEvidence = new CredentialEvidence(credentialProof);
            if (!this.lpSeedKey.TrySign(credentialEvidence))
            {
                return StateResult.Continue;
            }

            if (this.credentials.Nodes.TryAdd(credentialEvidence))
            {
                _ = service.AddCredentialEvidence(credentialEvidence);
                this.logger.TryGet()?.Log($"Added: {credentialEvidence.ToString()}");
            }

            return StateResult.Continue;
        }
    }

    private async Task<StateResult> ProcessLink(LpDogma.Link link)
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

        if (!this.credentials.Nodes.CheckAuthorization(link.Credit1) ||
            !this.credentials.Nodes.CheckAuthorization(link.Credit2) ||
            !this.credentials.Nodes.CheckAuthorization(link.LinkerPublicKey))
        {
            return StateResult.Continue;
        }

        if (!this.lpBase.Options.TestFeatures &&
            MicsRange.FromPastToFastCorrected(Mics.FromMinutes(10)).IsWithin(link.UpdatedMics))
        {
            return StateResult.Continue;
        }
        else
        {
            link.UpdatedMics = Mics.FastCorrected;
        }

        // Linkage x Point
        var value1 = new Value(LpConstants.LpPublicKey, 1, link.Credit1); // LpKey#1@Credit1
        var proof1 = new LinkProof(value1, link.LinkerPublicKey); // @Credit + Linker
        var value2 = new Value(LpConstants.LpPublicKey, 1, link.Credit2); // LpKey#1@Credit2
        var proof2 = new LinkProof(value2, link.LinkerPublicKey); // @Credit + Linker
        this.lpSeedKey.TrySign(proof1, LpConstants.LpExpirationMics); // Proof{@Credit + Linker}/LpKey
        this.lpSeedKey.TrySign(proof2, LpConstants.LpExpirationMics);
        var linkedMics = Mics.GetMicsId();
        var evidence1 = new LinkageEvidence(true, linkedMics, proof1, proof2); // Evidence{Proof{@Credit + Linker}/LpKey}/Merger

        this.credentials.Nodes.TryGet(link.Credit1.Mergers[0], out var credentialEvidence);
        evidence1 = await this.ConnectAndRunService<LinkageEvidence>(credentialEvidence.Proof.State.NetNode, service => service.SignLinkageEvidence(evidence1));

        var evidence2 = new LinkageEvidence(false, linkedMics, proof1, proof2); // Evidence{Proof{@Credit + Linker}/LpKey}/Merger
        evidence2 = await this.ConnectAndRunService<LinkageEvidence>(credentialEvidence.Proof.State.NetNode, service => service.SignLinkageEvidence(evidence2));

        if (evidence1 is null || !evidence1.ValidateAndVerify() ||
            evidence2 is null || !evidence2.ValidateAndVerify())
        {
            return StateResult.Continue;
        }

        this.credentials.Nodes.TryGet(link.LinkerPublicKey, out credentialEvidence);
        Linkage.TryCreate(evidence1, evidence2, out var linkage);
        linkage = await this.ConnectAndRunService<Linkage>(credentialEvidence.Proof.State.NetNode, service => service.SignLinkage(linkage));
        this.lpSeedKey.TrySign(linkage, LpConstants.LpExpirationMics);

        return StateResult.Continue;
    }

    private async Task<T?> ConnectAndRunService<T>(NetNode netNode, Func<LpDogmaNetService, NetTask<T?>> func)
    {
        if (this.CancellationToken.IsCancellationRequested ||
            this.lpSeedKey is null)
        {
            return default;
        }

        using (var connection = await this.netTerminal.Connect(netNode))
        {
            if (connection is null)
            {
                this.userInterfaceService.WriteLine($"Could not connect to {netNode.ToString()}");
                return default;
            }

            var service = connection.GetService<LpDogmaNetService>();
            var auth = AuthenticationToken.CreateAndSign(this.lpSeedKey, connection);
            var r = await service.Authenticate(auth);
            if (r.Result != NetResult.Success)
            {
                return default;
            }

            var t = await func(service);
            return t;
        }
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
