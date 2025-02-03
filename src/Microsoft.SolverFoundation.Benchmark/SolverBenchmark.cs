using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Jobs;
using Microsoft.SolverFoundation.ReferenceTests;
using Microsoft.SolverFoundation.Services;

namespace Microsoft.SolverFoundation.Benchmark
{
    [SimpleJob(RuntimeMoniker.Net472, baseline: true)]
    [SimpleJob(RuntimeMoniker.Net60)]
    [SimpleJob(RuntimeMoniker.Net70)]
    [SimpleJob(RuntimeMoniker.Net80)]
    [SimpleJob(RuntimeMoniker.Net90)]
    [MemoryDiagnoser]
    [MarkdownExporter]
    public class SolverBenchmark
    {
        private CustomScenario _scenario;

        [GlobalSetup]
        public void Setup()
        {
            _scenario = new CustomScenario();
            _scenario.Create();
        }

        [Benchmark]
        public Solution Solve()
        {
            return _scenario.Solve();
        }
    }
}