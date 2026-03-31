// Copyright (c) All contributors. All rights reserved. Licensed under the MIT license.

namespace Lp;

/// <summary>
/// Specifies options for validation operations.
/// </summary>
[Flags]
public enum ValidationOption : int
{
    /// <summary>
    /// Performs standard validation.
    /// </summary>
    Default = 0,

    /// <summary>
    /// Performs validation without checking the expiration date.
    /// </summary>
    IgnoreExpiration = 1,

    /// <summary>
    /// Performs validation without signature verification.<br/>
    /// Use this option before signing.
    /// </summary>
    IgnoreSignatureBeforeSigning = 2,
}
