using BenchmarkDotNet.Configs;
using BenchmarkDotNet.Environments;
using BenchmarkDotNet.Jobs;
using BenchmarkDotNet.Running;

namespace DeBroglie.Benchmark
{
    public class Program
    {
        public static void Main(string[] args)
        {
            BenchmarkRunner
                .Run<Benchmarks>(
                    DefaultConfig.Instance
                        .With(Job.Default.WithId("A"))
                        .With(Job.Default)
                        .With(Job.Default.WithId("Z"))
                        );
        }
    }
}
