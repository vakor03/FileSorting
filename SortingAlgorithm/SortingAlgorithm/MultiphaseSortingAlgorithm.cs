using System.Diagnostics;
using System.Text;

namespace SortingAlgorithm.SortingAlgorithm;

public class MultiphaseSortingAlgorithm(ILogger logger, FileChunkSorter fileChunkSorter, int m = 3) : IFileSortingAlgorithm
{
    private readonly Dictionary<StreamReader, int> _lastValues = new();
    private readonly FibonacciCalculator _fibonacciCalculator = new FibonacciCalculator();

    public void SortFile(string path) {
        Stopwatch totalTimeStopwatch = new();
        totalTimeStopwatch.Start();
        logger.Log("Starting multiphase sorting algorithm");

        string multiphaseSortingTempFolder = "MultiphaseSortingTemp";
        if (!Directory.Exists(multiphaseSortingTempFolder))
            Directory.CreateDirectory(multiphaseSortingTempFolder);

        string fileAPath = Path.Combine(multiphaseSortingTempFolder, "fileA.txt");
        string[] filesB = CreateAdditionalFilePaths("fileB", m, multiphaseSortingTempFolder);

        CleanFiles(filesB);
        CleanFile(fileAPath);

        fileChunkSorter.SortFileInChunks(path, fileAPath);

        DivideFileAInSeries(fileAPath, filesB.SkipLast(1).ToArray());
        // LogSeriesCountInFiles(filesB);
        // PrintSeriesInFiles(filesB);
        
        string fileToMergeIn = filesB.Last();
        
        for (int i = 0;; i++)
        {
            logger.Log("");
            logger.Log($"Iteration {i}");
            Stopwatch stopwatch = new();
            stopwatch.Start();
        
            _lastValues.Clear();
            MergeSeries(filesB, fileToMergeIn);
            // LogSeriesCountInFiles(filesB);
            // PrintSeriesInFiles(filesB);
        
            if (filesB.Where(el => el != fileToMergeIn).All(IsFileEmpty))
            {
                FileSortingAlgorithmHelpers.CopyFileContents(fileToMergeIn, fileAPath);
                break;
            }
        
            //
            fileToMergeIn = filesB.First(IsFileEmpty);
            stopwatch.Stop();
            logger.Log($"Iteration {i} took {stopwatch.Elapsed.TotalSeconds} s");
        }
        
        totalTimeStopwatch.Stop();
        logger.Log($"Total time taken {totalTimeStopwatch.Elapsed.TotalSeconds}");
    }
    /// <summary>
    /// Print series via logger for debugging purposes
    /// </summary>
    private void PrintSeriesInFiles(string[] filesB)
    {
        for (int i = 0; i < filesB.Length; i++)
        {
            using StreamReader reader = new(filesB[i]);
            logger.Log($"\tFile {i}:");
            while (!reader.EndOfStream || _lastValues.ContainsKey(reader))
            {
                var readSeriesNonAlloc = ReadSeriesNonAlloc(reader).ToArray();
                if (readSeriesNonAlloc.Length > 0)
                    logger.Log($"\t\t{String.Join(", ", readSeriesNonAlloc)}");
                else
                    logger.Log($"\t\tEmpty");
            }
        }
    }

    /// <summary>
    /// Count series in files for debugging purposes
    /// </summary>
    private void LogSeriesCountInFiles(string[] filesB)
    {
        long[] seriesCounts = new long[filesB.Length];
        for (int i = 0; i < filesB.Length; i++)
        {
            using StreamReader reader = new(filesB[i]);
            while (!reader.EndOfStream || _lastValues.ContainsKey(reader))
            {
                ReadSeriesNonAlloc(reader).Count();
                seriesCounts[i]++;
            }
        }

        logger.Log($"Series counts: {String.Join(", ", seriesCounts)}");
    }

    private void CleanFiles(string[] files)
    {
        foreach (string file in files)
            File.WriteAllText(file, string.Empty);
    }

    private void CleanFile(string file)
    {
        File.WriteAllText(file, string.Empty);
    }

    private bool IsFileEmpty(string fileBPath)
    {
        using StreamReader fileBReader = new(fileBPath);
        return fileBReader.EndOfStream;
    }

