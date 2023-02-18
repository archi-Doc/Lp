// Copyright (c) All contributors. All rights reserved. Licensed under the MIT license.

namespace CrystalData;

public partial class Zen<TIdentifier>
{
    public class RootFlake : Flake
    {
        internal RootFlake(Zen<TIdentifier> zen)
            : base(zen, null, default!)
        {
        }

        public override bool IsRemoved => false;
    }
}
