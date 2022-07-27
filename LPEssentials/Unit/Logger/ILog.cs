// Copyright (c) All contributors. All rights reserved. Licensed under the MIT license.

namespace Arc.Unit;

/// <summary>
/// Interface for log output.<br/>
/// Log levels and log output are fixed.
/// </summary>
public interface ILog
{
    /// <summary>
    /// Send a log to the log output.
    /// </summary>
    /// <param name="eventId">The event id.</param>
    /// <param name="message">The message.</param>
    /// <param name="exception">The exception.</param>
    public void Log(int eventId, string message, Exception? exception = null);

    public Type OutputType { get; }
}
