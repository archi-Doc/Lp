// Copyright (c) All contributors. All rights reserved. Licensed under the MIT license.

using System;
using System.Buffers;
using System.Collections.Frozen;
using System.Runtime.InteropServices;
using System.Text;

namespace Lp;

/// <summary>
/// Represents a seed phrase generator and validator.
/// </summary>
public static class Seedphrase
{
    /// <summary>
    /// The default number of words for a seed phrase.
    /// </summary>
    public const int DefaultNumberOfWords = 24; // 11bits x (24-1) = 253 bits

    /// <summary>
    /// The minimum number of words required for a seed phrase.
    /// </summary>
    public const int MinimumNumberOfWords = 16;

    private const string SeedphrasesPath = "Misc.Strings.Seedphrases";

    #region FieldAndProperty

    public static uint Mask => mask;

    /// <summary>
    /// The array of words used in the seed phrase dictionary.
    /// </summary>
    private static string[] words = [];
    private static uint divisor;
    private static uint mask;

    /// <summary>
    /// The dictionary mapping words to their indices in the seed phrase dictionary.
    /// </summary>
    private static Dictionary<string, ushort> dictionary = new(StringComparer.InvariantCultureIgnoreCase);

    private static FrozenDictionary<string, ushort> fastDictionary = FrozenDictionary<string, ushort>.Empty;

    #endregion

    static Seedphrase()
    {
        var assembly = System.Reflection.Assembly.GetExecutingAssembly();
        try
        {
            using (var stream = assembly.GetManifestResourceStream(assembly.GetName().Name + "." + SeedphrasesPath))
            {
                if (stream != null)
                {
                    var wordsArray = TinyhandSerializer.Deserialize<string[]>(stream, TinyhandSerializerOptions.Lz4);
                    if (wordsArray is not null)
                    {
                        words = wordsArray;
                        divisor = (uint)words.Length;
                        mask = divisor - 1;
                        for (ushort i = 0; i < wordsArray.Length; i++)
                        {
                            dictionary.TryAdd(words[i], i);
                        }

                        fastDictionary = dictionary.ToFrozenDictionary(StringComparer.OrdinalIgnoreCase);
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
    public static string Create()
    {
        if (divisor == 0 || dictionary == null)
        {
            throw new PanicException();
        }

        Span<ushort> index = stackalloc ushort[DefaultNumberOfWords - 1];
        for (var i = 0; i < index.Length; i++)
        {
            index[i] = (ushort)(RandomVault.Default.NextUInt32() & mask);
        }

        return Create(index);
    }

    public static string Create(ReadOnlySpan<ushort> seedSpan)
    {
        if (divisor == 0 || dictionary == null)
        {
            throw new PanicException();
        }

        var span = MemoryMarshal.AsBytes<ushort>(seedSpan);
        var checksum = (ushort)(XxHash3.Hash64(span) % divisor);

        var length = words[checksum].Length;
        foreach (var index in seedSpan)
        {
            if (index >= words.Length)
            {
                throw new ArgumentOutOfRangeException(nameof(seedSpan));
            }

            length = checked(length + words[index].Length + 1);
        }

        return string.Create(length, new PhraseState(seedSpan, checksum), static (destination, state) =>
        {
            foreach (var index in state.Indices)
            {
                var word = words[index];
                word.AsSpan().CopyTo(destination);
                destination = destination.Slice(word.Length);
                destination[0] = ' ';
                destination = destination.Slice(1);
            }

            // Checksum
            words[state.Checksum].AsSpan().CopyTo(destination);
        });
    }

    /// <summary>
    /// Tries to get a 32-byte seed (SHA3-256) from the given seed phrase.
    /// </summary>
    /// <param name="seedphrase">The seed phrase.</param>
    /// <returns>A 32-byte seed (SHA3-256) if the phrase is valid; otherwise, null.</returns>
    /// <exception cref="PanicException">Thrown when the words array or dictionary is not initialized.</exception>
    public static byte[]? TryGetSeed(string seedphrase)
    {
        if (words.Length == 0 || dictionary is null)
        {
            throw new PanicException();
        }

        var phrase = seedphrase.AsSpan();
        var wordCount = phrase.Count(' ') + 1;
        if (wordCount < MinimumNumberOfWords)
        {// Minimum length
            return null;
        }

        ushort[]? rentedIndices = null;
        Span<ushort> index = wordCount <= 128
            ? stackalloc ushort[wordCount]
            : (rentedIndices = ArrayPool<ushort>.Shared.Rent(wordCount)).AsSpan(0, wordCount);
        try
        {
            var lookup = dictionary.GetAlternateLookup<ReadOnlySpan<char>>();
            var fastLookup = fastDictionary.GetAlternateLookup<ReadOnlySpan<char>>();
            var i = 0;
            foreach (var range in phrase.Split(' '))
            {
                var word = phrase[range];
                if (!fastLookup.TryGetValue(word, out var value) && !lookup.TryGetValue(word, out value))
                {
                    return null;
                }

                index[i++] = value;
            }

            var span = MemoryMarshal.AsBytes<ushort>(index.Slice(0, index.Length - 1));
            var checksum = (uint)(XxHash3.Hash64(span) % divisor);
            if (checksum != index[index.Length - 1])
            {
                return null;
            }

            var byteCount = Encoding.UTF8.GetByteCount(phrase);
            byte[]? rentedUtf8 = null;
            Span<byte> utf8 = byteCount <= 1024
                ? stackalloc byte[byteCount]
                : (rentedUtf8 = ArrayPool<byte>.Shared.Rent(byteCount)).AsSpan(0, byteCount);
            try
            {
                Encoding.UTF8.GetBytes(phrase, utf8);
                return Sha3Helper.Get256_ByteArray(utf8);
            }
            finally
            {
                utf8.Clear();
                if (rentedUtf8 is not null)
                {
                    ArrayPool<byte>.Shared.Return(rentedUtf8);
                }
            }
        }
        finally
        {
            index.Clear();
            if (rentedIndices is not null)
            {
                ArrayPool<ushort>.Shared.Return(rentedIndices);
            }
        }
    }

    private readonly ref struct PhraseState(ReadOnlySpan<ushort> indices, ushort checksum)
    {
        public ReadOnlySpan<ushort> Indices { get; } = indices;

        public ushort Checksum { get; } = checksum;
    }

    /*
    /// <summary>
    /// Tries to alter the given seed phrase with additional data to produce a new 32-byte seed.
    /// </summary>
    /// <param name="seedphrase">The seed phrase.</param>
    /// <param name="additional">Additional data to alter the seed.</param>
    /// <param name="seed32">The resulting 32-byte seed.</param>
    /// <returns>True if the seed was successfully altered; otherwise, false.</returns>
    /// <exception cref="ArgumentException">Thrown when <paramref name="seed32"/> is not 32 bytes long.</exception>
    public static bool TryAlter(string seedphrase, ReadOnlySpan<byte> additional, Span<byte> seed32)
    {
        if (seed32.Length != 32)
        {
            throw new ArgumentException("seed32 must be 32 bytes long.", nameof(seed32));
        }

        var previousSeed = TryGetSeed(seedphrase);
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
    }*/
}
