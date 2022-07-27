// Copyright (c) All contributors. All rights reserved. Licensed under the MIT license.

namespace Arc.Unit;

/// <summary>
/// Interface to display information via <see cref="ILog"/>.
/// </summary>
public interface ILogInformation
{
    /// <summary>
    /// Display information.
    /// </summary>
    /// <param name="logger"><see cref="ILog"/> instance.</param>
    public void LogInformation(ILog logger);
}
