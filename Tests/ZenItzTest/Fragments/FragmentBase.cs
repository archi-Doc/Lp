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
            var byteArray = TinyhandSerializer.Serialize(this);
            var id = Identifier.FromReadOnlySpan(byteArray.AsSpan(0, 4)); // tempcode
            return id.Equals(identifier);
        }
        catch
        {
            return false;
        }
    }
}
