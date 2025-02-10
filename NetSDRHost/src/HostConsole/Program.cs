using SdrHost;

using var srdHost = new Host();
await srdHost.RunAsync(Console.ReadLine, Console.WriteLine, $"log_{DateTime.UtcNow:yyyyMMdd_HHmmss}.bin");