// Copyright (c) All contributors. All rights reserved. Licensed under the MIT license.

namespace Netsphere;

public static class NetConstants
{
    public const string NodePrivateKeyName = "nodeprivatekey";
    public const int MaxPacketLength = 1432; // 1500 - 60 - 8 = 1432 bytes
    public const int MinPort = 49152; // Ephemeral port 49152 - 60999
    public const int MaxPort = 60999;

    internal const bool LogLowLevelNet = false;
    internal const int AckDelayMics = 10_000; // 10ms
    internal const long ConnectionClosedToDisposalMics = 10_000_000; // 10s
    internal const int TransmissionTimeoutMics = 5_000_000; // 5s
    internal const int TransmissionDisposalMics = 5_000_000; // 5s

    internal const int DefaultRetransmissionTimeoutMics = 500_000; // 500ms
    internal const int SendIntervalMilliseconds = 1;
    internal const int SendIntervalNanoseconds = SendIntervalMilliseconds * 1_000_000;
    internal const int SendCapacityPerRound = 50;
    internal const int InitialSendStreamDelayMilliseconds = 100;
    internal const int MaxSendStreamDelayMilliseconds = 1_000;
    internal const int InitialReceiveStreamDelayMilliseconds = 100;
    internal const int MaxReceiveStreamDelayMilliseconds = 1_000;
    internal const int TerminateTerminalDelayMilliseconds = 100;

    internal static readonly long MicsPerRound = Mics.FromMilliseconds(1);
    internal static readonly TimeSpan DefaultSendTimeout = TimeSpan.FromSeconds(4);
    internal static readonly TimeSpan CreateTransmissionDelay = TimeSpan.FromMilliseconds(100);
}
