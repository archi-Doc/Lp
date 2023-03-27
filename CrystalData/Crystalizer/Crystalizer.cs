// Copyright (c) All contributors. All rights reserved. Licensed under the MIT license.

using System.Diagnostics.CodeAnalysis;

namespace CrystalData;

public class CrystalizerClass
{// tempcode
    public CrystalizerClass()
    {
    }

    public ICrystal<T> Create<T>()
        where T : ITinyhandSerialize<T>, ITinyhandReconstruct<T>
        => new CrystalImpl<T>(this);

    public bool TryGetPolicy<T>([MaybeNullWhen(false)] out CrystalPolicy policy)
        => this.typeToCrystalPolicy.TryGetValue(typeof(T), out policy);

    public void AddPolicy<T>(CrystalPolicy policy)
        => this.typeToCrystalPolicy.TryAdd(typeof(T), policy);

    private ThreadsafeTypeKeyHashTable<CrystalPolicy> typeToCrystalPolicy = new();
}
