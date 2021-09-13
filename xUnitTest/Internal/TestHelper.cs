// Copyright (c) All contributors. All rights reserved. Licensed under the MIT license.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Xunit;

namespace xUnitTest
{
    public static class TestHelper
    {
        public static bool ByteArrayEqual(byte[]? array1, byte[]? array2, int length)
        {
            if (array1 == null || array2 == null)
            {
                return false;
            }
            else if (array1.Length < length || array2.Length < length)
            {
                return false;
            }

            for (var n = 0; n < length; n++)
            {
                if (array1[n] != array2[n])
                {
                    return false;
                }
            }

            return true;
        }
    }
}
