using BenchmarkDotNet.Running;
using DotnetConfigServer.Benchmarks;

Console.WriteLine("Dotnet Config Server Benchmarks");
Console.WriteLine("==============================");
Console.WriteLine();
Console.WriteLine("Available benchmarks:");
Console.WriteLine("1. ConfigurationBenchmarks - Core configuration CRUD operations");
Console.WriteLine("2. EncryptionBenchmarks - Encryption/decryption performance");
Console.WriteLine("3. VersioningBenchmarks - Configuration versioning operations");
Console.WriteLine("4. DiffBenchmarks - Configuration comparison and diff operations");
Console.WriteLine();

var summary = BenchmarkRunner.Run(typeof(Program).Assembly);
