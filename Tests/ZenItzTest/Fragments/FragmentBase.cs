// Copyright (c) All contributors. All rights reserved. Licensed under the MIT license.

using Tinyhand;

namespace LP.Fragments;

[TinyhandUnion(0, typeof(TestFragment))]
public abstract partial class FragmentBase
{
    public bool Check(Identifier identifier)
    {
        try
        {
            var result = TinyhandSerializer.SerializeAndGetMarker(this);
            var id = Identifier.FromReadOnlySpan(result.ByteArray.AsSpan(0, result.MarkerPosition));
            return id.Equals(identifier);
        }
        catch
        {
            return false;
        }
    }
}
