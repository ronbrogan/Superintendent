using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.Testing;
using Microsoft.CodeAnalysis.Text;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Superintendent.Generation;
using System.Diagnostics;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using Basic.Reference.Assemblies;

namespace Superintendent.Core.Tests
{
    [TestClass]
    public class CommandSinkClientGeneratorTests : GeneratorTestClass<CommandSinkClientGenerator>
    {
        public override MetadataReference[] AdditionalReferences => new[]
        {
            MetadataReference.CreateFromFile(typeof(Fun).Assembly.Location),
            MetadataReference.CreateFromFile(typeof(GenerateClientAttribute).Assembly.Location),
        };


        [TestMethod]
        public async Task TestMethod1()
        {
            var (generatorRun, comp) = this.Compile(@"
namespace MyCode
{
    using Superintendent.Core;
    using Superintendent.Core.Remote;
    using Superintendent.Generation;

    [GenerateClientAttribute]
    public partial class ProgramOffsets
    {
        [ParamNames(""v"")]
        public Fun<int, int> DoWork { get; set; }

        public FunVoid<int> DoWork2 { get; set; }

        public Ptr<float> Val { get; set; }

        public static ProgramOffsets V1 = new()
        {
        };
    }

    public class ClientUser
    {
        public void Test()
        {
            RpcRemoteProcess proc = null;
            var sink = proc.GetCommandSink(""test.dll"");
            ProgramOffsets offsets = ProgramOffsets.V1;
            ProgramOffsets.Client client = offsets.CreateClient(sink);
            
            client.DoWork(v: 2);

            var v = client.ReadVal();
            client.WriteVal(v+1f);
        }
    }
}
");

            Assert.AreEqual(0, generatorRun.Diagnostics.Count(d => d.Severity == DiagnosticSeverity.Error));
        }

    }

    [TestClass]
    public abstract class GeneratorTestClass<TGenerator>
        where TGenerator : IIncrementalGenerator, new()
    {
        public abstract MetadataReference[] AdditionalReferences { get; }

        public (GeneratorRunResult, Compilation) Compile(string source)
        {
            var references = AdditionalReferences
                .Concat(Net60.References.All);

            var compilation = CSharpCompilation.Create("compilation",
                new[] { CSharpSyntaxTree.ParseText(source) },
                references,
                new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary));

            // directly create an instance of the generator
            // (Note: in the compiler this is loaded from an assembly, and created via reflection at runtime)
            var generator = new TGenerator();

            // Create the driver that will control the generation, passing in our generator
            GeneratorDriver driver = CSharpGeneratorDriver.Create(generator);

            // Run the generation pass
            // (Note: the generator driver itself is immutable, and all calls return an updated version of the driver that you should use for subsequent calls)
            driver = driver.RunGeneratorsAndUpdateCompilation(compilation, out var outputCompilation, out var diagnostics);

            // We can now assert things about the resulting compilation:
            Debug.Assert(diagnostics.IsEmpty); // there were no diagnostics created by the generators

            var diags = outputCompilation.GetDiagnostics();
            Assert.IsFalse(diags.Where(d => d.Severity == DiagnosticSeverity.Error).Any(), "Error diags from compilation: " + string.Join("\r\n", diags.Select(d => d.ToString())));

            // Or we can look at the results directly:
            GeneratorDriverRunResult runResult = driver.GetRunResult();

            // The runResult contains the combined results of all generators passed to the driver
            Debug.Assert(runResult.GeneratedTrees.Length == 1);
            Debug.Assert(runResult.Diagnostics.IsEmpty);

            // Or you can access the individual results on a by-generator basis
            return (runResult.Results[0], outputCompilation);
        }

    }
}