// Copyright (c) All contributors. All rights reserved. Licensed under the MIT license.

namespace ZenItz;

#pragma warning disable SA1401 // Fields should be private

public partial class Zen<TIdentifier>
{
    internal class HimoGoshujinClass
    {
        public HimoGoshujinClass(Zen<TIdentifier> zen)
        {
            this.Zen = zen;
        }

        public Zen<TIdentifier> Zen { get; }

        public Himo.GoshujinClass Goshujin => this.goshujin;

        internal long TotalSize;

        private Himo.GoshujinClass goshujin = new();
    }
}
