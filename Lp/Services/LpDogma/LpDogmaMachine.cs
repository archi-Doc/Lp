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
    private readonly Merger merger;
    private SeedKey? lpSeedKey = default;

    public LpDogmaMachine(IUserInterfaceService consoleSeuserInterfaceServicevice, ILogger<LpDogmaMachine> logger, LpBase lpBase, NetTerminal netTerminal, AuthorityControl authorityControl, LpDogma lpDogma, Credentials credentials, Merger merger)
    {
        this.userInterfaceService = consoleSeuserInterfaceServicevice;
        this.logger = logger;
        this.lpBase = lpBase;
        this.netTerminal = netTerminal;
        this.modestLogger = new(this.logger);
        this.authorityControl = authorityControl;
        this.lpDogma = lpDogma;
        this.credentials = credentials;
        this.merger = merger;

        this.DefaultTimeout = TimeSpan.FromSeconds(3);
    }

    public int Count { get; set; }

    [StateMethod(0)]
    protected async Task<StateResult> Initial(StateParameter parameter)
    {
        if (this.lpBase.BasalServiceCount < BasalServiceThreshold)
        {
            //return StateResult.Continue;
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

        foreach (var x in this.lpDogma.Evols)
        {
            var result = await this.Process(x);
            if (result == StateResult.Terminate)
            {
                return StateResult.Terminate;
            }
        }

        /*foreach (var x in this.lpDogma.Mergers)
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
        }*/

        return StateResult.Continue;
    }

    private async Task<StateResult> Process(LpDogma.Evol evol)
    {
        if (this.CancellationToken.IsCancellationRequested ||
            this.lpSeedKey is null)
        {
            return StateResult.Terminate;
        }

        if (!evol.Validate())
        {
            return StateResult.Continue;
        }

        // Prepare LpCredit
        var result = await this.merger.GetOrCreateCredit(LpConstants.LpIdentity);
        if (result is null)
        {
            this.userInterfaceService.WriteLine($"Failed to create credit: Lp");
            return StateResult.Continue;
        }

        // Evol: LpKey#Point1@LpCredit -> Merger1#Point2@Credit1
        var sourceValue = new Value(LpConstants.LpPublicKey, evol.LpPoint, LpConstants.LpCredit); // LpKey#Point@LpCredit
        var creditIdentity = new CreditIdentity(LpConstants.LpIdentifier, evol.Originator, [evol.Merger]);
        var credit = creditIdentity.ToCredit();
        var destinationValue = new Value(evol.Merger, evol.DestinationPoint, credit); // Merger1#Point2@Credit1
        var proof = new EvolProof(evol.Linker, sourceValue, destinationValue, creditIdentity);

        var creditIdentity = new CreditIdentity(LpConstants.LpIdentifier, evol.Originator, [evol.Merger]);
        Console.WriteLine(creditIdentity.ToString(Alias.Instance));
        Console.WriteLine(creditIdentity.GetIdentifier().ToString(Alias.Instance));

        /*
        var destinationValue = new Value(LpConstants.LpPublicKey, 100, LpConstants.LpCredit); // Merger1#100@Credit1
        SignaturePublicKey linkerPublicKey = default;
        var proof = new EvolProof(linkerPublicKey, sourceValue, destinationValue, default);
        this.lpSeedKey.TrySign(proof, Mics.FromSeconds(19));*/

        return StateResult.Continue;
    }

    /*private async Task<StateResult> ProcessCredential(LpDogma.Credential credentialNode, CredentialKind credentialKind)
    {
        if (this.CancellationToken.IsCancellationRequested ||
            this.lpSeedKey is null)
        {
            return StateResult.Terminate;
        }

        return StateResult.Continue;

        if (this.credentials.Nodes.TryGet(credentialNode.PublicKey, out var credentialEvidence) &&
            credentialEvidence.Proof.Value.Point == credentialNode.Point)
        {
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
    }*/

    /*private async Task<StateResult> ProcessLink(LpDogma.Link link)
    {
        if (this.CancellationToken.IsCancellationRequested ||
            this.lpSeedKey is null)
        {
            return StateResult.Terminate;
        }

        if (this.credentials.LinkerCredentials.CredentialKeyChain.FindFirst(credentialNode.PublicKey) is not null)
        {
            // this.userInterfaceService.WriteLine($"{credentialNode.MergerKey.ToString()} -> valid");
            return StateResult.Continue;
        }

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

        var value1 = new Value(LpConstants.LpPublicKey, 1, link.Credit1); // LpKey#1@Credit1
        var proof1 = new LinkProof(link.LinkerPublicKey, value1); // @Credit + Linker
        var value2 = new Value(LpConstants.LpPublicKey, 1, link.Credit2); // LpKey#1@Credit2
        var proof2 = new LinkProof(link.LinkerPublicKey, value2); // @Credit + Linker
        this.lpSeedKey.TrySign(proof1, LpConstants.LpExpirationMics); // Proof{@Credit + Linker}/LpKey
        this.lpSeedKey.TrySign(proof2, LpConstants.LpExpirationMics);
        var linkedMics = Mics.GetMicsId();

        if (link.Credit1.MergerCount == 0 || link.Credit2.MergerCount == 0)
        {
            return StateResult.Continue;
        }

        var evidence1 = new ContractableEvidence(true, proof1, proof2, linkedMics, LpConstants.LpExpirationMics); // Evidence{Proof{@Credit + Linker}/LpKey}/Merger
        evidence1 = await this.ConnectAndRunService<ContractableEvidence>(link.Credit1.Mergers[0], service => service.SignContractableEvidence(evidence1));
        if (evidence1 is null)
        {
            return StateResult.Continue;
        }

        var evidence2 = new ContractableEvidence(false, proof1, proof2, linkedMics, LpConstants.LpExpirationMics); // Evidence{Proof{@Credit + Linker}/LpKey}/Merger
        evidence2 = await this.ConnectAndRunService<ContractableEvidence>(link.Credit2.Mergers[0], service => service.SignContractableEvidence(evidence2));
        if (evidence2 is null)
        {
            return StateResult.Continue;
        }

        if (!LinkLinkage.TryCreate(evidence1, evidence2, out var linkage))
        {
            return StateResult.Continue;
        }

        linkage = await this.ConnectAndRunService<LinkLinkage>(link.LinkerPublicKey, service => service.SignLinkage(linkage));
        if (linkage is not null)
        {
            var rr = linkage.ValidateAndVerify();
            this.credentials.Links.TryAdd(linkage);
        }

        return StateResult.Continue;
    }*/

    private async Task<T?> ConnectAndRunService<T>(SignaturePublicKey publicKey, Func<LpDogmaNetService, NetTask<T?>> func)
    {
        if (this.CancellationToken.IsCancellationRequested ||
            this.lpSeedKey is null)
        {
            return default;
        }

        if (!this.credentials.Nodes.TryGet(publicKey, out var credentialEvidence))
        {
            return default;
        }

        if (credentialEvidence.Proof.State.NetNode is not { } netNode)
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
            this.userInterfaceService.WriteLine($"{r.Result}");

            if (r.Result != NetResult.Success)
            {
                return default;
            }

            var t = await func(service).ResponseAsync;
            this.userInterfaceService.WriteLine($"{t.ToString()}");
            return t.Value;
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
