// Copyright (c) All contributors. All rights reserved. Licensed under the MIT license.

using Netsphere.Crypto;

namespace Netsphere;

/// <summary>
/// A base interface for net service.
/// </summary>
public interface INetService
{
}

public interface INetServiceAgreement
{
    NetTask<NetResult> UpdateAgreement(CertificateToken<ConnectionAgreement> token);
}

public interface INetServiceBidirectional
{
    NetTask<NetResult> ConnectBidirectionally(CertificateToken<ConnectionAgreement>? token);
}
