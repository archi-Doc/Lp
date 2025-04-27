// Copyright (c) All contributors. All rights reserved. Licensed under the MIT license.

using System;
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

    /// <summary>
    /// The array of words used in the seed phrase dictionary.
    /// </summary>
    private static string[] words = [];
    private static uint divisor;

    /// <summary>
    /// The dictionary mapping words to their indices in the seed phrase dictionary.
    /// </summary>
    private static Dictionary<string, ushort> dictionary = new(StringComparer.InvariantCultureIgnoreCase);

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
                        for (ushort i = 0; i < wordsArray.Length; i++)
                        {
                            dictionary.TryAdd(words[i], i);
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
    public static string Create()
    {
        if (divisor == 0 || dictionary == null)
        {
            throw new PanicException();
        }

        var index = new ushort[DefaultNumberOfWords - 1];
        for (var i = 0; i < index.Length; i++)
        {
            index[i] = (ushort)(RandomVault.Default.NextUInt32() % divisor);
        }

        return Create(index);
    }

    public static string Create(ReadOnlySpan<ushort> seedSpan)
    {
        var span = MemoryMarshal.AsBytes<ushort>(seedSpan);
        var checksum = (ushort)(XxHash3.Hash64(span) % divisor);

        var sb = new StringBuilder();
        for (var i = 0; i < seedSpan.Length; i++)
        {
            sb.Append(words[seedSpan[i]]);
            sb.Append(" ");
        }

        // Checksum
        sb.Append(words[checksum]);

        return sb.ToString();
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

        var wordArray = seedphrase.Split(' ');
        if (wordArray.Length < MinimumNumberOfWords)
        {// Minimum length
            return null;
        }

        var index = new ushort[wordArray.Length];
        for (var i = 0; i < wordArray.Length; i++)
        {
            if (!dictionary.TryGetValue(wordArray[i], out var j))
            {
                return null;
            }

            index[i] = j;
        }

        var span = MemoryMarshal.AsBytes<ushort>(index.AsSpan().Slice(0, index.Length - 1));
        var checksum = (uint)(XxHash3.Hash64(span) % divisor);
        if (checksum != index[index.Length - 1])
        {
            return null;
        }

        var seed = Sha3Helper.Get256_ByteArray(Encoding.UTF8.GetBytes(seedphrase));
        return seed;
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
