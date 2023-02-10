// Copyright (c) All contributors. All rights reserved. Licensed under the MIT license.

using System.Runtime.CompilerServices;
using Tinyhand.IO;

namespace ZenItz;

public partial class Zen<TIdentifier>
{
    [TinyhandObject(ExplicitKeyOnly = true)]
    public partial class DataBase : IData, ITinyhandSerialize<DataBase>
    {
        public static IData StaticNew() => new DataBase();

        public static int StaticId() => 1;

        public static void Serialize(ref TinyhandWriter writer, scoped ref Zen<TIdentifier>.DataBase? value, TinyhandSerializerOptions options)
        {
            var db = value;
            while (db != null)
            {
                writer.Write(db.Id);
                writer.Write(db.file);
                db = db.Next;
            }

            writer.Write((int)0);
        }

        public static void Deserialize(ref TinyhandReader reader, scoped ref Zen<TIdentifier>.DataBase? value, TinyhandSerializerOptions options)
        {
            while (true)
            {
                var id = reader.ReadInt32();
                if (id == 0)
                {
                    break;
                }

                var file = reader.ReadInt64();

            }
        }

        public int Id => StaticId();

#pragma warning disable SA1401 // Fields should be private
        internal DataBase? Next;
#pragma warning restore SA1401 // Fields should be private

        private long file;
    }

    public partial class Data2 : DataBase
    {
        public static new IData New() => new Data2();

        public static new int GetId() => 2;

        public new int Id => GetId();
    }

    internal interface IDataInternal : IData
    {
        void SaveInternal(bool unload);
    }

    public class BlockData : IDataInternal
    {
        public BlockData(Flake flake)
        {
            this.himo = new(flake);
            this.himo.HimoType = Type.FlakeHimo;
        }

        public void SetSpan(ReadOnlySpan<byte> data, bool clearSavedFlag)
        {// lock (Flake.syncObject)
            this.Update(this.flakeData.SetSpanInternal(data), clearSavedFlag);
        }

        public void SetMemoryOwner(ByteArrayPool.ReadOnlyMemoryOwner dataToBeMoved, object? obj, bool clearSavedFlag)
        {// lock (Flake.syncObject)
            this.Update(this.flakeData.SetMemoryOwnerInternal(dataToBeMoved, obj), clearSavedFlag);
        }

        /*public void SetObject(ITinyhandSerialize obj, bool clearSavedFlag)
        {// lock (Flake.syncObject)
            this.Update(this.flakeData.SetObjectInternal(obj), clearSavedFlag);
        }*/

        /*public bool TryGetSpan(out ReadOnlySpan<byte> data)
        {// lock (Flake.syncObject)
            var result = this.flakeData.TryGetSpanInternal(out data);
            this.Update(result.MemoryDifference);
            return result.Result;
        }*/

        public void GetMemoryOwner(out ByteArrayPool.ReadOnlyMemoryOwner memoryOwner)
        {// lock (Flake.syncObject)
            memoryOwner = this.flakeData.MemoryOwner.IncrementAndShare();
            this.himo.Update();
        }

        public ZenResult TryGetObject<T>(out T? obj)
            where T : ITinyhandSerialize<T>
        {// lock (Flake.syncObject)
            var result = this.flakeData.TryGetObjectInternal(out obj);
            this.himo.Update();
            return result;
        }

        void Zen<TIdentifier>.IDataInternal.SaveInternal(bool unload)
        {// lock (this.flake.syncObject)
            if (!this.isSaved)
            {// Not saved.
                var memoryOwner = this.flakeData.MemoryOwner.IncrementAndShare();
                this.Flake.Zen.IO.Save(ref this.Flake.flakeFile, memoryOwner);
                memoryOwner.Return();

                this.isSaved = true;
            }

            if (unload)
            {
                var memoryDifference = this.flakeData.Clear();
                this.Remove(memoryDifference);
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private void Update((bool Changed, int MemoryDifference) result, bool clearSavedFlag)
        {
            if (clearSavedFlag && result.Changed)
            {
                this.isSaved = false;
            }

            this.Update(result.MemoryDifference);
        }

        private HimoGoshujinClass.Himo himo;
        private bool isSaved = true;
        private FlakeData flakeData = new();
    }
}
