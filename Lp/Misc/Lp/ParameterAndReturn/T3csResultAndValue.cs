// Copyright (c) All contributors. All rights reserved. Licensed under the MIT license.

using System.Runtime.CompilerServices;

namespace Lp;

/// <summary>
/// Represents a T3CS result and value.
/// </summary>
/// <typeparam name="TValue">The type of the value.</typeparam>
[TinyhandObject]
public readonly partial struct T3csResultAndValue<TValue>
{
    public T3csResultAndValue(T3csResult result, TValue value)
    {
        this.Result = result;
        this.Value = value;
    }

    public T3csResultAndValue(TValue value)
    {
        this.Result = T3csResult.Success;
        this.Value = value;
    }

    public T3csResultAndValue(T3csResult result)
    {
        this.Result = result;
        if (typeof(TValue) == typeof(T3csResult))
        {
            this.Value = Unsafe.As<T3csResult, TValue>(ref result);
        }
        else
        {
            this.Value = default;
        }
    }

    public bool IsFailure => this.Result != T3csResult.Success;

    public bool IsSuccess => this.Result == T3csResult.Success;

    [Key(0)]
    public readonly T3csResult Result;

    [Key(1)]
    public readonly TValue? Value;

    public override string ToString()
        => $"Result: {this.Result.ToString()}, Value: {this.Value?.ToString()}";
}
