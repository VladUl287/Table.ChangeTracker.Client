using BenchmarkDotNet.Running;
using Tracker.Benchmarks;

Console.WriteLine(new HashersBenchamrk().Murmur_Hasher());
Console.WriteLine(new HashersBenchamrk().Fnv1a_Hasher());
Console.WriteLine(new HashersBenchamrk().XxHash64_Hasher());

BenchmarkRunner.Run<HashersBenchamrk>();
return;

BenchmarkRunner.Run<ReferenceEqualVsManuallStringCompare>();
return;

BenchmarkRunner.Run<ETagComparerBenchmark>();