    private void MergeSeries(string[] filesB, string fileToMergeIn, int count = 8)
    {
        Stopwatch stopwatch = new();

        Dictionary<string, int> numbersToClear = new();
        const int BufferSize = 65536*4; // 64 KB, adjust as necessary

        // Create a FileStream for writing with a larger buffer.
        // Using FileMode.Create to ensure we're writing a new file
        using (var fsOut = new FileStream(fileToMergeIn, FileMode.Create, FileAccess.Write, FileShare.None, BufferSize))
        using (StreamWriter currentWriter = new StreamWriter(fsOut, Encoding.UTF8, BufferSize))
        {
            // Taking all non-empty files except the file to merge in
            string[] files = filesB.Where(el => el != fileToMergeIn && !IsFileEmpty(el)).ToArray();

            
            stopwatch.Restart();
            // Creating dictionary of StreamReaders with larger buffers
            Dictionary<StreamReader, string> fileNamesDict = files.ToDictionary(el => CreateNewStreamReader(el, BufferSize), el => el);
            Dictionary<IEnumerable<int>, StreamReader> seriesDict = new();
            Dictionary<IEnumerator<int>, IEnumerable<int>> enumeratorsDict = new();
            Dictionary<IEnumerator<int>, bool> seriesHasNext = new();
            
            stopwatch.Stop();
            logger.Log($"Opening files took {stopwatch.Elapsed.TotalSeconds} s");

            int totalSeriesCount = 0;
            while (fileNamesDict.Keys.All(el => !el.EndOfStream || _lastValues.ContainsKey(el)))
            {
                totalSeriesCount++;

                CreateSeriesDict(fileNamesDict, seriesDict);

                CreateEnumeratorsDict(seriesDict, enumeratorsDict);

                CreateSeriesHasNext(enumeratorsDict, fileNamesDict, seriesDict, numbersToClear, seriesHasNext);

                // Merge series
                MergeSeries(seriesHasNext, fileNamesDict, seriesDict, enumeratorsDict, numbersToClear, currentWriter);

                // Dispose all enumerators
                DisposeEnumerators(enumeratorsDict);
                
                // _currentValueCache.Clear();
                // seriesHasNext.Clear();
            }

            logger.Log($"Total series count: {totalSeriesCount}");

            
            stopwatch.Restart();
            // Dispose all file readers
            foreach (StreamReader streamReader in fileNamesDict.Keys)
                streamReader.Dispose();
            stopwatch.Stop();
            logger.Log($"Closing files took {stopwatch.Elapsed.TotalSeconds} s");
        }
        
        stopwatch.Restart();
        // Parallel.ForEach(numbersToClear,(pair, _) => { RemoveFirstNLinesEfficiently(pair.Key, pair.Value); });
        foreach ((string? key, int value) in numbersToClear) {
            RemoveFirstNLinesEfficiently(key, value);
        }
        stopwatch.Stop();
        logger.Log($"Removing lines took {stopwatch.Elapsed.TotalSeconds} s");
    }

    private static void DisposeEnumerators(Dictionary<IEnumerator<int>, IEnumerable<int>> enumeratorsDict) {
        foreach (IEnumerator<int> seriesEnumerator in enumeratorsDict.Keys)
            seriesEnumerator.Dispose();
    }

    private void MergeSeries(Dictionary<IEnumerator<int>, bool> seriesHasNext, Dictionary<StreamReader, string> fileNamesDict, Dictionary<IEnumerable<int>, StreamReader> seriesDict, Dictionary<IEnumerator<int>, IEnumerable<int>> enumeratorsDict,
                             Dictionary<string, int> numbersToClear, StreamWriter currentWriter) {
        // IEnumerator<int> minEnumerator = FindIEnumeratorWithMinCurrent(seriesHasNext);
        // int minValue = minEnumerator.Current;
        while (seriesHasNext.Any(el => el.Value))
        {
            // Find the minimum current value from active series
            IEnumerator<int> minSeriesEnumerator = FindIEnumeratorWithMinCurrent(seriesHasNext);
            // IEnumerator<int> minSeriesEnumerator = minEnumerator;

            string file = fileNamesDict[seriesDict[enumeratorsDict[minSeriesEnumerator]]];
            if (!numbersToClear.TryAdd(file, 1)) numbersToClear[file]++;

            currentWriter.WriteLine(GetCurrent(minSeriesEnumerator));

            // Move the enumerator forward
            var hasNext = MoveEnumeratorNext(seriesHasNext, minSeriesEnumerator);
            // if (hasNext && minSeriesEnumerator.Current < minValue) {
                
            // }
        }
    }

