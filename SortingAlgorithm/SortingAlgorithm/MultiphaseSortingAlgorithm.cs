namespace SortingAlgorithm.SortingAlgorithm;

public class MultiphaseSortingAlgorithm(ILogger logger, int m = 3) : IFileSortingAlgorithm
{
    private readonly Dictionary<StreamReader, int> _lastValues = new();
    private readonly FibonacciCalculator _fibonacciCalculator = new FibonacciCalculator();

    public void SortFile(string path)
    {
        logger.Log("Starting multiphase sorting algorithm");

        string multiphaseSortingTempFolder = "MultiphaseSortingTemp";
        if (!Directory.Exists(multiphaseSortingTempFolder))
            Directory.CreateDirectory(multiphaseSortingTempFolder);

        string fileAPath = Path.Combine(multiphaseSortingTempFolder, "fileA.txt");
        string[] filesB = CreateAdditionalFilePaths("fileB", m, multiphaseSortingTempFolder);

        CleanFiles(filesB);
        CleanFile(fileAPath);

        FileSortingAlgorithmHelpers.CopyFileContents(path, fileAPath);

        DivideFileAInSeries(fileAPath, filesB.SkipLast(1).ToArray());
        LogSeriesCountInFiles(filesB);
        // PrintSeriesInFiles(filesB);

        string fileToMergeIn = filesB.Last();

        for (int i = 0; i < 200; i++)
        {
            logger.Log("");

            _lastValues.Clear();
            MergeSeries(filesB, fileToMergeIn);
            // LogSeriesCountInFiles(filesB);
            // PrintSeriesInFiles(filesB);

            if (filesB.Where(el => el != fileToMergeIn).All(IsFileEmpty))
            {
                FileSortingAlgorithmHelpers.CopyFileContents(fileToMergeIn, fileAPath);
                return;
            }

            //
            fileToMergeIn = filesB.First(IsFileEmpty);
        }
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
            seriesCounts[i] = CountSeriesInFile(filesB[i]);
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
        Dictionary<string, int> numbersToClear = new();
        using (StreamWriter currentWriter = new StreamWriter(fileToMergeIn))
        {
            // Taking all non-empty files except the file to merge in
            string[] files = filesB.Where(el => el != fileToMergeIn && !IsFileEmpty(el)).ToArray();

            // Creating a dictionary of file readers and their file names
            Dictionary<StreamReader, string> fileNamesDict = files.ToDictionary(el => new StreamReader(el), el => el);

            int totalSeriesCount = 0;
            while (fileNamesDict.Keys.All(el => !el.EndOfStream || _lastValues.ContainsKey(el)))
            {
                totalSeriesCount++;

                // Creating a dictionary of series and their file names
                // I needed enumerables, so that I do not need to store the series in memory
                Dictionary<IEnumerable<int>, StreamReader> seriesDict =
                    fileNamesDict.ToDictionary(el => ReadSeriesNonAlloc(el.Key), el => el.Key);

                // Creating a dictionary of series enumerators and their series
                // I needed enumerators, so that I can get the current value of the series
                Dictionary<IEnumerator<int>, IEnumerable<int>> enumeratorsDict =
                    seriesDict.ToDictionary(el => el.Key.GetEnumerator(), el => el.Key);

                // Creating a dictionary of series enumerators and their hasNext values
                // HasNext is used to check if the enumerator has more values. For example if serie is empty, it is false in here
                Dictionary<IEnumerator<int>, bool> seriesHasNext =
                    enumeratorsDict.Keys.ToDictionary(el => el, el => el.MoveNext());


                // I serie is empty, I want to delete the line from this file later
                foreach (var (key, value) in seriesHasNext)
                {
                    if (!value)
                    {
                        string file = fileNamesDict[seriesDict[enumeratorsDict[key]]];
                        if (!numbersToClear.TryAdd(file, 1)) numbersToClear[file]++;
                    }
                }

                // Merging series
                while (seriesHasNext.Any(el => el.Value))
                {
                    IEnumerator<int> minSeriesEnumerator = seriesHasNext
                        .Where(el => el.Value)
                        .OrderBy(el => el.Key.Current)
                        .First()
                        .Key;

                    // This if statement is used to count how many numbers I need to delete from the file later
                    string file = fileNamesDict[seriesDict[enumeratorsDict[minSeriesEnumerator]]];
                    if (!numbersToClear.TryAdd(file, 1)) numbersToClear[file]++;

                    // Writing the current value of the series to the file
                    currentWriter.WriteLine(minSeriesEnumerator.Current);
                    
                    // Moving the enumerator to the next value. So that I am moving only pointer of current series
                    seriesHasNext[minSeriesEnumerator] = minSeriesEnumerator.MoveNext();
                }

                // Disposing all series enumerators
                foreach (IEnumerator<int> seriesEnumerator in enumeratorsDict.Keys)
                    seriesEnumerator.Dispose();
            }

            logger.Log($"Total series count: {totalSeriesCount}");

            // Disposing all file readers
            foreach (StreamReader streamReader in fileNamesDict.Keys)
                streamReader.Dispose();
        }

        foreach (var (key, value) in numbersToClear)
        {
            // Because we "moving" some lines to another file, we need to delete them from the original file
            RemoveFirstNLinesEfficiently(key, value);
            // logger.Log($"Clearing {value} numbers from {key}");
        }
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
    private IEnumerable<int> ReadSeriesNonAlloc(StreamReader fileAReader)
    {
        int previousNumber = int.MinValue;

        if (_lastValues.TryGetValue(fileAReader, out int lastValue))
        {
            if (lastValue < 0)
            {
                _lastValues.Remove(fileAReader);
                yield break;
            }

            previousNumber = lastValue;
            _lastValues.Remove(fileAReader);
            yield return lastValue;
        }

        while (!fileAReader.EndOfStream)
        {
            string? line = fileAReader.ReadLine();

            if (int.TryParse(line, out int number))
            {
                // if end of serie is reached write -1 to the dictionary and break the loop
                if (number < previousNumber)
                {
                    _lastValues[fileAReader] = number;
                    break;
                }

                yield return number;
                previousNumber = number;
            }
            // if serie is empty, write -1 to the dictionary and break the loop
            else if (line == " ")
            {
                if (previousNumber > 0)
                {
                    _lastValues[fileAReader] = -1;
                }

                yield break;
            }
        }
    }
}