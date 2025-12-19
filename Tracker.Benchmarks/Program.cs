using BenchmarkDotNet.Running;
using Tracker.Benchmarks;

BenchmarkRunner.Run<NpgsqlOperationBenchmark>();
return;

//BenchmarkRunner.Run<TrackerMiddlewareFlterBenchmark>();
//return;

//BenchmarkRunner.Run<HashersBenchamrk>();
//return;

//BenchmarkRunner.Run<ReferenceEqualVsManuallStringCompare>();
//return;

//BenchmarkRunner.Run<ETagComparerBenchmark>();
