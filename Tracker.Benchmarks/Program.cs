using BenchmarkDotNet.Running;
using Tracker.Benchmarks;

BenchmarkRunner.Run<NpgsqlOperationBenchmark>();
return;

//var bench = new TrackerEndpointFilterBenchmark();
//bench.Setup();
//await bench.Middleware_PostMethod();

//BenchmarkRunner.Run<TrackerEndpointFilterBenchmark>();
//return;

//BenchmarkRunner.Run<HashersBenchamrk>();
//return;

//BenchmarkRunner.Run<ReferenceEqualVsManuallStringCompare>();
//return;

//BenchmarkRunner.Run<ETagComparerBenchmark>();
