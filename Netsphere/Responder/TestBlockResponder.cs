// Copyright (c) All contributors. All rights reserved. Licensed under the MIT license.

namespace Netsphere.Responder;

internal class TestBlockResponder : NetResponder<TestBlock, TestBlock>
{
    public static readonly NetResponder<TestBlock, TestBlock> Instance = new TestBlockResponder();

    public override TestBlock? Respond(TestBlock value)
    {
        return null; // temporary value;
    }
}
