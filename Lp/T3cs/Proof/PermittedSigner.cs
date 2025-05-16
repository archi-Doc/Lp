// Copyright (c) All contributors. All rights reserved. Licensed under the MIT license.

namespace Lp.T3cs;

/// <summary>
/// Specifies the permitted signers.
/// </summary>
[Flags]
public enum PermittedSigner
{
    /// <summary>
    /// The owner is permitted to sign.
    /// </summary>
    Owner = 1 << 0,

    /// <summary>
    /// The merger is permitted to sign.
    /// </summary>
    Merger = 1 << 1,

    /// <summary>
    /// The LP key is permitted to sign.
    /// </summary>
    LpKey = 1 << 2,
}
