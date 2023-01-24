// Copyright (c) All contributors. All rights reserved. Licensed under the MIT license.

namespace ZenItz;

#pragma warning disable SA1401 // Fields should be private

public partial class Zen<TIdentifier>
{
    internal class FlakeObjectGoshujinClass
    {
        public FlakeObjectGoshujinClass(Zen<TIdentifier> zen)
        {
            this.Zen = zen;
        }

        public Zen<TIdentifier> Zen { get; }

        public FlakeObjectBase.GoshujinClass Goshujin => this.goshujin;

        internal long TotalSize;

        private FlakeObjectBase.GoshujinClass goshujin = new();
    }
}
