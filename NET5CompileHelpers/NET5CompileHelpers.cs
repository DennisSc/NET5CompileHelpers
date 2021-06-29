using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.Text;
using System.Runtime.Loader;
using System.Runtime.CompilerServices;


namespace NET5CompileHelpers
{
    public class Compiler
    {
        public byte[] CompileDLLFromString(string code)
        {
            Debug.WriteLine($"Starting compilation of: '{code}'");

            var sourceCode = code;

            using (var peStream = new MemoryStream())
            {
                var result = GenerateCodeDLL(sourceCode).Emit(peStream);

                if (!result.Success)
                {
                    Debug.WriteLine("Compilation done with error.");

                    var failures = result.Diagnostics.Where(diagnostic => diagnostic.IsWarningAsError || diagnostic.Severity == DiagnosticSeverity.Error);

                    foreach (var diagnostic in failures)
                    {
                        Debug.WriteLine("{0}: {1}", diagnostic.Id, diagnostic.GetMessage());
                    }

                    return null;
                }

                Debug.WriteLine("Compilation done without any error.");

                peStream.Seek(0, SeekOrigin.Begin);

                return peStream.ToArray();
            }
        }

        public byte[] CompileFromString(string code)
        {
            Debug.WriteLine($"Starting compilation of: '{code}'");

            var sourceCode = code;

            using (var peStream = new MemoryStream())
            {
                var result = GenerateCodeDLL(sourceCode).Emit(peStream);

                if (!result.Success)
                {
                    Debug.WriteLine("Compilation done with error.");

                    var failures = result.Diagnostics.Where(diagnostic => diagnostic.IsWarningAsError || diagnostic.Severity == DiagnosticSeverity.Error);

                    foreach (var diagnostic in failures)
                    {
                        Debug.WriteLine("{0}: {1}", diagnostic.Id, diagnostic.GetMessage());
                    }

                    return null;
                }

                Debug.WriteLine("Compilation done without any error.");

                peStream.Seek(0, SeekOrigin.Begin);

                return peStream.ToArray();
            }
        }


        public byte[] CompileFromFile(string filepath)
        {
            Debug.WriteLine($"Starting compilation of: '{filepath}'");

            var sourceCode = File.ReadAllText(filepath);

            using (var peStream = new MemoryStream())
            {
                var result = GenerateCode(sourceCode).Emit(peStream);

                if (!result.Success)
                {
                    Debug.WriteLine("Compilation done with error.");

                    var failures = result.Diagnostics.Where(diagnostic => diagnostic.IsWarningAsError || diagnostic.Severity == DiagnosticSeverity.Error);

                    foreach (var diagnostic in failures)
                    {
                        Debug.WriteLine("{0}: {1}", diagnostic.Id, diagnostic.GetMessage());
                    }

                    return null;
                }

                Debug.WriteLine("Compilation done without any error.");

                peStream.Seek(0, SeekOrigin.Begin);

                return peStream.ToArray();
            }
        }


        public byte[] CompileDLLFromFile(string filepath)
        {
            Debug.WriteLine($"Starting compilation of: '{filepath}'");

            var sourceCode = File.ReadAllText(filepath);

            using (var peStream = new MemoryStream())
            {
                var result = GenerateCodeDLL(sourceCode).Emit(peStream);

                if (!result.Success)
                {
                    Debug.WriteLine("Compilation done with error.");

                    var failures = result.Diagnostics.Where(diagnostic => diagnostic.IsWarningAsError || diagnostic.Severity == DiagnosticSeverity.Error);

                    foreach (var diagnostic in failures)
                    {
                        Debug.WriteLine("{0}: {1}", diagnostic.Id, diagnostic.GetMessage());
                    }

                    return null;
                }

                Debug.WriteLine("Compilation done without any error.");

                peStream.Seek(0, SeekOrigin.Begin);

                return peStream.ToArray();
            }
        }



        private static CSharpCompilation GenerateCode(string sourceCode)
        {
            var codeString = SourceText.From(sourceCode);
            var options = CSharpParseOptions.Default.WithLanguageVersion(LanguageVersion.CSharp9);

            var parsedSyntaxTree = SyntaxFactory.ParseSyntaxTree(codeString, options);

            var references = new List<MetadataReference>
            {
                MetadataReference.CreateFromFile(typeof(object).Assembly.Location),
                MetadataReference.CreateFromFile(typeof(Console).Assembly.Location)
            };

            Assembly.GetEntryAssembly()?.GetReferencedAssemblies().ToList()
                .ForEach(a => references.Add(MetadataReference.CreateFromFile(Assembly.Load(a).Location)));

            return CSharpCompilation.Create("Hello.dll",
                new[] { parsedSyntaxTree },
                references: references,
                options: new CSharpCompilationOptions(OutputKind.ConsoleApplication,
                    optimizationLevel: OptimizationLevel.Release,
                    assemblyIdentityComparer: DesktopAssemblyIdentityComparer.Default));
        }

