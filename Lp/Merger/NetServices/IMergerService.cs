// Copyright (c) All contributors. All rights reserved. Licensed under the MIT license.

using Netsphere.Crypto;

namespace Lp.T3cs;

[NetServiceInterface]
public partial interface IMergerService : INetServiceWithOwner
{
    NetTask<InformationResult?> GetInformation();

    // [TinyhandObject]
    // public partial record InformationResult([property: Key(0)] string Name);

    [TinyhandObject]
    public partial record InformationResult
    {
        public InformationResult()
        {
        }

        [Key(0)]
        [MaxLength(16)]
        public partial string MergerName { get; set; } = string.Empty;
    }

    NetTask<T3csResultAndValue<Credit>> CreateCredit(Merger.CreateCreditParams param);
}

[NetServiceObject]
internal class MergerServiceAgent : IMergerService
{
    private readonly Merger merger;
    private OwnerToken? ownerToken;
    private OwnerData? ownerData;

    public MergerServiceAgent(Merger merger)
    {
        this.merger = merger;
    }

    public async NetTask<IMergerService.InformationResult?> GetInformation()
    {
        if (!this.merger.Initialized)
        {
            return default;
        }

        return this.merger.Configuration.ToInformationResult();
    }

    public NetTask<T3csResultAndValue<Credit>> CreateCredit(Merger.CreateCreditParams param)
    {
        if (!TransmissionContext.Current.AuthenticationTokenEquals(param.Proof.PublicKey))
        {
            return new(new T3csResultAndValue<Credit>(T3csResult.NotAuthenticated));
        }

        /*if (!TransmissionContext.Current.TryGetAuthenticationToken(out var token))
        {
            return new(new T3CSResultAndValue<Credit>(T3CSResult.NotAuthenticated));
        }

        if (!token.PublicKey.Equals(param.Proof.PublicKey))
        {
            return new(new T3CSResultAndValue<Credit>(T3CSResult.InvalidProof));
        }*/

        return this.merger.CreateCredit(param);
    }

    public async NetTask<NetResult> Authenticate(OwnerToken token)
    {
        var serverConnection = TransmissionContext.Current.ServerConnection;
        if (!token.ValidateAndVerify(serverConnection))
        {
            return NetResult.NotAuthenticated;
        }

        if (token.Credit is null)
        {
            return NetResult.InvalidData;
        }

        var ownerData = await this.merger.FindOwnerData(token);
        if (ownerData is null)
        {
            return NetResult.NotFound;
        }

        this.ownerToken = token;
        this.ownerData = ownerData;

        return NetResult.Success;
    }
}
