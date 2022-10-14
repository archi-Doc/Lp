// Copyright (c) All contributors. All rights reserved. Licensed under the MIT license.

namespace ZenItz.Obsolete;

public interface IFlake
{
}

public interface IFragment : IFlake
{
}

[TinyhandUnion(0, typeof(TestFragment))]
public abstract partial class FlakeBase
{
    public bool CheckIdentifier(Identifier identifier)
    {
        try
        {
            var bytes = TinyhandSerializer.Serialize(this, TinyhandSerializerOptions.Conditional);
            var id = Identifier.FromReadOnlySpan(bytes);
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
    [Key(0)]
    public string Name { get; private set; } = string.Empty;
}

public class TestUnprotected : IUnprotected
{
}
