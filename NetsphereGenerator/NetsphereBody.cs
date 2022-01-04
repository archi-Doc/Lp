// Copyright (c) All contributors. All rights reserved. Licensed under the MIT license.

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using Arc.Visceral;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Text;

#pragma warning disable RS2008
#pragma warning disable SA1306 // Field names should begin with lower-case letter
#pragma warning disable SA1310 // Field names should not contain underscore
#pragma warning disable SA1401 // Fields should be private
#pragma warning disable SA1117 // Parameters should be on same line or separate lines

namespace Netsphere.Generator;

public class NetsphereBody : VisceralBody<NetsphereObject>
{
    public const string GeneratorName = "NetsphereGenerator";

    public static readonly DiagnosticDescriptor Error_AttributePropertyError = new DiagnosticDescriptor(
        id: "BMG003", title: "Attribute property type error", messageFormat: "The argument specified does not match the type of the property",
        category: GeneratorName, DiagnosticSeverity.Error, isEnabledByDefault: true);

    public static readonly DiagnosticDescriptor Error_KeywordUsed = new DiagnosticDescriptor(
        id: "BMG004", title: "Keyword used", messageFormat: "The type '{0}' already contains a definition for '{1}'",
        category: GeneratorName, DiagnosticSeverity.Error, isEnabledByDefault: true);

    public static readonly DiagnosticDescriptor Error_NotDerived = new DiagnosticDescriptor(
        id: "BMG005", title: "Not derived", messageFormat: "MachineObject '{0}' must be derived from Machine class",
        category: GeneratorName, DiagnosticSeverity.Error, isEnabledByDefault: true);

    public static readonly DiagnosticDescriptor Error_MethodFormat = new DiagnosticDescriptor(
        id: "BMG006", title: "Invalid method", messageFormat: "State method must be in the format of 'protected StateResult TestMethod(StateParameter parameter)'",
        category: GeneratorName, DiagnosticSeverity.Error, isEnabledByDefault: true);

    public static readonly DiagnosticDescriptor Error_OpenGenericClass = new DiagnosticDescriptor(
        id: "BMG007", title: "Not closed generic", messageFormat: "MachineObject '{0}' is not a closed generic class",
        category: GeneratorName, DiagnosticSeverity.Error, isEnabledByDefault: true);

    public static readonly DiagnosticDescriptor Error_DuplicateTypeId = new DiagnosticDescriptor(
        id: "BMG008", title: "Duplicate Service Id", messageFormat: "Service Id '{0}' must be unique",
        category: GeneratorName, DiagnosticSeverity.Error, isEnabledByDefault: true);

    public static readonly DiagnosticDescriptor Error_DuplicateStateId = new DiagnosticDescriptor(
        id: "BMG009", title: "Duplicate state id", messageFormat: "State method id must be unique",
        category: GeneratorName, DiagnosticSeverity.Error, isEnabledByDefault: true);

    public static readonly DiagnosticDescriptor Error_NoDefaultStateMethod = new DiagnosticDescriptor(
        id: "BMG010", title: "No default state method", messageFormat: "Default state method (state method id = 0) is required",
        category: GeneratorName, DiagnosticSeverity.Error, isEnabledByDefault: true);

    public static readonly DiagnosticDescriptor Error_GroupType = new DiagnosticDescriptor(
        id: "BMG011", title: "Group type error", messageFormat: "Group must implement IMachineGroup<TIdentifier> interface",
        category: GeneratorName, DiagnosticSeverity.Error, isEnabledByDefault: true);

    public static readonly DiagnosticDescriptor Error_IdentifierIsNotSerializable = new DiagnosticDescriptor(
        id: "BMG012", title: "Identifier not serializable", messageFormat: "Identifier type '{0}' must be serializable (have TinyhandObject attribute)",
        category: GeneratorName, DiagnosticSeverity.Error, isEnabledByDefault: true);

    public NetsphereBody(SourceProductionContext context)
        : base(context)
    {
    }

    internal Dictionary<uint, NetsphereObject> Interfaces = new();

    internal Dictionary<uint, NetsphereObject> Implementations = new();

    internal Dictionary<string, List<NetsphereObject>> Namespaces = new();

    public void Prepare()
    {
        // Configure objects.
        var array = this.FullNameToObject.Values.ToArray();
        foreach (var x in array)
        {
            x.Configure();
        }

        this.FlushDiagnostic();
        if (this.Abort)
        {
            return;
        }

        array = this.Interfaces.Values.Concat(this.Implementations.Values).ToArray();
        foreach (var x in array)
        {
            x.ConfigureRelation();
        }

        // Check
        foreach (var x in array)
        {
            x.Check();
        }

        this.FlushDiagnostic();
        if (this.Abort)
        {
            return;
        }
    }

