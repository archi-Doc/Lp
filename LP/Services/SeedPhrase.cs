// Copyright (c) All contributors. All rights reserved. Licensed under the MIT license.

using System.Runtime.InteropServices;
using System.Text;

namespace LP;

public class Seedphrase
{
    public const int SeedphraseDefaultLength = 16;
    public const int SeedphraseMinimumLength = 12;
    private const string TinyhandPath = "Strings.english.tinyhand";

    public Seedphrase()
    {
        var assembly = System.Reflection.Assembly.GetExecutingAssembly();
        try
        {
            using (var stream = assembly.GetManifestResourceStream(assembly.GetName().Name + "." + TinyhandPath))
            {
                if (stream != null)
                {
                    var words = TinyhandSerializer.Deserialize<string[]>(stream, TinyhandSerializerOptions.Lz4);
                    if (words != null)
                    {
                        this.words = words;
                        for (uint i = 0; i < words.Length; i++)
                        {
                            this.dictionary.TryAdd(this.words[i], i);
                        }
                    }
                }
            }
        }
        catch
        {
        }
    }

    public string Create()
    {
        if (this.words.Length == 0 || this.dictionary == null)
        {
            throw new PanicException();
        }

        var length = SeedphraseDefaultLength;
        var index = new uint[length - 1];
        for (var i = 0; i < index.Length; i++)
        {
            index[i] = Random.Crypto.NextUInt32() % (uint)this.words.Length;
        }

        var span = MemoryMarshal.AsBytes<uint>(index);
        var checksum = (uint)FarmHash.Hash64(span) % (uint)this.words.Length;

        var sb = new StringBuilder();
        for (var i = 0; i < index.Length; i++)
        {
            sb.Append(this.words[index[i]]);
            sb.Append(" ");
        }

        // Checksum
        sb.Append(this.words[checksum]);

        return sb.ToString();
    }

    public byte[]? TryGetSeed(string phrase)
    {
        if (this.words.Length == 0 || this.dictionary == null)
        {
            throw new PanicException();
        }

        var words = phrase.Split(' ');
        if (words.Length < SeedphraseMinimumLength)
        {// Minimum length
            return null;
        }

        var index = new uint[words.Length];
        for (var i = 0; i < words.Length; i++)
        {
            if (!this.dictionary.TryGetValue(words[i], out var j))
            {
                return null;
            }

            index[i] = j;
        }

        var span = MemoryMarshal.AsBytes<uint>(index.AsSpan().Slice(0, index.Length - 1));
        var checksum = (uint)FarmHash.Hash64(span) % (uint)this.words.Length;
        if (checksum != index[index.Length - 1])
        {
            return null;
        }

        var hash = Hash.ObjectPool.Get();
        var seed = hash.GetHash(System.Text.Encoding.UTF8.GetBytes(phrase));
        Hash.ObjectPool.Return(hash);
        return seed;
    }

    private string[] words = Array.Empty<string>();
    private Dictionary<string, uint> dictionary = new(StringComparer.InvariantCultureIgnoreCase);
}
