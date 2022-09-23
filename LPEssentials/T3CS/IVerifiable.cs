// Copyright (c) All contributors. All rights reserved. Licensed under the MIT license.

namespace LP;

/// <summary>
/// Validate that object members and verify that the signature is appropriate.
/// </summary>
public interface IVerifiable
{
    /// <summary>
    /// Validate that object members and verify that the signature is appropriate.
    /// </summary>
    /// <returns><see langword="true" />: Success.</returns>
    bool ValidateAndVerify();
}
