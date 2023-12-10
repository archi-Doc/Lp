// Copyright (c) All contributors. All rights reserved. Licensed under the MIT license.

using System.Security.Cryptography;

namespace Netsphere;

internal partial class EmbryoControl
{
    [ValueLinkObject(Isolation = IsolationLevel.Serializable)]
    private sealed partial class Embryo
    {
        [Link(Primary = true, Type = ChainType.LinkedList, Name = "Lifespan")]
        public Embryo(NetEndPoint endPoint, ulong referencedMics)
        {
            this.EndPoint = endPoint;
            this.ReferencedMics = referencedMics;
        }

        [Link(Type = ChainType.Unordered, AddValue = false)]
        public NetEndPoint EndPoint { get; private set; }

        public ulong ReferencedMics { get; set; }

        private byte[] embryo = [];
        private Aes? aes;

        public Aes? GetAes()
        {
            var x = this.aes;
            if (x is not null)
            {
                this.aes = default;
            }
            else
            {
                x = Aes.Create();
                x.KeySize = 256;
                if (this.embryo.Length >= 32)
                {
                    x.Key = this.embryo.AsSpan(0, 32).ToArray();
                }
            }

            return x;
        }

        public void ReturnAes(Aes aes)
        {
            if (this.aes is null)
            {
                this.aes = aes;
            }
            else
            {
                aes.Dispose();
                return;
            }
        }
    }

    public EmbryoControl()
    {
    }

    public Aes? TryGetAes(NetEndPoint endPoint, ulong currentMics)
    {
        lock (this.items.SyncObject)
        {
            if (this.items.EndPointChain.TryGetValue(endPoint, out var item))
            {
                this.items.LifespanChain.AddFirst(item);
                item.ReferencedMics = currentMics;

                return item.GetAes();
            }
        }

        return default;
    }

    public void ReturnAes(NetEndPoint endPoint, Aes aes)
    {
        lock (this.items.SyncObject)
        {
            if (this.items.EndPointChain.TryGetValue(endPoint, out var item))
            {
                item.ReturnAes(aes);
            }
        }
    }

    private Embryo.GoshujinClass items = new();
}
