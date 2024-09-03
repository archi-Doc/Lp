﻿// Copyright (c) All contributors. All rights reserved. Licensed under the MIT license.

namespace Netsphere;

public static class NetConstants
{
    public const string NodeName = "NodeName";
    public const string NodePrivateKeyName = "NodePrivatekey";
    public const string NodePublicKeyName = "NodePublickey";
    public const string RemotePrivateKeyName = "RemotePrivatekey";
    public const string RemotePublicKeyName = "RemotePublickey";
    public const string OperationPrivateKeyName = "OperationPrivatekey";
    public const string OperationPublicKeyName = "OperationPublickey";

    public const int MaxPacketLength = 1350; // 1500 - 60 - 8 = 1432 bytes
    public const int RelayLength = 48; // RelayHeader(32) + Padding(0-15)
    public const int MinPort = 49152; // Ephemeral port 49152 - 60999
    public const int MaxPort = 60999;
    public const int IntegralityDefaultPoolSize = 4;

    internal const bool LogLowLevelNet = true;
    internal const int AckDelayMics = 10_000; // 10ms
    internal const long ConnectionClosedToDisposalMics = 10_000_000; // 10s
    internal const int TransmissionTimeoutMics = 5_000_000; // 5s
    internal const int TransmissionDisposalMics = 5_000_000; // 5s
    internal const int WaitIntervalMilliseconds = 200; // 200ms

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
    internal static readonly TimeSpan CreateTransmissionDelay = TimeSpan.FromMilliseconds(100);
    internal static readonly TimeSpan WaitIntervalTimeSpan = TimeSpan.FromMilliseconds(WaitIntervalMilliseconds);
    internal static readonly TimeSpan DefaultPacketTransmissionTimeout = TimeSpan.FromSeconds(2);
    internal static readonly TimeSpan DefaultTransmissionTimeout = TimeSpan.FromSeconds(4);
}
