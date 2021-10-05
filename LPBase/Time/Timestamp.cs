// Copyright (c) All contributors. All rights reserved. Licensed under the MIT license.

using System.Diagnostics;

namespace LP;

public static class Timestamp
{
    public static long Get() => Stopwatch.GetTimestamp();
}
