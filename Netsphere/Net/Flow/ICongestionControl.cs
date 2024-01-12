// Copyright (c) All contributors. All rights reserved. Licensed under the MIT license.

namespace Netsphere.Net;

internal enum ProcessSendResult
{
    Complete,
    Remaining,
    Congested,
}

internal interface ICongestionControl
{
    // Connection Connection { get; }

    int NumberOfGenesInFlight { get; }

    bool IsCongested { get; }

    /// <summary>
    /// Update the state of congestion control and resend if there are packets that require resending.<br/>
    /// When the connection is closed, return false and release the congestion control.
    /// </summary>
    /// <param name="netSender">An instance of <see cref="NetSender"/>.</param>
    /// <returns>false: Release the congestion control.</returns>
    bool Process(NetSender netSender);

    void Report();

    void AddInFlight(SendGene sendGene, long rto);

    void RemoveInFlight(SendGene sendGene);
}
