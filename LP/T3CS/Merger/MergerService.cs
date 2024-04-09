// Copyright (c) All contributors. All rights reserved. Licensed under the MIT license.

using Netsphere;
using Netsphere.Crypto;

namespace LP.NetServices.T3CS;

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

    NetTask<T3CSResult> CreateCredit(Merger.CreateCreditParams param);
}

[NetServiceFilter<MergerOrTestFilter>]
[NetServiceObject]
public class MergerServiceImpl : IMergerService
{// LPCallContext.Current
    public MergerServiceImpl(Merger.Provider mergerProvider)
    {
        this.merger = mergerProvider.GetOrException();
    }

    public async NetTask<IMergerService.InformationResult?> GetInformation()
    {
        return this.merger.Information.ToInformationResult();
    }

    public NetTask<T3CSResult> CreateCredit(Merger.CreateCreditParams param)
    {
        if (!TransmissionContext.Current.ServerConnection.GetContext().TryGetAuthenticationToken(out _))
        {
            return new(T3CSResult.NotAuthorized);
        }

        return this.merger.CreateCredit(param);
    }

    private Merger merger;
}
