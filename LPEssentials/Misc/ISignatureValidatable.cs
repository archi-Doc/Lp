// Copyright (c) All contributors. All rights reserved. Licensed under the MIT license.

namespace LP;

/// <summary>
/// Validate that object members are appropriate.
/// </summary>
/// <typeparam name="T">.</typeparam>
public interface ISignatureValidatable<T> : IIdentifierValidatable<T>
    where T : ITinyhandSerialize<T>
{
}
