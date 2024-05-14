// Copyright (c) All contributors. All rights reserved. Licensed under the MIT license.

using Netsphere.Crypto;

namespace LP.T3CS;

[NetServiceInterface]
public partial interface IRelayMergerService : IMergerService
{
}

internal class RelayMerger : Merger
{
    public RelayMerger(SignaturePrivateKey mergerPrivateKey, UnitContext context, ILogger<Merger> logger, LPBase lpBase, ICrystal<CreditData.GoshujinClass> crystal, MergerInformation mergerInformation)
        : base(mergerPrivateKey, context, logger, lpBase, crystal, mergerInformation)
    {
    }
}

[NetServiceObject]
internal class RelayMergerServiceAgent : IRelayMergerService
{
    public RelayMergerServiceAgent(Merger.Provider mergerProvider)
    {
        this.merger = mergerProvider.GetOrException();
    }

    public async NetTask<IMergerService.InformationResult?> GetInformation()
    {
        return this.merger.Information.ToInformationResult();
    }

    public NetTask<T3CSResultAndValue<Credit>> CreateCredit(Merger.CreateCreditParams param)
    {
        if (!TransmissionContext.Current.AuthenticationTokenEquals(param.Proof.PublicKey))
        {
            return new(new T3CSResultAndValue<Credit>(T3CSResult.NotAuthenticated));
        }

        return this.merger.CreateCredit(param);
    }

    private Merger merger;
}
