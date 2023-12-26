// Copyright (c) All contributors. All rights reserved. Licensed under the MIT license.

namespace Netsphere;

internal static class NetConstants
{
    public const int AckDelayMics = 10_000; // 10ms
    public const long ConnectionOpenToClosedMics = 5_000_000; // 5s
    public const long ConnectionClosedToDisposalMics = 10_000_000; // 10s
    public const int TransmissionTimeoutMics = 5_000_000; // 5s
    public const int TransmissionDisposalMics = 5_000_000; // 5s

    public const int SendIntervalMilliseconds = 1;
    public const int SendIntervalNanoseconds = SendIntervalMilliseconds * 1_000_000;
    public const int SendCapacityPerRound = 50;

    public static readonly long MicsPerRound = Mics.FromMilliseconds(1);
    public static readonly double MicsPerRoundRev = 1d / MicsPerRound;
    public static readonly TimeSpan DefaultSendTimeout = TimeSpan.FromSeconds(4);
    public static readonly TimeSpan CreateTransmissionDelay = TimeSpan.FromMilliseconds(100);
    
}
