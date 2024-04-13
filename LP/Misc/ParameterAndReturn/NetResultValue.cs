// Copyright (c) All contributors. All rights reserved. Licensed under the MIT license.

using System.Runtime.CompilerServices;

namespace LP;

/// <summary>
/// Represents a T3CS result and value.
/// </summary>
/// <typeparam name="TValue">The type of the value.</typeparam>
[TinyhandObject]
public readonly partial struct T3CSResultAndValue<TValue>
{
    public T3CSResultAndValue(T3CSResult result, TValue value)
    {
        this.Result = result;
        this.Value = value;
    }

    public T3CSResultAndValue(TValue value)
    {
        this.Result = T3CSResult.Success;
        this.Value = value;
    }

    public T3CSResultAndValue(T3CSResult result)
    {
        this.Result = result;
        if (typeof(TValue) == typeof(T3CSResult))
        {
            this.Value = Unsafe.As<T3CSResult, TValue>(ref result);
        }
        else
        {
            this.Value = default;
        }
    }

    public bool IsFailure => this.Result != T3CSResult.Success;

    public bool IsSuccess => this.Result == T3CSResult.Success;

    [Key(0)]
    public readonly T3CSResult Result;

    [Key(1)]
    public readonly TValue? Value;

    public override string ToString()
        => $"Result: {this.Result.ToString()}, Value: {this.Value?.ToString()}";
}
