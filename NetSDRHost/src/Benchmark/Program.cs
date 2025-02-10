using Benchmark;
using BenchmarkDotNet.Configs;
using BenchmarkDotNet.Diagnosers;
using BenchmarkDotNet.Jobs;
using BenchmarkDotNet.Running;

BenchmarkRunner.Run<ControlMessageBenchmark>(
    ManualConfig
        .CreateMinimumViable()

        //.Create(DefaultConfig.Instance)
        //.AddJob(Job.Default

        //    .WithLaunchCount(1)     // benchmark process will be launched only once
        //    .WithIterationCount(10)
        //    .WithInvocationCount(10)
        //    .WithWarmupCount(3)     // 3 warmup iteration
        //    .WithUnrollFactor(5)
        //)

        .WithOptions(ConfigOptions.DisableLogFile)  // Disable file output
        .WithOptions(ConfigOptions.JoinSummary)    // Join output in one line
        .WithOptions(ConfigOptions.KeepBenchmarkFiles) // Prevent file creation
        .WithOptions(ConfigOptions.DisableOptimizationsValidator)
        .AddDiagnoser(MemoryDiagnoser.Default)
);
