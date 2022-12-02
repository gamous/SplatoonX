﻿using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Linq;
using System.Reflection;
using System.Runtime.Loader;
using System.Text;
using System.Threading.Tasks;
using ECommons.Reflection;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.Text;


namespace Splatoon.SplatoonScripting
{


    internal class Compiler
    {
        internal static Assembly Load(byte[] assembly)
        {
            PluginLog.Information($"Beginning assembly load");
            if(DalamudReflector.TryGetLocalPlugin(out var instance, out var type))
            {
                var loader = type.GetField("loader", ReflectionHelper.AllFlags).GetValue(instance);
                var context = loader.GetFoP<AssemblyLoadContext>("context");
                using var stream = new MemoryStream(assembly);
                try
                {
                    var a = context.LoadFromStream(stream);
                    return a;
                }
                catch(Exception e)
                {
                    e.LogDuo();
                }
            }
            return null;
        } 

        internal static byte[] Compile(string sourceCode, string identity)
        {
            using (var peStream = new MemoryStream())
            {
                var result = GenerateCode(sourceCode, identity).Emit(peStream);

                if (!result.Success)
                {
                    PluginLog.Warning("Compilation done with error.");

                    var failures = result.Diagnostics.Where(diagnostic => diagnostic.IsWarningAsError || diagnostic.Severity == DiagnosticSeverity.Error);

                    foreach (var diagnostic in failures)
                    {
                        PluginLog.Warning($"{diagnostic.Id}: {diagnostic.GetMessage()}");
                    }

                    return null;
                }

                PluginLog.Information("Compilation done without any error.");

                peStream.Seek(0, SeekOrigin.Begin);

                return peStream.ToArray();
            }
        }

        private static CSharpCompilation GenerateCode(string sourceCode, string identity = "Script")
        {
            var codeString = SourceText.From(sourceCode);
            var options = CSharpParseOptions.Default.WithLanguageVersion(LanguageVersion.CSharp9);

            var parsedSyntaxTree = SyntaxFactory.ParseSyntaxTree(codeString, options);

            var references = new List<MetadataReference>();
            foreach (var f in Directory.GetFiles(Path.GetDirectoryName(typeof(object).Assembly.Location), "*", SearchOption.TopDirectoryOnly))
            {
                if (IsValidAssembly(f)) references.Add(MetadataReference.CreateFromFile(f));
            }
            foreach (var f in Directory.GetFiles(Path.GetDirectoryName(typeof(System.Windows.Forms.Form).Assembly.Location), "*", SearchOption.TopDirectoryOnly))
            {
                if (IsValidAssembly(f)) references.Add(MetadataReference.CreateFromFile(f));
            }
            foreach (var f in Directory.GetFiles(Svc.PluginInterface.AssemblyLocation.DirectoryName, "*", SearchOption.AllDirectories))
            {
                if (IsValidAssembly(f)) references.Add(MetadataReference.CreateFromFile(f));
            }
            foreach (var f in Directory.GetFiles(Path.GetDirectoryName(Svc.PluginInterface.GetType().Assembly.Location), "*", SearchOption.AllDirectories))
            {
                if (IsValidAssembly(f)) references.Add(MetadataReference.CreateFromFile(f));
            }

            //PluginLog.Information($"References: {references.Select(x => x.Display).Join(", ")}");

            var id = $"SplatoonScript-{identity}-{Guid.NewGuid()}";
            PluginLog.Information($"Assembly name: {id}");
            return CSharpCompilation.Create(id,
                new[] { parsedSyntaxTree },
                references: references,
                options: new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary,
                    optimizationLevel: OptimizationLevel.Release,
                    assemblyIdentityComparer: DesktopAssemblyIdentityComparer.Default,
                    allowUnsafe: true));
        }

        static bool IsValidAssembly(string path)
        {
            try
            {
                var assembly = AssemblyName.GetAssemblyName(path);
                return true;
            }
            catch (Exception ex)
            {
                return false;
            }
        }
    }
}
