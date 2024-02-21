// Copyright (c) All contributors. All rights reserved. Licensed under the MIT license.

namespace Netsphere;

internal static class NetConstants
{
    public const bool LogLowLevelNet = true;
    public const int AckDelayMics = 10_000; // 10ms
    public const long ConnectionClosedToDisposalMics = 10_000_000; // 10s
    public const int TransmissionTimeoutMics = 5_000_000; // 5s
    public const int TransmissionDisposalMics = 5_000_000; // 5s

    public const int DefaultRetransmissionTimeoutMics = 500_000; // 500ms
    public const int SendIntervalMilliseconds = 1;
    public const int SendIntervalNanoseconds = SendIntervalMilliseconds * 1_000_000;
    public const int SendCapacityPerRound = 50;
    public const int InitialSendStreamDelayMilliseconds = 100;
    public const int MaxSendStreamDelayMilliseconds = 1_000;
    public const int InitialReceiveStreamDelayMilliseconds = 100;
    public const int MaxReceiveStreamDelayMilliseconds = 1_000;

    public const int DefaultSendBufferSize = 1024 * 1024;

    public const int MaxPacketLength = 1432; // 1500 - 60 - 8 = 1432 bytes
    public const int MinPort = 49152; // Ephemeral port 49152 - 60999
    public const int MaxPort = 60999;

    public static readonly long MicsPerRound = Mics.FromMilliseconds(1);
    public static readonly TimeSpan DefaultSendTimeout = TimeSpan.FromSeconds(4);
    public static readonly TimeSpan CreateTransmissionDelay = TimeSpan.FromMilliseconds(100);
}
