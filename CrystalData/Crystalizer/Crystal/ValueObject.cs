// Copyright (c) All contributors. All rights reserved. Licensed under the MIT license.

using System.Diagnostics.CodeAnalysis;
using Tinyhand;
using Tinyhand.IO;

namespace CrystalData;

public class KeyValueStore<TKey, TValue> : ITinyhandSerialize<KeyValueStore<TKey, TValue>>, ITinyhandReconstruct<KeyValueStore<TKey, TValue>>
{
    public KeyValueStore()
    {
    }

    static void ITinyhandSerialize<KeyValueStore<TKey, TValue>>.Deserialize(ref TinyhandReader reader, scoped ref KeyValueStore<TKey, TValue>? value, TinyhandSerializerOptions options)
    {
    }

    static void ITinyhandSerialize<KeyValueStore<TKey, TValue>>.Serialize(ref TinyhandWriter writer, scoped ref KeyValueStore<TKey, TValue>? value, TinyhandSerializerOptions options)
    {
    }

    static void ITinyhandReconstruct<KeyValueStore<TKey, TValue>>.Reconstruct([NotNull] ref KeyValueStore<TKey, TValue>? value, TinyhandSerializerOptions options)
    {
        value ??= new();
    }
}
