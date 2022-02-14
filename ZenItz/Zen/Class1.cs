// Copyright (c) All contributors. All rights reserved. Licensed under the MIT license.

namespace ZenItz;

public interface IFlake
{
}

public interface IFragment : IFlake
{
}

[TinyhandUnion(0, typeof(TestFragment))]
public abstract partial class FlakeBase
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

public interface IUnprotected
{
}

public readonly struct ZenItem<TFlake, TUnprotected>
    where TFlake : IFlake
    where TUnprotected : IUnprotected
{
    public readonly TFlake Flake;
    public readonly TUnprotected? Unprotected;
}

[TinyhandObject]
public partial class TestFragment : FlakeBase
{
    // [Key(0, MarkPosition = true)]
    public string Name { get; private set; } = string.Empty;
}

public class TestUnprotected : IUnprotected
{
}
