using BenchmarkDotNet.Configs;
#if !NET46 && !NET40
using BenchmarkDotNet.Exporters;
#endif
using BenchmarkDotNet.Running;

namespace Microsoft.SolverFoundation.Benchmark
{
    public class Program
    {
        public static void Main()
        {
            BenchmarkRunner.Run<SolverBenchmark>(
                ManualConfig.Create(DefaultConfig.Instance)
#if !NET46 && !NET40
                    .AddExporter(MarkdownExporter.Default) // Export results as Markdown
#endif
            );
        }
    }
}