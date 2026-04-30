// Copyright (c) All contributors. All rights reserved. Licensed under the MIT license.

namespace Arc.Threading;

public delegate void ExecutionSignalHandler(ExecutionCore executionCore, ExecutionSignal executionSignal);

public enum ExecutionSignal
{
    Cancel,
    Exit,
}
