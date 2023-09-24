// Copyright (c) All contributors. All rights reserved. Licensed under the MIT license.

using LP.T3CS;

namespace LP;

/// <summary>
/// Validate that object members and verify that the signature is appropriate.
/// </summary>
public interface IVerifiable : IValidatable
{
    PublicKey PublicKey { get; }

    byte[] Signature { get; }
}
