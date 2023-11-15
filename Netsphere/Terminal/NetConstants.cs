// Copyright (c) All contributors. All rights reserved. Licensed under the MIT license.

using Netsphere.Time;

namespace Netsphere;

internal static class NetConstants
{
    public const int SendIntervalMilliseconds = 1;
    public const int SendIntervalNanoseconds = SendIntervalMilliseconds * 1_000_000;
    public const int SendCapacityPerRound = 50;
    public const int SendingAckIntervalInMilliseconds = 10;
    public const double ResendWaitMilliseconds = 500;

    public static readonly long MicsPerRound = Mics.FromMilliseconds(1);
    public static readonly double MicsPerRoundRev = 1d / MicsPerRound;
}
