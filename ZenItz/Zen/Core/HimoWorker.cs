// Copyright (c) All contributors. All rights reserved. Licensed under the MIT license.

namespace ZenItz;

#pragma warning disable SA1401 // Fields should be private

public partial class Zen<TIdentifier>
{
    internal class HimoWorker : TaskWorker<HimoWork>
    {
        private const int DeleteNumber = 10;

        public HimoWorker(ThreadCoreBase parent, HimoGoshujinClass himoGoshujin)
            : base(parent, WorkerMethod, true)
        {
            this.himoGoshujin = himoGoshujin;
        }

        private static async Task WorkerMethod(TaskWorker<HimoWork> w, HimoWork work)
        {
            var worker = (HimoWorker)w;
            /*while (true)
            {
                var sizeToDelete = worker.himoGoshujin.TotalSize - worker.himoGoshujin.Zen.Options.MemorySizeLimit;
                if (sizeToDelete < 0)
                {
                    return;
                }
            }*/

            while (worker.himoGoshujin.TotalSize > worker.himoGoshujin.Zen.Options.MemorySizeLimit)
            {
                var count = 0;
                var list = new Identifier[DeleteNumber];
                while (count < DeleteNumber && worker.himoGoshujin.Goshujin.UnloadQueueChain.TryDequeue(out var himo))
                {

                }

                // Unload
                var h = worker.himoGoshujin.Goshujin.UnloadQueueChain.Peek();
                h.Save(true);
            }
        }

        private HimoGoshujinClass himoGoshujin;
    }

    internal class HimoWork : IEquatable<HimoWork>
    {
        public enum Type
        {
            Unload,
        }

        public HimoWork(Type workType)
        {
            this.WorkType = workType;
        }

        public Type WorkType { get; private set; }

        public bool Equals(Zen<TIdentifier>.HimoWork? other)
        {
            if (other == null)
            {
                return false;
            }

            return this.WorkType == other.WorkType;
        }
    }
}
