// Copyright (c) All contributors. All rights reserved. Licensed under the MIT license.

namespace LP.Logging;

public class StreamLoggerOptions : FileLoggerOptions
{
    /// <summary>
    /// Gets or sets a value indicating whether or not to clear logs at startup.
    /// </summary>
    public bool ClearLogsOnStartup { get; set; } = true;

    /// <summary>
    /// Gets or sets the upper limit of log stream.
    /// </summary>
    public int MaxStreamCapacity { get; set; } = 10;
}
