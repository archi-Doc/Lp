// Copyright (c) All contributors. All rights reserved. Licensed under the MIT license.

namespace LP.Fragments;

[TinyhandUnion(0, typeof(TestFragment))]
public abstract partial class FragmentBase
{
    public bool CheckIdentifier(Identifier identifier)
    {
        try
        {
            var bytes = TinyhandSerializer.Serialize(this, TinyhandSerializerOptions.Signature);
            var id = Identifier.FromReadOnlySpan(bytes);
            return id.Equals(identifier);
        }
        catch
        {
            return false;
        }
    }
}
