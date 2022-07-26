// Copyright (c) All contributors. All rights reserved. Licensed under the MIT license.

using System.Diagnostics.CodeAnalysis;
using SimpleCommandLine;
using Tinyhand.Tree;

namespace Arc.Unit;

/// <summary>
/// Manages command line arguments..
/// </summary>
public class UnitArguments
{
    public UnitArguments()
    {
    }

    public string RawArguments => this.rawArguments;

    public bool ContainsValue(string value) => this.values.Contains(value);

    public IEnumerable<string> GetValues() => this.values;

    public bool TryGetOption(string option, [MaybeNullWhen(false)] out string value)
    {// this.options.TryGetValue(option.ToLower(), out value);
        foreach (var x in this.options)
        {
            if (x.Key == option)
            {
                value = x.Value;
                return true;
            }
        }

        value = default;
        return false;
    }

    public bool ContainsOption(string option)
    {// this.options.ContainsKey(option.ToLower());
        return this.options.Any(x => x.Key == option);
    }

    public IEnumerable<(string Option, string Value)> GetOptions()
    {
        foreach (var x in this.options)
        {
            yield return (x.Key, x.Value);
        }
    }

    internal void Add(string args)
    {
        this.rawArguments = args;
        string? previousOption = null;

        foreach (var x in args.FormatArguments())
        {
            if (x.IsOptionString())
            {// -option
                if (previousOption != null)
                {
                    ProcessOptionString(previousOption, string.Empty); // Previous option
                    previousOption = null;
                }

                previousOption = x.Trim('-');
            }
            else
            {// value
                if (previousOption != null)
                {// -option value
                    ProcessOptionString(previousOption, ProcessValueString(x));
                    previousOption = null;
                }
                else
                {// value
                    this.values.Add(ProcessValueString(x));
                }
            }
        }

        if (previousOption != null)
        {
            ProcessOptionString(previousOption, string.Empty); // Previous option
        }

        void ProcessOptionString(string option, string value)
        {
            option = option.ToLower();
            this.options.Add(new(option, value));

            /*if (this.options.TryGetValue(option, out var v) && !string.IsNullOrEmpty(v))
            {// Contains a valid value.
                return;
            }

            this.options[option] = value;*/
        }

        static string ProcessValueString(string value)
        {
            if (value.Length >= 2 && value.StartsWith('\"') && value.EndsWith('\"'))
            {
                return value.Substring(1, value.Length - 2);
            }
            else
            {
                return value;
            }
        }
    }

    private string rawArguments = string.Empty;
    private List<string> values = new();
    private List<KeyValuePair<string, string>> options = new();
}
