// Copyright (c) All contributors. All rights reserved. Licensed under the MIT license.

using System.Collections.Concurrent;
using System.Diagnostics.CodeAnalysis;
using Netsphere.Server;

namespace Netsphere;

public sealed class ResponderControl
{
    public ResponderControl()
    {
    }

    private ConcurrentDictionary<ulong, INetResponder> dataIdToResponder = new();

    public void Register<TResponder>(TResponder responder)
        where TResponder : INetResponder
        => this.dataIdToResponder.TryAdd(responder.DataId, responder);

    public bool TryGet(ulong dataId, [MaybeNullWhen(false)] out INetResponder responder)
        => this.dataIdToResponder.TryGetValue(dataId, out responder);
}
