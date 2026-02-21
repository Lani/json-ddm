using BenchmarkDotNet.Running;

namespace JsonDdm.Benchmarks;

class Program
{
  static void Main(string[] args)
  {
    // Run all benchmarks in the assembly
    BenchmarkSwitcher.FromAssembly(typeof(Program).Assembly).Run(args);
  }
}
