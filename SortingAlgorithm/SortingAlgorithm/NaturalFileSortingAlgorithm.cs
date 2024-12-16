namespace SortingAlgorithm.SortingAlgorithm;

public class NaturalFileSortingAlgorithm(ILogger logger) : IFileSortingAlgorithm {
    public void SortFile(string path) {
        logger.Log("Starting natural file sorting algorithm");

        string naturalFileSortingTempFolder = "NaturalFileSortingTemp";
        if (!Directory.Exists(naturalFileSortingTempFolder))
            Directory.CreateDirectory(naturalFileSortingTempFolder);

        string fileAPath = Path.Combine(naturalFileSortingTempFolder, "fileA.txt");
        string fileBPath = Path.Combine(naturalFileSortingTempFolder, "fileB.txt");
        string fileCPath = Path.Combine(naturalFileSortingTempFolder, "fileC.txt");

        logger.Log("Copying file contents");
        FileSortingAlgorithmHelpers.CopyFileContents(path, fileAPath);

        while (true) {
            logger.Log("Dividing file A in series");
            DivideFileAInSeries(fileAPath: fileAPath, fileBPath: fileBPath, fileCPath: fileCPath);

            if (IsFileEmpty(fileBPath) || IsFileEmpty(fileCPath))
                break;

            logger.Log("Merging series");
            MergeSeries(fileAPath: fileAPath, fileBPath: fileBPath, fileCPath: fileCPath);
        }
    }

    private void MergeSeries(string fileAPath, string fileBPath, string fileCPath) {
        using (StreamWriter fileAWriter = new(fileAPath))
        using (StreamReader fileBReader = new(fileBPath))
        using (StreamReader fileCReader = new(fileCPath)) {
            while (!fileBReader.EndOfStream || !fileCReader.EndOfStream || _lastValues.Count > 0) {
                IEnumerable<int> seriesB = ReadSeriesNonAlloc(fileBReader);
                IEnumerable<int> seriesC = ReadSeriesNonAlloc(fileCReader);

                using IEnumerator<int> seriesBEnumerator = seriesB.GetEnumerator();
                using IEnumerator<int> seriesCEnumerator = seriesC.GetEnumerator();

                bool seriesBHasNext = seriesBEnumerator.MoveNext();
                bool seriesCHasNext = seriesCEnumerator.MoveNext();

                while (true) {
                    // if (seriesBHasNext)
                    //     logger.Log("seriesBEnumerator.Current: " + seriesBEnumerator.Current);
                    //
                    // if (seriesCHasNext)
                    //     logger.Log("seriesCEnumerator.Current: " + seriesCEnumerator.Current);

                    if (!seriesBHasNext && !seriesCHasNext)
                        break;

                    if (!seriesBHasNext) {
                        fileAWriter.WriteLine(seriesCEnumerator.Current);
                        seriesCHasNext = seriesCEnumerator.MoveNext();
                        continue;
                    }

                    if (!seriesCHasNext) {
                        fileAWriter.WriteLine(seriesBEnumerator.Current);
                        seriesBHasNext = seriesBEnumerator.MoveNext();
                        continue;
                    }

                    if (seriesBEnumerator.Current < seriesCEnumerator.Current) {
                        fileAWriter.WriteLine(seriesBEnumerator.Current);
                        seriesBHasNext = seriesBEnumerator.MoveNext();
                    }
                    else {
                        fileAWriter.WriteLine(seriesCEnumerator.Current);
                        seriesCHasNext = seriesCEnumerator.MoveNext();
                    }
                }
            }
        }
    }

    private bool IsFileEmpty(string fileBPath) {
        using StreamReader fileBReader = new(fileBPath);
        return fileBReader.EndOfStream;
    }

    private void DivideFileAInSeries(string fileAPath, string fileBPath, string fileCPath) {
        using (StreamReader fileAReader = new StreamReader(fileAPath))
        using (StreamWriter fileBWriter = new StreamWriter(fileBPath))
        using (StreamWriter fileCWriter = new StreamWriter(fileCPath)) {
            StreamWriter currentSeriesWriter = fileBWriter;
            while (!fileAReader.EndOfStream || _lastValues.Count > 0) {
                IEnumerable<int> currentSeries = ReadSeriesNonAlloc(fileAReader);

                foreach (int number in currentSeries)
                    currentSeriesWriter.WriteLine(number);

                currentSeriesWriter = currentSeriesWriter == fileBWriter ? fileCWriter : fileBWriter;
            }
        }
    }

    private readonly Dictionary<StreamReader, int> _lastValues = new();

    private IEnumerable<int> ReadSeriesNonAlloc(StreamReader fileAReader) {
        int previousNumber = int.MinValue;

        if (_lastValues.TryGetValue(fileAReader, out int lastValue)) {
            previousNumber = lastValue;
            _lastValues.Remove(fileAReader);
            yield return lastValue;
        }

        while (!fileAReader.EndOfStream) {
            string? line = fileAReader.ReadLine();

            if (int.TryParse(line, out int number)) {
                if (number < previousNumber) {
                    _lastValues[fileAReader] = number;
                    break;
                }

                yield return number;
                previousNumber = number;
            }
            else {
                logger.Log($"Failed to parse line: {line}");
            }
        }
    }
}