        private static CSharpCompilation GenerateCodeDLL(string sourceCode)
        {
            var codeString = SourceText.From(sourceCode);
            var options = CSharpParseOptions.Default.WithLanguageVersion(LanguageVersion.CSharp9);

            var parsedSyntaxTree = SyntaxFactory.ParseSyntaxTree(codeString, options);

            var references = new List<MetadataReference>
            {
                MetadataReference.CreateFromFile(typeof(object).Assembly.Location),
                MetadataReference.CreateFromFile(typeof(Console).Assembly.Location)
            };

            Assembly.GetEntryAssembly()?.GetReferencedAssemblies().ToList()
                .ForEach(a => references.Add(MetadataReference.CreateFromFile(Assembly.Load(a).Location)));

            return CSharpCompilation.Create("Hello.dll",
                new[] { parsedSyntaxTree },
                references: references,
                options: new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary,
                    optimizationLevel: OptimizationLevel.Release,
                    assemblyIdentityComparer: DesktopAssemblyIdentityComparer.Default));
        }

    }

    class SimpleUnloadableAssemblyLoadContext : AssemblyLoadContext
    {
        public SimpleUnloadableAssemblyLoadContext()
            : base(true)
        {
        }

        protected override Assembly Load(AssemblyName assemblyName)
        {
            return null;
        }


    }



    public class Runner
    {
        public class DLLinfo
        {
            public object instance;
            public MethodInfo EntryPoint;
        }

        public DLLinfo LoadDLL(byte[] pestream, string className, string entrypoint)
        {
            byte[] byteAssembly = pestream;

            var asm = new MemoryStream(byteAssembly);
            var assemblyLoadContext = new SimpleUnloadableAssemblyLoadContext();
            Assembly assembly = assemblyLoadContext.LoadFromStream(asm);


            //Assembly assembly = Assembly.Load(byteAssembly);
            Type exampleClassType = assembly.GetType(className);
            var instance = Activator.CreateInstance(exampleClassType);

            MethodInfo _EntryPoint = exampleClassType.GetMethod(entrypoint);
            //var result = _EntryPoint.Invoke(instance, arg);







            assemblyLoadContext.Unload();
            GC.Collect();
            GC.WaitForPendingFinalizers();


            return new DLLinfo { instance = instance, EntryPoint = _EntryPoint };
        }



        public object RunDll(byte[] pestream, string className, string entrypoint, object[] arg)
        {
            byte[] byteAssembly = pestream;

            var asm = new MemoryStream(byteAssembly);
            var assemblyLoadContext = new SimpleUnloadableAssemblyLoadContext();
            Assembly assembly = assemblyLoadContext.LoadFromStream(asm);


            //Assembly assembly = Assembly.Load(byteAssembly);
            Type exampleClassType = assembly.GetType(className);
            var instance = Activator.CreateInstance(exampleClassType);
            MethodInfo _EntryPoint = exampleClassType.GetMethod(entrypoint);







            assemblyLoadContext.Unload();
            GC.Collect();
            GC.WaitForPendingFinalizers();

            var result = _EntryPoint.Invoke(instance, arg);

            return result;
        }


        public void Execute(byte[] compiledAssembly, string[] args)
        {
            var assemblyLoadContextWeakRef = LoadAndExecute(compiledAssembly, args);

            for (var i = 0; i < 8 && assemblyLoadContextWeakRef.IsAlive; i++)
            {
                GC.Collect();
                GC.WaitForPendingFinalizers();
            }

            Debug.WriteLine(assemblyLoadContextWeakRef.IsAlive ? "Unloading failed!" : "Unloading success!");
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        private static WeakReference LoadAndExecute(byte[] compiledAssembly, string[] args)
        {
            using (var asm = new MemoryStream(compiledAssembly))
            {
                var assemblyLoadContext = new SimpleUnloadableAssemblyLoadContext();

                var assembly = assemblyLoadContext.LoadFromStream(asm);

                var entry = assembly.EntryPoint;

                _ = entry != null && entry.GetParameters().Length > 0
                    ? entry.Invoke(null, new object[] { args })
                    : entry.Invoke(null, null);

                assemblyLoadContext.Unload();

                return new WeakReference(assemblyLoadContext);
            }
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        private static WeakReference LoadDLL(byte[] compiledAssembly)
        {
            using (var asm = new MemoryStream(compiledAssembly))
            {
                var assemblyLoadContext = new SimpleUnloadableAssemblyLoadContext();

                var assembly = assemblyLoadContext.LoadFromStream(asm);



                assemblyLoadContext.Unload();

                return new WeakReference(assemblyLoadContext);
            }
        }
    }



}
