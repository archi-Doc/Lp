// Copyright (c) All contributors. All rights reserved. Licensed under the MIT license.

namespace Lp.T3cs;

[NetServiceInterface]
public partial interface IMergerService : INetService
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

        [Key(0, AddProperty = "MergerName")]
        [MaxLength(16)]
        private string mergerName = default!;
    }

    NetTask<T3csResultAndValue<Credit>> CreateCredit(Merger.CreateCreditParams param);
}

[NetServiceObject]
internal class MergerServiceAgent : IMergerService
{
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

        return this.merger.Information.ToInformationResult();
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

    private Merger merger;
}
