// Copyright (c) All contributors. All rights reserved. Licensed under the MIT license.

using System.Diagnostics.CodeAnalysis;

namespace Lp.Data;

internal readonly struct VisceralOperation<T> : IVisceralOperation<T>
{
    internal VisceralOperation(VisceralOperator<T> ope, T instance)
    {
        this.ope = ope;
        this.instance = instance;
    }

    public string[] GetNames() => this.ope.GetNames();

    public bool TrySet<TValue>(string name, TValue value)
        => this.ope.TrySet<TValue>(this.instance, name, value);

    public bool TryGet<TValue>(string name, [MaybeNullWhen(false)] out TValue value)
    {
        if (this.ope.TryGet(this.instance, name, out var v))
        {
            try
            {
                value = (TValue?)v;
                return value != null;
            }
            catch
            {
            }
        }

        value = default;
        return false;
    }

    private readonly VisceralOperator<T> ope;
    private readonly T instance;
}
