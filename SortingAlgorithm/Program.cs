using SortingAlgorithm.FIleGenerator;
using SortingAlgorithm.SortingAlgorithm;

Random random = new Random();
IRandomFileGenerator randomFileGenerator = new RandomFileGenerator(random);
randomFileGenerator.GenerateFileWithRandomInts("testMB.txt", new MemorySize(1, MemorySizeUnit.GB));
// randomFileGenerator.GenerateFileWithRandomInts("testKB.txt", new MemorySize(1, MemorySizeUnit.KB));

ILogger fileLogger = new FileLogger(DateTime.Now.ToString("h_mm_ss"+".log"));
ILogger consoleLogger = new ConsoleLogger();

ILogger logger = new CombinedLogger(consoleLogger);

FileChunkSorter fileChunkSorter = new FileChunkSorter();
IFileSortingAlgorithm fileSortingAlgorithm = new MultiphaseSortingAlgorithm(logger,fileChunkSorter,3);
fileSortingAlgorithm.SortFile("testMB.txt");

logger.Dispose();

