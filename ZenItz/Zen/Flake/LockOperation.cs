// Copyright (c) All contributors. All rights reserved. Licensed under the MIT license.

using System.Runtime.CompilerServices;

namespace ZenItz;

public partial class Zen<TIdentifier>
{
    public partial class Flake
    {
        public struct LockOperation<TData> : IDisposable
            where TData : IData
        {
            public LockOperation(Flake flake)
            {
                this.flake = flake;
            }

            public bool IsValid => this.data != null;

            public TData? Data => this.data;

            public void Dispose()
            {
                this.Exit();
            }

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            internal bool Enter()
            {
                if (!this.lockTaken)
                {
                    this.lockTaken = this.flake.semaphore.Enter();
                }

                return this.lockTaken;
            }

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            internal void Exit()
            {
                this.data = default;
                if (this.lockTaken)
                {
                    this.flake.semaphore.Exit();
                    this.lockTaken = false;
                }
            }

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            internal void SetData(TData data)
                => this.data = data;

            private readonly Flake flake;
            private TData? data;
            private bool lockTaken;
        }
    }
}
