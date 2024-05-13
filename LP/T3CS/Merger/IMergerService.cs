// Copyright (c) All contributors. All rights reserved. Licensed under the MIT license.

namespace LP.T3CS;

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

    NetTask<T3CSResultAndValue<Credit>> CreateCredit(Merger.CreateCreditParams param);
}

[NetServiceObject]
internal class MergerServiceAgent : IMergerService
{
    public MergerServiceAgent(Merger.Provider mergerProvider)
    {
        this.merger = mergerProvider.GetOrException();
    }

    public async NetTask<IMergerService.InformationResult?> GetInformation()
    {
        return this.merger.Information.ToInformationResult();
    }

    public NetTask<T3CSResultAndValue<Credit>> CreateCredit(Merger.CreateCreditParams param)
    {
        if (TransmissionContext.Current.AuthenticationTokenEquals(param.Proof.PublicKey))
        {
            return new(new T3CSResultAndValue<Credit>(T3CSResult.NotAuthenticated));
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
