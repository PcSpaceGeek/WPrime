# WPrime
This program calculates all the prime numbers on a given interval. Is written in C#.

--------------

**Features:**
- Uses multithreaded functionalities. The used can determine the no. of cores used in the calculations.
- Can find all the prime numbers until 1*10^16.
- Provides text ant visual feedback on the progress.
- The program automatically partitions the given interval in 10^8 sized intervals. After finishing an interval all the prime numbers found in that interval are saved in a separate file. This makes sure that resulting files remains at an acceptable size (~50MB) and limits the space used up in memory.
- The determined primes are saved in text files in the root directory.
- The process can be cancelled at any point in the calculation process.

--------------

## Instructions

- Provide the starting and ending numbers.
- Specify the no. of cores.
- Hit the “Calculate” button and sit back until it finishes.
