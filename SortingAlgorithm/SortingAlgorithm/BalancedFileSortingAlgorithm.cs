namespace SortingAlgorithm.SortingAlgorithm {
    public class BalancedFileSortingAlgorithm(ILogger logger, int m = 5) : IFileSortingAlgorithm {
        public void SortFile(string path) {
            logger.Log("Starting balanced file sorting algorithm");

            string balancedFileSortingTempFolder = "BalancedFileSortingTemp";
            if (!Directory.Exists(balancedFileSortingTempFolder))
                Directory.CreateDirectory(balancedFileSortingTempFolder);

            string fileAPath = Path.Combine(balancedFileSortingTempFolder, "fileA.txt");
            string[] filesB = CreateAdditionalFiles("fileB", m, balancedFileSortingTempFolder);
            string[] filesC = CreateAdditionalFiles("fileC", m, balancedFileSortingTempFolder);

            FileSortingAlgorithmHelpers.CopyFileContents(path, fileAPath);

            DivideFileAInSeries(fileAPath, filesB);

            while (true) {
                ClearFiles(filesC);
                TraverseSeries(filesB, filesC);
                if (IsFileEmpty(filesC[1])) {
                    FileSortingAlgorithmHelpers.CopyFileContents(filesC[0], fileAPath);
                    break;
                }

                ClearFiles(filesB);
                TraverseSeries(filesC, filesB);
                if (IsFileEmpty(filesB[1])) {
                    FileSortingAlgorithmHelpers.CopyFileContents(filesB[0], fileAPath);
                    break;
                }
            }
        }
        
        private bool IsFileEmpty(string fileBPath) {
            using StreamReader fileBReader = new(fileBPath);
            return fileBReader.EndOfStream;
        }

        private void ClearFiles(string[] filesB) {
            foreach (string file in filesB)
                File.WriteAllText(file, string.Empty);
        }

        private void TraverseSeries(string[] from, string[] to) {
            StreamReader[] fromReaders = new StreamReader[from.Length];
            for (int i = 0; i < fromReaders.Length; i++)
                fromReaders[i] = new StreamReader(from[i]);

            StreamWriter[] toWriters = new StreamWriter[to.Length];
            for (int i = 0; i < toWriters.Length; i++)
                toWriters[i] = new StreamWriter(to[i]);

            StreamWriter currentSeriesWriter = toWriters[0];

            while (fromReaders.Any(el => !el.EndOfStream) || _lastValues.Count > 0) {
                IEnumerable<int>[] series = fromReaders.Select(ReadSeriesNonAlloc).ToArray();
                IEnumerator<int>[] seriesEnumerators = series.Select(el => el.GetEnumerator()).ToArray();

                Dictionary<IEnumerator<int>, bool> seriesHasNext = seriesEnumerators.ToDictionary(el => el, el => el.MoveNext());

                while (true) {
                    if (seriesHasNext.Values.All(el => !el))
                        break;

                    IEnumerator<int> enumeratorWithMaxValue = seriesEnumerators
                        .Where(el => seriesHasNext[el])
                        .First(el => el.Current == seriesEnumerators.Where(enumerator=>seriesHasNext[enumerator]).Min(maxValue => maxValue.Current));

                    currentSeriesWriter.WriteLine(enumeratorWithMaxValue.Current);
                    seriesHasNext[enumeratorWithMaxValue] = enumeratorWithMaxValue.MoveNext();
                }

                currentSeriesWriter = GetNextSeriesWriter(currentSeriesWriter, toWriters);

                foreach (IEnumerator<int> seriesEnumerator in seriesEnumerators)
                    seriesEnumerator.Dispose();
            }

            foreach (StreamWriter streamWriter in toWriters)
                streamWriter.Dispose();

            foreach (StreamReader streamReader in fromReaders)
                streamReader.Dispose();
        }

        private void DivideFileAInSeries(string fileAPath, string[] filesB) {
            StreamWriter[] fileBWriters = new StreamWriter[filesB.Length];
            using (StreamReader fileAReader = new StreamReader(fileAPath)) {
                for (int i = 0; i < fileBWriters.Length; i++)
                    fileBWriters[i] = new StreamWriter(filesB[i]);

                StreamWriter currentSeriesWriter = fileBWriters[0];
                while (!fileAReader.EndOfStream || _lastValues.Count > 0) {
                    IEnumerable<int> currentSeries = ReadSeriesNonAlloc(fileAReader);

                    foreach (int number in currentSeries)
                        currentSeriesWriter.WriteLine(number);

                    currentSeriesWriter = GetNextSeriesWriter(currentSeriesWriter, fileBWriters);
                }

                foreach (StreamWriter streamWriter in fileBWriters)
                    streamWriter.Dispose();
            }
        }

        private Dictionary<StreamReader, int> _lastValues = new();

        private StreamWriter GetNextSeriesWriter(StreamWriter currentSeriesWriter, StreamWriter[] fileBWriters) {
            for (int i = 0; i < fileBWriters.Length; i++) {
                if (fileBWriters[i] == currentSeriesWriter) {
                    if (i == fileBWriters.Length - 1)
                        return fileBWriters[0];
                    else
                        return fileBWriters[i + 1];
                }
            }

            throw new Exception("Current series writer not found in fileB writers");
        }

        private string[] CreateAdditionalFiles(string fileBase, int count, string parent) {
            string[] filePaths = new string[count];
            for (int i = 0; i < count; i++)
                filePaths[i] = Path.Combine(parent, fileBase + i + ".txt");

            return filePaths;
        }

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
}