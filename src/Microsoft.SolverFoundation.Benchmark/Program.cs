using BenchmarkDotNet.Configs;
using BenchmarkDotNet.Exporters;
using BenchmarkDotNet.Running;

namespace Microsoft.SolverFoundation.Benchmark
{
    public class Program
    {
        public static void Main()
        {
            BenchmarkRunner.Run<SolverBenchmark>(
                ManualConfig.Create(DefaultConfig.Instance)
                    .AddExporter(MarkdownExporter.Default) // Export results as Markdown
            );
        }
    }
}