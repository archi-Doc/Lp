// Copyright (c) All contributors. All rights reserved. Licensed under the MIT license.

using System.Collections.Concurrent;
using Netsphere.Crypto;
using Netsphere.Time;

namespace Netsphere;

#pragma warning disable SA1401 // Fields should be private

public class CallContext<TServerContext> : CallContext
    where TServerContext : ServerContext
{
    public CallContext()
    {
    }

    public new TServerContext ServerContext => (TServerContext)base.ServerContext;
}

public class CallContext
{
    public static CallContext Current => CurrentCallContext.Value!;

    public CallContext()
    {
    }

    public ServerContext ServerContext { get; private set; } = default!;

    public ByteArrayPool.MemoryOwner RentData;

    public NetResult Result { get; set; }

    public ulong DataId { get; private set; }

    public DateTime Timestamp { get; private set; }

    public ConcurrentDictionary<string, object> Items
    {
        get
        {
            lock (this.syncObject)
            {
                this.items ??= new();
            }

            return this.items;
        }
    }

    // public Token CreateToken(Token.Type tokenType) => new Token(tokenType, this.ServerContext.Terminal.Salt, Mics.GetCorrected() + Token.DefaultMics, Identifier.Zero);

    internal void Initialize(ServerContext serviceContext, ByteArrayPool.MemoryOwner rentData, ulong dataId)
    {
        this.ServerContext = serviceContext;
        this.RentData = rentData;
        this.DataId = dataId;
        this.Timestamp = DateTime.UtcNow;
    }

    internal static AsyncLocal<CallContext?> CurrentCallContext = new();
    private object syncObject = new();
    private ConcurrentDictionary<string, object>? items;
}
