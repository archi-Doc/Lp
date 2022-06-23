// Copyright (c) All contributors. All rights reserved. Licensed under the MIT license.

using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Threading;
using System.Xml.Linq;
using Tinyhand;
using Tinyhand.Tree;

namespace Arc.Text;

public class C4
{
    public const int MaxIdLength = 256; // The maximum length of an identifier.
    public const int MaxTextLength = 16 * 1024; // The maximum length of a text.
    public const int MaxSize = 4 * 1024 * 1024; // Max data size, 4MB

    private object cs; // critical section
    private Utf16Hashtable<string> currentCultureTable; // Current culture data (name to string).
    private Utf16Hashtable<string> defaultCultureTable; // Default culture data (name to string).
    private Utf16Hashtable<Utf16Hashtable<string>> cultureTable; // Culture and data (culture to Utf16Hashtable<string>).
    private string defaultCulture; // Default culture
    private CultureInfo currentCulture;

    public C4()
    {
        this.cs = new object();

        var table = new Utf16Hashtable<string>();
        this.currentCultureTable = table;
        this.defaultCultureTable = table;
        this.defaultCulture = this.DefaultCulture;
        this.currentCulture = new CultureInfo(this.DefaultCulture);

        this.cultureTable = new Utf16Hashtable<Utf16Hashtable<string>>();
        this.cultureTable.TryAdd(this.currentCulture.Name, table);
    }

    public static C4 Instance { get; } = new C4();

    public string DefaultCulture => "en-US";

    public string ErrorText => "C4 error"; // Error message.

    public CultureInfo CurrentCulture => this.currentCulture;

    /// <summary>
    /// Get a string that matches the identifier.
    /// </summary>
    /// <param name="identifier">The identifier.</param>
    /// <returns>Returns a string. If no string is found, the return value is the identifier.</returns>
    public string this[string? identifier]
    {
        get
        {
            if (identifier == null)
            {
                return this.ErrorText;
            }

            string? result;
            if (this.currentCultureTable.TryGetValue(identifier, out result))
            {
                return result;
            }

            if (this.currentCultureTable != this.defaultCultureTable && this.defaultCultureTable.TryGetValue(identifier, out result))
            {
                return result;
            }

            return identifier;
        }
    }

    /// <summary>
    /// Get a string that matches the identifier.
    /// </summary>
    /// <param name="identifier">The identifier.</param>
    /// <returns>Returns a string. If no string is found, the return value is null.</returns>
    public string? Get(string? identifier)
    {
        if (identifier == null)
        {
            return this.ErrorText;
        }

        string? result;
        if (this.currentCultureTable.TryGetValue(identifier, out result))
        {
            return result;
        }

        if (this.currentCultureTable != this.defaultCultureTable && this.defaultCultureTable.TryGetValue(identifier, out result))
        {
            return result;
        }

        return null;
    }

    public string ConvertToCultureName(string name) => name switch
    {
        "ja" => "ja-JP",
        "en" => "en-US",
        _ => name,
    };

    /// <summary>
    /// Set the default culture.
    /// </summary>
    /// <param name="defaultCulture">A string of the default culture.</param>
    public void SetDefaultCulture(string defaultCulture)
    {
        defaultCulture = this.ConvertToCultureName(defaultCulture);

        lock (this.cs)
        {
            this.defaultCulture = defaultCulture;

            Utf16Hashtable<string>? table = null;
            if (!this.cultureTable.TryGetValue(defaultCulture, out table))
            {
                table = new Utf16Hashtable<string>();
            }

            Volatile.Write(ref this.defaultCultureTable, table);
        }
    }

    /// <summary>
    /// Change culture.
    /// </summary>
    /// <param name="cultureName">The culture name.</param>
    public void ChangeCulture(string cultureName)
    {
        cultureName = this.ConvertToCultureName(cultureName);

        if (cultureName == this.CurrentCulture.Name)
        {
            return;
        }

        var cultureInfo = new CultureInfo(cultureName);

        lock (this.cs)
        {
            if (!this.cultureTable.TryGetValue(cultureName, out var table))
            {
                throw new CultureNotFoundException();
            }

            Volatile.Write(ref this.currentCultureTable, table);
            Volatile.Write(ref this.currentCulture, cultureInfo);
        }
    }

    /// <summary>
    /// Get a name of the current culture.
    /// </summary>
    /// <returns>A name of the current culture.</returns>
    public string GetCulture() => this.CurrentCulture.Name;

    /// <summary>
    /// Load from a file.
    /// </summary>
    /// <param name="culture">The target culture.</param>
    /// <param name="fileName">The file name.</param>
    /// <param name="clearFlag">Clear the string data and reload.</param>
    public void Load(string culture, string fileName, bool clearFlag = false)
    {
        using (var fs = File.OpenRead(fileName))
        {
            lock (this.cs)
            {
                this.Load(culture, fs, clearFlag);
            }
        }
    }

    /// <summary>
    /// Load from stream.
    /// </summary>
    /// <param name="culture">The target culture.</param>
    /// <param name="stream">Stream.</param>
    /// <param name="clearFlag">Clear the string data and reload.</param>
    public void LoadStream(string culture, Stream stream, bool clearFlag = false)
    {
        lock (this.cs)
        {
            this.Load(culture, stream, clearFlag);
        }
    }

#if !NETFX_CORE
    /// <summary>
    /// Load from assembly.
    /// </summary>
    /// <param name="culture">The target culture.</param>
    /// <param name="assemblyname">The assembly name.</param>
    /// <param name="clearFlag">Clear the string data and reload.</param>
    public void LoadAssembly(string culture, string assemblyname, bool clearFlag = false)
    {
        var asm = System.Reflection.Assembly.GetExecutingAssembly();
        using (var stream = asm.GetManifestResourceStream(asm.GetName().Name + "." + assemblyname))
        {
            if (stream == null)
            {
                throw new FileNotFoundException();
            }

            lock (this.cs)
            {
                this.Load(culture, stream, clearFlag);
            }
        }
    }
#endif

    private void Load(string culture, Stream stream, bool clearFlag)
    {
        if (stream.Length > MaxSize)
        {
            throw new OverflowException();
        }

        using var ms = new MemoryStream();
        stream.CopyTo(ms);
        var group = (Group)TinyhandParser.Parse(ms.ToArray());

        Utf16Hashtable<string>? table = null;
        culture = this.ConvertToCultureName(culture);
        if (!this.cultureTable.TryGetValue(culture, out table))
        {
            table = new Utf16Hashtable<string>();
            this.cultureTable.TryAdd(culture, table);
        }
        else if (clearFlag)
        {// Clear
            table.Clear();
        }

        foreach (var x in group)
        {
            if (x.TryGetLeft_IdentifierUtf16(out var identifier))
            {
                if (x.TryGetRight_Value_String(out var valueString) && valueString.ValueStringUtf16.Length <= MaxTextLength)
                {
                    table.TryAdd(identifier, valueString.ValueStringUtf16);
                }
            }
        }

        if (culture == this.defaultCulture)
        {
            Volatile.Write(ref this.defaultCultureTable, table);
        }

        return;
    }
}
