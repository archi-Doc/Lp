// Copyright (c) All contributors. All rights reserved. Licensed under the MIT license.

using Netsphere;
using Netsphere.Crypto;

namespace Playground;

[NetServiceInterface]
public interface ITestService : INetService, INetServiceAgreement
{
    Task<string?> DoubleString(string input);

    NetTask<byte[]?> Pingpong(byte[] data);
}

[NetServiceObject]
internal class TestServiceImpl : ITestService
{
    async Task<string?> ITestService.DoubleString(string input)
        => input + input;

    // [NetServiceFilter<NullFilter>]
    public async NetTask<byte[]?> Pingpong(byte[] data)
    {
        return data;
    }

    async NetTask<NetResult> INetServiceAgreement.UpdateAgreement(CertificateToken<ConnectionAgreement> token)
    {
        var transmissionContext = TransmissionContext.Current;
        if (!transmissionContext.ServerConnection.ValidateAndVerifyWithSalt(token))
        {// Invalid token
            return NetResult.NotAuthenticated;
        }

        return NetResult.Success;
    }
}
