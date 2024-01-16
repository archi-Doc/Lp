// Copyright (c) All contributors. All rights reserved. Licensed under the MIT license.

namespace Netsphere.Net;

internal interface ICongestionControl
{
    int NumberOfGenesInFlight { get; }

    bool IsCongested { get; }

    /// <summary>
    /// Update the state of congestion control and resend if there are packets that require resending.<br/>
    /// When the connection is closed, return false and release the congestion control.
    /// </summary>
    /// <param name="netSender">An instance of <see cref="NetSender"/>.</param>
    /// <param name="elapsedMics">elapsedMics.</param>
    /// <param name="elapsedMilliseconds">elapsedMilliseconds.</param>
    /// <returns>false: Release the congestion control.</returns>
    bool Process(NetSender netSender, long elapsedMics, double elapsedMilliseconds);

    void Report();

    void AddInFlight(SendGene sendGene, long rto);

    void RemoveInFlight(SendGene sendGene);
}
