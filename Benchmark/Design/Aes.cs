﻿// Copyright (c) All contributors. All rights reserved. Licensed under the MIT license.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;
using Arc.Collections;

namespace Benchmark.Design
{
    public class Aes128
    {
        public const int KeyBits = 128;
        public const int KeyBytes = KeyBits / 8;
        public const int IVBits = 128;
        public const int IVBytes = IVBits / 8;

        public static ObjectPool<Aes128> ObjectPool { get; } = new(() => new Aes128());

        public Aes128()
        {
            this.Aes = Aes.Create();
            this.Aes.KeySize = KeyBits;

            this.Key = new byte[KeyBytes];
            this.Aes.Key = this.Key;

            this.IV = new byte[IVBytes];
            this.Aes.IV = this.IV;
        }

        public Aes Aes { get; }

        public byte[] Key { get; }

        public byte[] IV { get; }
    }
}
