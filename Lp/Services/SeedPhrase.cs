// Copyright (c) All contributors. All rights reserved. Licensed under the MIT license.

using System.Runtime.InteropServices;
using System.Text;
using Netsphere.Crypto;

namespace Lp;

/// <summary>
/// Represents a seed phrase generator and validator.
/// </summary>
public class Seedphrase
{
    /// <summary>
    /// The default number of the seed phrase.
    /// </summary>
    public const int SeedphraseDefaultNumber = 24; // 11bits x (24-1) = 253 bits

    /// <summary>
    /// The minimum number of the seed phrase.
    /// </summary>
    public const int SeedphraseMinimumNumber = 16;

    /// <summary>
    /// The number of words in the seed phrase dictionary.
    /// </summary>
    public const int NumberOfWords = 2048; // 11bits

    private const string SeedphrasesPath = "Misc.Strings.Seedphrases";

    /// <summary>
    /// Initializes a new instance of the <see cref="Seedphrase"/> class.
    /// </summary>
    public Seedphrase()
    {
        var assembly = System.Reflection.Assembly.GetExecutingAssembly();
        try
        {
            using (var stream = assembly.GetManifestResourceStream(assembly.GetName().Name + "." + SeedphrasesPath))
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

    /// <summary>
    /// Creates a new seed phrase.
    /// </summary>
    /// <returns>A new seed phrase as a string.</returns>
    /// <exception cref="PanicException">Thrown when the words array or dictionary is not initialized.</exception>
    public string Create()
    {
        if (this.words.Length == 0 || this.dictionary == null)
        {
            throw new PanicException();
        }

        var length = SeedphraseDefaultNumber;
        var index = new uint[length - 1];
        for (var i = 0; i < index.Length; i++)
        {
            index[i] = RandomVault.Default.NextUInt32() % (uint)this.words.Length;
        }

        var span = MemoryMarshal.AsBytes<uint>(index);
        var checksum = (uint)XxHash3.Hash64(span) % (uint)this.words.Length;

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

    /// <summary>
    /// Tries to get a 32 bytes seed (SHA3-256) from the given seed phrase.
    /// </summary>
    /// <param name="seedphrase">The seed phrase.</param>
    /// <returns>A 32 bytes seed (SHA3-256) if the phrase is valid; otherwise, null.</returns>
    /// <exception cref="PanicException">Thrown when the words array or dictionary is not initialized.</exception>
    public byte[]? TryGetSeed(string seedphrase)
    {
        if (this.words.Length == 0 || this.dictionary == null)
        {
            throw new PanicException();
        }

        var words = seedphrase.Split(' ');
        if (words.Length < SeedphraseMinimumNumber)
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
        var checksum = (uint)XxHash3.Hash64(span) % (uint)this.words.Length;
        if (checksum != index[index.Length - 1])
        {
            return null;
        }

        var seed = Sha3Helper.Get256_ByteArray(System.Text.Encoding.UTF8.GetBytes(seedphrase));
        return seed;
    }

    public bool TryAlter(string seedphrase, ReadOnlySpan<byte> additional, Span<byte> seed32)
    {
        if (seed32.Length != 32)
        {
            throw new ArgumentException("seed32 must be 32 bytes long.", nameof(seed32));
        }

        var previousSeed = this.TryGetSeed(seedphrase);
        if (previousSeed == null)
        {
            seed32 = default;
            return false;
        }

        Span<byte> hash = stackalloc byte[Blake3.Size];
        using var hasher = Blake3Hasher.New();
        hasher.Update(additional);
        hasher.Update(previousSeed);
        hasher.Finalize(seed32);
        return true;
    }

    private string[] words = Array.Empty<string>();
    private Dictionary<string, uint> dictionary = new(StringComparer.InvariantCultureIgnoreCase);
}
