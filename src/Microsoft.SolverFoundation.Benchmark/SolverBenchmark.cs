using BenchmarkDotNet.Attributes;
#if NET46
using BenchmarkDotNet.Attributes.Exporters;
#elif !NET40
using BenchmarkDotNet.Jobs;
#endif
using Microsoft.SolverFoundation.ReferenceTests;
using Microsoft.SolverFoundation.Services;

namespace Microsoft.SolverFoundation.Benchmark
{
#if !NET46 && !NET40
    [SimpleJob(RuntimeMoniker.Net472, baseline: true)]
    [SimpleJob(RuntimeMoniker.Net60)]
    [SimpleJob(RuntimeMoniker.Net70)]
    [SimpleJob(RuntimeMoniker.Net80)]
    [SimpleJob(RuntimeMoniker.Net90)]
#endif
#if !NET40
    [MemoryDiagnoser]
    [MarkdownExporter]
#endif
    public class SolverBenchmark
    {
        private CustomScenario _scenario;

#if !NET40
        [GlobalSetup]
#endif
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
