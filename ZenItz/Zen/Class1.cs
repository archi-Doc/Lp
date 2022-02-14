// Copyright (c) All contributors. All rights reserved. Licensed under the MIT license.

namespace ZenItz;

public interface IFlake
{
}

public interface IFragment : IFlake
{
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

public class TestFragment : IFlake
{
}

public class TestUnprotected : IUnprotected
{
}

public struct TestItem : ZenItem<TestFragment, TestUnprotected>
{
}
