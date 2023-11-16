// Copyright (c) All contributors. All rights reserved. Licensed under the MIT license.

namespace LP.Data;

public sealed class VisceralClass
{
    private VisceralClass()
    {
    }

    public static IVisceralOperation<T>? TryGet<T>(T instance)
    {
        return new VisceralOperation<T>(Cached<T>.ope, instance);
    }

    internal static class Cached<T>
    {
#pragma warning disable SA1307 // Accessible fields should begin with upper-case letter
#pragma warning disable SA1401 // Fields should be private
        internal static VisceralOperator<T> ope;

        static Cached()
        {
            ope = new VisceralOperator<T>();
        }
    }
}
