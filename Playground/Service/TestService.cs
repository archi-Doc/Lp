// Copyright (c) All contributors. All rights reserved. Licensed under the MIT license.

using Netsphere;
using Netsphere.Crypto;

namespace Playground;

[NetServiceInterface]
public interface ITestService : INetService, INetServiceAgreement
{
    NetTask<string?> DoubleString(string input);

    NetTask<byte[]?> Pingpong(byte[] data);
}

[NetServiceObject]
internal class TestServiceImpl : ITestService
{
    async NetTask<string?> ITestService.DoubleString(string input)
        => input + input;

    public async NetTask<byte[]?> Pingpong(byte[] data)
    {
        TransmissionContext.Current.ServerConnection.Flag = true;
        return data;
    }

    async NetTask<NetResult> INetServiceAgreement.UpdateAgreement(CertificateToken<ConnectionAgreement> token)
    {
        return NetResult.Success;
    }
}
