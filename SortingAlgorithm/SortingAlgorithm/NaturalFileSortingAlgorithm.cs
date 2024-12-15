namespace SortingAlgorithm.SortingAlgorithm;

public class NaturalFileSortingAlgorithm(ILogger logger) : IFileSortingAlgorithm
{
    public void SortFile(string path)
    {
        logger.Log("Starting natural file sorting algorithm");

        string naturalFileSortingTempFolder = "NaturalFileSortingTemp";
        if (!Directory.Exists(naturalFileSortingTempFolder))
            Directory.CreateDirectory(naturalFileSortingTempFolder);

        string fileAPath = Path.Combine(naturalFileSortingTempFolder, "fileA.txt");
        string fileBPath = Path.Combine(naturalFileSortingTempFolder, "fileB.txt");
        string fileCPath = Path.Combine(naturalFileSortingTempFolder, "fileC.txt");

        logger.Log("Copying file contents");
        FileSortingAlgorithmHelpers.CopyFileContents(path, fileAPath);

        int seriesSize = 1;

        while (true)
        {
            DivideFileAInSeries(fileAPath: fileAPath, fileBPath: fileBPath, fileCPath: fileCPath);

            if (IsFileEmpty(fileBPath) || IsFileEmpty(fileCPath))
                break;

            MergeSeries(fileAPath: fileAPath, fileBPath: fileBPath, fileCPath: fileCPath);
        }
    }

    private void MergeSeries(string fileAPath, string fileBPath, string fileCPath)
    {
        using (StreamWriter fileAWriter = new(fileAPath))
        using (StreamReader fileBReader = new(fileBPath))
        using (StreamReader fileCReader = new(fileCPath))
        {
            while (!fileBReader.EndOfStream || !fileCReader.EndOfStream)
            {
                IEnumerable<int> seriesB = ReadSeriesNonAlloc(fileBReader);
                IEnumerable<int> seriesC = ReadSeriesNonAlloc(fileCReader);

                IEnumerator<int> seriesBEnumerator = seriesB.GetEnumerator();
                IEnumerator<int> seriesCEnumerator = seriesC.GetEnumerator();

                bool seriesBHasNext = true;
                bool seriesCHasNext = true;
                
                while (true)
                {
                    if (!seriesBHasNext && !seriesCHasNext)
                        break;

                    if (!seriesBHasNext)
                    {
                        fileAWriter.WriteLine(seriesCEnumerator.Current);
                        continue;
                    }

                    if (!seriesCHasNext)
                    {
                        fileAWriter.WriteLine(seriesBEnumerator.Current);
                        continue;
                    }

                    if (seriesBEnumerator.Current < seriesCEnumerator.Current)
                    {
                        fileAWriter.WriteLine(seriesBEnumerator.Current);
                        continue;
                    }

                    fileAWriter.WriteLine(seriesCEnumerator.Current);

                    seriesBHasNext = seriesBEnumerator.MoveNext();
                    seriesCHasNext = seriesCEnumerator.MoveNext();
                }
            }
        }
    }

    private bool IsFileEmpty(string fileBPath)
    {
        using StreamReader fileBReader = new(fileBPath);
        return fileBReader.EndOfStream;
    }

    private void DivideFileAInSeries(string fileAPath, string fileBPath, string fileCPath)
    {
        using (StreamReader fileAReader = new StreamReader(fileAPath))
        using (StreamWriter fileBWriter = new StreamWriter(fileBPath))
        using (StreamWriter fileCWriter = new StreamWriter(fileCPath))
        {
            StreamWriter currentSeriesWriter = fileBWriter;
            while (!fileAReader.EndOfStream)
            {
                IEnumerable<int> currentSeries = ReadSeriesNonAlloc(fileAReader);

                foreach (int number in currentSeries)
                    currentSeriesWriter.WriteLine(number);

                currentSeriesWriter = currentSeriesWriter == fileBWriter ? fileCWriter : fileBWriter;
            }
        }
    }

    private IEnumerable<int> ReadSeriesNonAlloc(StreamReader fileAReader)
    {
        int previousNumber = int.MinValue;
        while (!fileAReader.EndOfStream)
        {
            string? line = fileAReader.ReadLine();
            if (String.IsNullOrEmpty(line))
                yield break;

            int number = int.Parse(line);

            if (number < previousNumber)
            {
                fileAReader.BaseStream.Seek(-line.Length, SeekOrigin.Current);
                break;
            }

            yield return number;
            previousNumber = number;
        }
    }
}