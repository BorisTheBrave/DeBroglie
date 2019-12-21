using BenchmarkDotNet.Running;

namespace DeBroglie.Benchmark
{
    public class Program
    {
        public static void Main(string[] args)
        {
            /*
            var benchmark = new Benchmarks();
            benchmark.Setup();
            benchmark.Path();
            */
            BenchmarkSwitcher.FromAssembly(typeof(Program).Assembly).Run(args);
        }
    }
}
