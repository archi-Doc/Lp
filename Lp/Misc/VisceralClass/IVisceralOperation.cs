// Copyright (c) All contributors. All rights reserved. Licensed under the MIT license.

using System.Diagnostics.CodeAnalysis;

namespace Lp.Data;

public interface IVisceralOperation<T>
{
    public string[] GetNames();

    public bool TryGet<TValue>(string name, [MaybeNullWhen(false)] out TValue value);

    public bool TrySet<TValue>(string name, TValue value);
}
