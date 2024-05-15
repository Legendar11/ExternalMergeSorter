# Document Generator and Document Sorter

Two utilities for large file creation and sorting

## Highly configurable
Provided program have several levels for configuraion:
- Options: parameters may change from run to run, main entrypoint for customization & optimization
- Configuration: more specific parameters for internal usage, might be used for precision configuration particularly for your platform
- Constants: static data used for algorithms, minor parameters and constants 

### Options for generation
- `input (i)` - input filename for unsorted document
- `output (o)` - output filename for sorted document
- `encoding (e)` - encoding for input & output files
- `parallelism (p)` - degree of parallelism
- `from (f)` - start of the generated random numbers range (included in range)
- `to (t)` - end of the generated random numbers range (not included in range)

### Options for sorting:
- `input (i)` - input filename for unsorted document
- `output (o)` - output filename for sorted document
- `encoding (e)` - encoding for input & output files
- `parallelism (p)` - degree of parallelism
- `merge (m)` - how many files are merged by external sort iteration
- `generate (g)` - specify in case you need to generate file with provided filesize before sorting

## Rapidly fast
We are committed to efficiency paradigms hence we know when, how and we should choose different data types and algorithms:
- Document generator uses direct access to generated file, no intermidiate writers/buffers
- Three different sort algorithms are used: external merge sort for large input file management, quick sort for chunk files sorting, insertion sort for chunk files merging 


## Perfect memory utilization
One of the most important characteristics for large file generation & sorting - used RAM due execution. That's why we did our best to reduce memory consumption:
- Document generator uses buffered string to be sure to create no additional allocation, resources for memory are allocated only once on program startup
- Document sorter uses only readed string from file chunk memory and reserves data for hash dictionary, there is no unexpected memory spikes due sorting

## Low-level approach
We do care how bytes are organized, how they are allocated and how they should be manipulated. By that reason we use only robust and effective algorithms:
- Document generator separate output file by independent areas for most effecient way to use parallelism
- Document sorter compares strings by char symbols, we don't produce extra string allocation by sorting 

## Benchmark & Tests supports
Provided solutions are fully covered by interactive benchmarks and carefully prepared unit tests - here is a room for experiments!
