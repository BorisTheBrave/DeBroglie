using BenchmarkDotNet.Configs;
using BenchmarkDotNet.Running;

namespace DeBroglie.Benchmark
{
    public class Program
    {
        public static void Main(string[] args)
        {
            /*
            for (var i = 0; i < 20; i++)
            {
                var benchmark = new Benchmarks();
                benchmark.Setup();
                benchmark.EdgedPath();
                System.Console.WriteLine(i);
            }
            */
            BenchmarkSwitcher.FromAssembly(typeof(Program).Assembly).Run(args);
            //BenchmarkSwitcher.FromAssembly(typeof(Program).Assembly).Run(args, new DebugInProcessConfig());
        }
    }
}
