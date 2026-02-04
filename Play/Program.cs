using System;

if (args.Length > 0 && args[0] == "stash-demo")
{
    Play.StashParsingDemo.DemoParsingAndGeneration();
}
else
{
    Console.WriteLine("Available demos:");
    Console.WriteLine("  dotnet run --project Play stash-demo    - Run stash notes parsing demo");
}

