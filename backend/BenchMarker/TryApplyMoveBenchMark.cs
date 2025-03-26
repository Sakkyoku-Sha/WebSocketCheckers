using System.Diagnostics;
using WebGameServer.GameLogic;
using WebGameServerTests;

int[] iterationCounts = { 1000, 10000, 100000, 1000000, 10000000 };
int runsPerTest = 100;

// Set up the initial game state similar to the unit test
var boardString = new string[]
{
    "........", // row 0
    "..e.e...", // row 1
    ".p......", // row 2
    "........", // row 3
    "........", // row 4
    "........", // row 5
    "........", // row 6
    "........", // row 7
};
var state = GameLogicTests.CreateBoardFromStringArray(boardString);
var move = new CheckersMove(1, 2, 5, 2);

Console.WriteLine("Benchmarking TryApplyMove...");
foreach (var count in iterationCounts)
{
    long totalTicks = 0;
    // Run the benchmark several times and average the ticks
    for (int run = 0; run < runsPerTest; run++)
    {
        var benchmarkState = state; // Copy state for each run
        var stopwatch = Stopwatch.StartNew();

        for (int i = 0; i < count; i++)
        {
            GameLogic.TryApplyMove(ref benchmarkState, move);
        }

        stopwatch.Stop();
        totalTicks += stopwatch.ElapsedTicks;
    }

    long averageTicks = totalTicks / runsPerTest;
    long averageNanoseconds = averageTicks * 1_000_000_000 / Stopwatch.Frequency;

    Console.WriteLine($"{count} iterations: Average {averageTicks} ticks, which equals {averageNanoseconds} ns");
}



/*
    Current Estimates dotnet build -c Release versions 
    
    1000 iterations: Average 1116 ticks, which equals 111600 ns
    10000 iterations: Average 5723 ticks, which equals 572300 ns
    100000 iterations: Average 21093 ticks, which equals 2109300 ns
    1000000 iterations: Average 105546 ticks, which equals 10554600 ns
    10000000 iterations: Average 994107 ticks, which equals 99410700 ns

*/