    private void CreateSeriesDict(Dictionary<StreamReader, string> fileNamesDict,
                                  Dictionary<IEnumerable<int>, StreamReader> seriesDict) {
        seriesDict.Clear();
        
        foreach ((StreamReader? key, string? _) in fileNamesDict)
            seriesDict.Add(ReadSeriesNonAlloc(key),key);
    }

    private void CreateEnumeratorsDict(Dictionary<IEnumerable<int>, StreamReader> seriesDict, Dictionary<IEnumerator<int>, IEnumerable<int>> enumeratorsDict) {
        enumeratorsDict.Clear();
        
        foreach ((IEnumerable<int> key, StreamReader? _) in seriesDict)
            enumeratorsDict.Add(key.GetEnumerator(), key);
    }

    private void CreateSeriesHasNext(Dictionary<IEnumerator<int>, IEnumerable<int>> enumeratorsDict,
                                                                   Dictionary<StreamReader, string> fileNamesDict,
                                                                   Dictionary<IEnumerable<int>, StreamReader> seriesDict,
                                                                   Dictionary<string, int> numbersToClear,
                                                                   Dictionary<IEnumerator<int>, bool> dictionary) {
        dictionary.Clear();
        FillDictionaryWithEnumeratorValues(dictionary, enumeratorsDict);

        foreach (var (enumerator, hasNext) in dictionary)
            if (!hasNext)
            {
                string file = fileNamesDict[seriesDict[enumeratorsDict[enumerator]]];
                if (!numbersToClear.TryAdd(file, 1)) numbersToClear[file]++;
            }
    }

    private int GetCurrent(IEnumerator<int> minSeriesEnumerator) =>
        minSeriesEnumerator.Current;

    private static StreamReader CreateNewStreamReader(string el, int BufferSize) =>
        new(
            new FileStream(el, FileMode.Open, FileAccess.Read, FileShare.Read, BufferSize),
            Encoding.UTF8,
            detectEncodingFromByteOrderMarks: false,
            bufferSize: BufferSize);

    private void FillDictionaryWithEnumeratorValues(Dictionary<IEnumerator<int>,bool> seriesHasNext, Dictionary<IEnumerator<int>,IEnumerable<int>> enumeratorsDict) {
        foreach ((IEnumerator<int> key, IEnumerable<int>? value) in enumeratorsDict) {
            bool moveNext = key.MoveNext();
            seriesHasNext.Add(key, moveNext);
        }
    }

    private bool MoveEnumeratorNext(Dictionary<IEnumerator<int>, bool> seriesHasNext, IEnumerator<int> minSeriesEnumerator) {
        bool moveNext = minSeriesEnumerator.MoveNext();
        seriesHasNext[minSeriesEnumerator] = moveNext;
        return moveNext;
    }

    private IEnumerator<int> FindIEnumeratorWithMinCurrent(Dictionary<IEnumerator<int>, bool> seriesHasNext)
    {
        IEnumerator<int> enumeratorWithSmallestValue = null;
        int smallestValue = int.MaxValue;
        
        foreach (var (enumerator, hasNext) in seriesHasNext)
        {
            if (!hasNext) continue;
            if (GetCurrent(enumerator) < smallestValue)
            {
                enumeratorWithSmallestValue = enumerator;
                smallestValue = GetCurrent(enumerator);
            }
        }
        
        return enumeratorWithSmallestValue;
    }


    private void RemoveFirstNLinesEfficiently(string filePath, int n)
    {
        string tempFilePath = filePath + ".tmp";

        using (var reader = new StreamReader(filePath))
        using (var writer = new StreamWriter(tempFilePath))
        {
            int lineCount = 0;

            while (lineCount < n && !reader.EndOfStream)
            {
                reader.ReadLine();
                lineCount++;
            }

            while (!reader.EndOfStream)
            {
                writer.WriteLine(reader.ReadLine());
            }
        }

        File.Delete(filePath);
        File.Move(tempFilePath, filePath);
    }

