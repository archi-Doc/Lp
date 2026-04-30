// Copyright (c) All contributors. All rights reserved. Licensed under the MIT license.

using System.Runtime.CompilerServices;

namespace Arc.Threading;

public class ExecutionRoot : ExecutionCore
{
#pragma warning disable SA1401 // Fields should be private
#pragma warning disable SA1304 // Non-private readonly fields should begin with upper-case letter

    internal readonly Lock SyncObject = new();
    internal readonly Xoshiro256StarStar Random = new();
    internal readonly Dictionary<long, ExecutionCore> IdToCore = new();

#pragma warning restore SA1304 // Non-private readonly fields should begin with upper-case letter
#pragma warning restore SA1401 // Fields should be private

    public ExecutionRoot()
        : base(0)
    {
    }
}
