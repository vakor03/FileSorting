using SortingAlgorithm.FIleGenerator;
using SortingAlgorithm.SortingAlgorithm;

Random random = new Random();
IRandomFileGenerator randomFileGenerator = new RandomFileGenerator(random);
randomFileGenerator.GenerateFileWithRandomInts("testMB.txt", new MemorySize(1, MemorySizeUnit.MB));
randomFileGenerator.GenerateFileWithRandomInts("testKB.txt", new MemorySize(1, MemorySizeUnit.KB));

ILogger logger = new ConsoleLogger();

IFileSortingAlgorithm fileSortingAlgorithm = new BalancedFileSortingAlgorithm(logger);
fileSortingAlgorithm.SortFile("testMB.txt");