    private string[] CreateAdditionalFilePaths(string fileBase, int count, string parent)
    {
        string[] filePaths = new string[count];
        for (int i = 0; i < count; i++)
            filePaths[i] = Path.Combine(parent, fileBase + i + ".txt");

        return filePaths;
    }

    private void DivideFileAInSeries(string fileAPath, string[] filesB)
    {
        int totalRuns = CountSeriesInFile(fileAPath);

        logger.Log("Total runs: " + totalRuns);

        int fileCount = filesB.Length;

        // Computing fibonacci distribution required for the series
        long[] fibDist = _fibonacciCalculator.ComputeFibonacciDistribution(totalRuns, fileCount, m);
        logger.Log($"Fibonacci distribution: {String.Join(", ", fibDist)}");
        logger.Log($"Total required runs: {fibDist.Sum()} ({totalRuns})");

        // Read series from the initial file and write them to the files. If the series in file is not enogh, write " " to indicate the empty serie
        using (var reader = new StreamReader(fileAPath))
        {
            StreamWriter[] writers = new StreamWriter[fileCount];
            for (long i = 0; i < fileCount; i++)
                writers[i] = new StreamWriter(filesB[i]);

            long runIndex = 0;
            for (long i = 0; i < fileCount; i++)
            {
                long runsForFile = fibDist[i];
                logger.Log("Runs for file " + i + ": " + runsForFile);
                long dummyRuns = 0;
                long nonDummyRuns = 0;
                for (long r = 0; r < runsForFile; r++)
                {
                    if (r + runIndex < totalRuns)
                    {
                        var series = ReadSeriesNonAlloc(reader);
                        foreach (var n in series) writers[i].WriteLine(n);
                        nonDummyRuns++;
                    }
                    else
                    {
                        // Using " " to indicate the empty serie
                        writers[i].WriteLine(" ");
                        dummyRuns++;
                    }
                }

                logger.Log(
                    $"File {i}: {nonDummyRuns} non-dummy runs, {dummyRuns} dummy runs {dummyRuns + nonDummyRuns} total runs");
                runIndex += runsForFile;
            }

            foreach (var w in writers) w.Dispose();
        }
    }

    private int CountSeriesInFile(string fileAPath)
    {
        int totalRuns = 0;
        using (var reader = new StreamReader(fileAPath))
        {
            while (!reader.EndOfStream || _lastValues.Count > 0)
            {
                IEnumerable<int> series = ReadSeriesNonAlloc(reader);
                // I need this .Coount() to read IEnumerable to the end
                series.Count();
                totalRuns++;
            }
        }

        return totalRuns;
    }

    /// <summary>
    /// Reading serie from the file. To know that the serie is ended I need to know next value of stream. This is why I am using _lastValues dictionary to save it.
    /// If serie is empty I just store -1 in the dictionary. If serie is not empty I store the last value of the serie.
    /// </summary>
    /// <param name="fileAReader"></param>
    /// <returns></returns>
    private IEnumerable<int> ReadSeriesNonAlloc(StreamReader fileAReader) {
        int previousNumber = int.MinValue;

        if (_lastValues.TryGetValue(fileAReader, out int lastValue)) {
            if (lastValue < 0) {
                _lastValues.Remove(fileAReader);
                yield break;
            }

            previousNumber = lastValue;
            _lastValues.Remove(fileAReader);
            yield return lastValue;
        }

        while (!fileAReader.EndOfStream) {
            string? line = fileAReader.ReadLine();
            if (line == " ") {
                if (previousNumber > 0) {
                    _lastValues[fileAReader] = -1;
                }

                yield break;
            }

            // if (int.TryParse(line, out int number))
            // {
            int number = int.Parse(line!);
            // if end of serie is reached write -1 to the dictionary and break the loop
            if (number < previousNumber) {
                _lastValues[fileAReader] = number;
                break;
            }

            yield return number;
            previousNumber = number;
        }
    }
}