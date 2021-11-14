// Copyright (c) All contributors. All rights reserved. Licensed under the MIT license.

using Arc.Crypto;

namespace LP.Net;

internal class GenePool
{
    public GenePool()
    {
        this.OriginalGene = Random.Crypto.NextULong();
    }

    public ulong GetGene()
    {
        if (this.pseudoRandom == null)
        {
            this.pseudoRandom = new(this.OriginalGene);
            return this.OriginalGene;
        }
        else
        {
            return this.pseudoRandom.NextULong();
        }
    }

    public void SetEmbryo(byte[] embryo)
    {
    }

    public ulong OriginalGene { get; }

    private Xoshiro256StarStar? pseudoRandom;
}
