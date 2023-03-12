// Copyright (c) All contributors. All rights reserved. Licensed under the MIT license.

using Arc.Collections;

namespace LP.Crystal;

internal class DataHimo
{
    public DataHimo(int maxInMemory)
    {
        this.maxInMemory = maxInMemory;
    }

    private UnorderedLinkedList<BaseData> list = new();
    private int maxInMemory;
}
