// Copyright (c) All contributors. All rights reserved. Licensed under the MIT license.

using System.Diagnostics.CodeAnalysis;

namespace ZenItz;

#pragma warning disable SA1401 // Fields should be private

public partial class Zen<TIdentifier>
{
    internal partial class FlakeData
    {
        public FlakeData(Zen<TIdentifier> zen)
        {
            this.Zen = zen;
        }

        

        public Zen<TIdentifier> Zen { get; }

        public object? Object { get; private set; }

        public bool MemoryOwnerAvailable { get; private set; }

        internal ByteArrayPool.ReadOnlyMemoryOwner MemoryOwner;
    }
}
