// Copyright (c) All contributors. All rights reserved. Licensed under the MIT license.

namespace ZenItz;

public record ZenStart(string ZenFile = Zen.DefaultZenFile, string? DefaultFolder = null, bool ForceStart = false);

public record ZenStop(string ZenFile = Zen.DefaultZenFile, string BackupFile = Zen.DefaultZenFile);

public enum ZenStartResult
{
    Success,
    AlreadyStarted,
    FileNotFound,
    ZenFileNotFound,
    ZenFileError,
}
