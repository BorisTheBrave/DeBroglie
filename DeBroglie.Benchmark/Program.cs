using BenchmarkDotNet.Running;

namespace DeBroglie.Benchmark
{
    public class Program
    {
        public static void Main(string[] args)
        {
            var summary = BenchmarkRunner.Run<Benchmarks>();
        }
    }
}
