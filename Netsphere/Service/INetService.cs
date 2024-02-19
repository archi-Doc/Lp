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
    NetTask<bool> UpdateAgreement(CertificateToken<ConnectionAgreement> token);
}

public interface INetServiceBidirectional
{
    NetTask<bool> ConnectBidirectionally(CertificateToken<ConnectionAgreement>? token);
}
