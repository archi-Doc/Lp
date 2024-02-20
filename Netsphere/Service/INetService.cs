// Copyright (c) All contributors. All rights reserved. Licensed under the MIT license.

using Netsphere.Crypto;

namespace Netsphere;

/// <summary>
/// A base interface for net service.
/// </summary>
public interface INetService
{
}

/// <summary>
/// An interface for adding functions to <see cref="INetService"/> to update the agreement.
/// </summary>
public interface INetServiceAgreement
{
    /// <summary>
    /// Determines whether to allow updates to the agreement.<br/>
    /// Returning <see cref="NetResult.Success"/> will update the agreement on both the Server and Client sides.
    /// </summary>
    /// <param name="token">A token.</param>
    /// <returns><see cref="NetResult.Success"/> to update the agreement.</returns>
    NetTask<NetResult> UpdateAgreement(CertificateToken<ConnectionAgreement> token);
}

/// <summary>
/// An interface for adding functions to <see cref="INetService"/> to enable bidirectional connection.
/// </summary>
public interface INetServiceBidirectional
{
    /// <summary>
    /// Determines whether to enable bidirectional connection.<br/>
    /// Returning <see cref="NetResult.Success"/> prepares the connection on both the Server and Client sides.
    /// </summary>
    /// <param name="token">A token.</param>
    /// <returns><see cref="NetResult.Success"/> to enable bidirectional connection.</returns>
    NetTask<NetResult> ConnectBidirectionally(CertificateToken<ConnectionAgreement>? token);
}