    public void Generate(IGeneratorInformation generator, CancellationToken cancellationToken)
    {
        ScopingStringBuilder ssb = new();
        GeneratorInformation info = new();
        List<NetsphereObject> rootObjects = new();

        // Namespace
        foreach (var x in this.Namespaces)
        {
            cancellationToken.ThrowIfCancellationRequested();
            this.GenerateHeader(ssb);
            var ns = ssb.ScopeNamespace(x.Key);

            rootObjects.AddRange(x.Value); // For loader generation

            var firstFlag = true;
            foreach (var y in x.Value)
            {
                if (!firstFlag)
                {
                    ssb.AppendLine();
                }

                firstFlag = false;

                y.Generate(ssb, info); // Primary objects
            }

            var result = ssb.Finalize();

            if (generator.GenerateToFile && generator.TargetFolder != null && Directory.Exists(generator.TargetFolder))
            {
                this.StringToFile(result, Path.Combine(generator.TargetFolder, $"gen.Netsphere.{x.Key}.cs"));
            }
            else
            {
                this.Context?.AddSource($"gen.Netsphere.{x.Key}", SourceText.From(result, Encoding.UTF8));
                this.Context2?.AddSource($"gen.Netsphere.{x.Key}", SourceText.From(result, Encoding.UTF8));
            }
        }

        cancellationToken.ThrowIfCancellationRequested();
        this.GenerateLoader(generator, info, rootObjects);

        this.FlushDiagnostic();
    }

    private void GenerateHeader(ScopingStringBuilder ssb)
    {
        ssb.AddHeader("// <auto-generated/>");
        ssb.AddUsing("System");
        ssb.AddUsing("System.Collections.Generic");
        ssb.AddUsing("System.Diagnostics.CodeAnalysis");
        ssb.AddUsing("System.Runtime.CompilerServices");
        ssb.AddUsing("Netsphere");

        ssb.AppendLine("#nullable enable", false);
        ssb.AppendLine("#pragma warning disable CS1591", false);
        ssb.AppendLine();
    }

    private void GenerateLoader(IGeneratorInformation generator, GeneratorInformation info, List<NetsphereObject> rootObjects)
    {
        var ssb = new ScopingStringBuilder();
        this.GenerateHeader(ssb);

        using (var scopeFormatter = ssb.ScopeNamespace("Netsphere.Generator"))
        {
            using (var methods = ssb.ScopeBrace("static class Generated"))
            {
                info.FinalizeBlock(ssb);

                NetsphereObject.GenerateLoader(ssb, info, null, rootObjects);
            }
        }

        this.GenerateInitializer(generator, ssb, info);

        var result = ssb.Finalize();

        if (generator.GenerateToFile && generator.TargetFolder != null && Directory.Exists(generator.TargetFolder))
        {
            this.StringToFile(result, Path.Combine(generator.TargetFolder, "gen.NetsphereGenerated.cs"));
        }
        else
        {
            this.Context?.AddSource($"gen.NetsphereLoader", SourceText.From(result, Encoding.UTF8));
            this.Context2?.AddSource($"gen.NetsphereLoader", SourceText.From(result, Encoding.UTF8));
        }
    }

    private void GenerateInitializer(IGeneratorInformation generator, ScopingStringBuilder ssb, GeneratorInformation info)
    {
        // Namespace
        var ns = "Netsphere";
        var assemblyId = string.Empty; // Assembly ID
        if (!string.IsNullOrEmpty(generator.CustomNamespace))
        {// Custom namespace.
            ns = generator.CustomNamespace;
        }
        else
        {// Other (Apps)
         // assemblyId = "_" + generator.AssemblyId.ToString("x");
            if (!string.IsNullOrEmpty(generator.AssemblyName))
            {
                assemblyId = VisceralHelper.AssemblyNameToIdentifier("_" + generator.AssemblyName);
            }
        }

        info.ModuleInitializerClass.Add("BigMachines.Generator.Generated");

        ssb.AppendLine();
        using (var scopeCrossLink = ssb.ScopeNamespace(ns!))
        using (var scopeClass = ssb.ScopeBrace("public static class BigMachinesModule" + assemblyId))
        {
            ssb.AppendLine("private static bool Initialized;");
            ssb.AppendLine();
            ssb.AppendLine("[ModuleInitializer]");

            using (var scopeMethod = ssb.ScopeBrace("public static void Initialize()"))
            {
                ssb.AppendLine("if (Initialized) return;");
                ssb.AppendLine("Initialized = true;");
                ssb.AppendLine();

                foreach (var x in info.ModuleInitializerClass)
                {
                    ssb.Append(x, true);
                    ssb.AppendLine(".RegisterBM();", false);
                }
            }
        }
    }
}
