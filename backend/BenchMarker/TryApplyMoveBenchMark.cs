using System.Diagnostics;
using WebGameServer.GameLogic;
using WebGameServer.State;
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
var fromBit = GameState.GetBitIndex(1, 2);
var toBit = GameState.GetBitIndex(5, 2); 

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
            GameLogic.TryApplyMove(ref benchmarkState, fromBit, toBit);
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
    
    
    ///After Adding History Management. (no major loss) 
    1000 iterations: Average 1085 ticks, which equals 108500 ns
    10000 iterations: Average 4792 ticks, which equals 479200 ns
    100000 iterations: Average 20095 ticks, which equals 2009500 ns
    1000000 iterations: Average 81027 ticks, which equals 8102700 ns
    10000000 iterations: Average 794730 ticks, which equals 79473000 ns
    
    
    //After Changing GameState to have getters and setters for serialization
    Benchmarking TryApplyMove...
    1000 iterations: Average 1134 ticks, which equals 113400 ns
    10000 iterations: Average 5809 ticks, which equals 580900 ns
    100000 iterations: Average 18862 ticks, which equals 1886200 ns
    1000000 iterations: Average 70726 ticks, which equals 7072600 ns
    10000000 iterations: Average 689924 ticks, which equals 68992400 ns

    //After Updating back end to use structs and use a stack instead of recursion. 
    //Seems to cause slow down at the highend, likely due to object creation and GC 
    
    1000 iterations: Average 935 ticks, which equals 93500 ns
    10000 iterations: Average 4411 ticks, which equals 441100 ns
    100000 iterations: Average 21965 ticks, which equals 2196500 ns
    1000000 iterations: Average 97177 ticks, which equals 9717700 ns
    10000000 iterations: Average 967530 ticks, which equals 96753000 ns

    //Consulted Stack Alloc implementation results, hard to tell the perf results for large itterations due to PC 
    //pc memory issues, but the min ticks are very very good now. 
    
    1000 iterations: Average 408 ticks, which equals 40800 ns
    10000 iterations: Average 944 ticks, which equals 94400 ns
    100000 iterations: Average 9564 ticks, which equals 956400 ns
    1000000 iterations: Average 82966 ticks, which equals 8296600 ns
    10000000 iterations: Average 818376 ticks, which equals 81837600 ns


*/