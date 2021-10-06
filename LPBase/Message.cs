// Copyright (c) All contributors. All rights reserved. Licensed under the MIT license.

using System.Threading;
using Arc.Threading;

namespace LP;

public static class Message
{// Create instance -> Configure -> LoadAsync -> Start/Stop loop -> SaveAsync (multiple)
    /// <summary>
    /// Configure objects.<br/>
    /// Ready: <see cref="Information"/>.
    /// </summary>
    public record Configure();

    /// <summary>
    /// Deserialize objects.<br/>
    /// Called once after Configure().
    /// </summary>
    public record LoadAsync();

    /// <summary>
    /// Start objects.<br/>
    /// Called once after LoadAsync().
    /// </summary>
    /// <param name="ParentCore">ParentCore.</param>
    public record Start(ThreadCoreBase ParentCore);

    /// <summary>
    /// Stop objects.<br/>
    /// Called once at the start of the LP termination process.
    /// </summary>
    public record Stop();

    /// <summary>
    ///  Serialize objects.<br/>
    ///  Called multiple times in LP life cycle (after Configure()).
    /// </summary>
    public record SaveAsync();
}
