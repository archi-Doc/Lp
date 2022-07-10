// Copyright (c) All contributors. All rights reserved. Licensed under the MIT license.

using System.Threading;
using Arc.Threading;
using LP;
using LP.Unit;

namespace LPEssentials.Radio;

public static class Message
{// Create instance -> Configure -> LoadAsync -> StartAsync -> StopAsync -> SaveAsync (multiple)
    /// <summary>
    /// Deserialize objects.<br/>
    /// Called once after Configure().<br/>
    /// Throw <see cref="PanicException"/> to abort the procedure.
    /// </summary>
    public record LoadAsync();

    /// <summary>
    /// Start objects.<br/>
    /// Called once after LoadAsync().
    /// </summary>
    /// <param name="ParentCore">ParentCore.</param>
    public record StartAsync(ThreadCoreBase ParentCore);

    /// <summary>
    /// Stop objects.<br/>
    /// Called only once at the beginning of the LP termination process.
    /// </summary>
    public record StopAsync();

    /// <summary>
    ///  Serialize objects.<br/>
    ///  Called multiple times in LP life cycle (after Configure()).
    /// </summary>
    public record SaveAsync();
}
