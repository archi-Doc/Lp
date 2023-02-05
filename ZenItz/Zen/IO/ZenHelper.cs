// Copyright (c) All contributors. All rights reserved. Licensed under the MIT license.

namespace ZenItz;

public static class ZenHelper
{
    public static bool IsValidFile(ulong file) => file != 0;

    /// <summary>
    /// Get the directory id (non-zero for a valid value) of the file.
    /// </summary>
    /// <param name="file">The file identifier.</param>
    /// <returns>The directory identifier.</returns>
    public static uint ToDirectoryId(ulong file) => (uint)(file >> 32);

    /// <summary>
    /// Get the snowflake id (non-zero for a valid value) of the file.
    /// </summary>
    /// <param name="file">The file identifier.</param>
    /// <returns>The snowflake identifier.</returns>
    public static uint ToSnowflakeId(ulong file) => (uint)file;

    public static ulong ToFile(uint directoryId, uint snowflakeId) => (ulong)directoryId << 32 | (ulong)snowflakeId;
}
