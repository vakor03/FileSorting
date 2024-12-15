namespace SortingAlgorithm.SortingAlgorithm;

public class StraightMergeSort(ILogger logger) : IFileSortingAlgorithm {
    public void SortFile(string path) {
        logger.Log("Starting straight merge sort");
            
        string straighMergeSortTempFolder = "StraightMergeSortTemp";
        if (!Directory.Exists(straighMergeSortTempFolder))
            Directory.CreateDirectory(straighMergeSortTempFolder);

        string fileAPath = Path.Combine(straighMergeSortTempFolder, "fileA.txt");
        string fileBPath = Path.Combine(straighMergeSortTempFolder, "fileB.txt");
        string fileCPath = Path.Combine(straighMergeSortTempFolder, "fileC.txt");

        logger.Log("Copying file contents");
        FileSortingAlgorithmHelpers.CopyFileContents(path, fileAPath);

        int seriesSize = 1;
        while (true) {
            logger.Log($"Dividing file A in series of size {seriesSize}");
            DivideFileAInSeries(fileBPath, fileCPath, fileAPath, seriesSize);

            if (IsFileEmpty(fileBPath) || IsFileEmpty(fileCPath))
                break;

            logger.Log("Merging series");
            MergeSeries(fileBPath, fileCPath, fileAPath, seriesSize);

            // Подвоюємо розмір серій
            seriesSize *= 2;
        }
    }

    private static void DivideFileAInSeries(string fileBPath, string fileCPath, string fileAPath, int seriesSize) {
        int i = 0;
        using (BufferedStreamWriter fileBWriter = new BufferedStreamWriter(fileBPath))
        using (BufferedStreamWriter fileCWriter = new BufferedStreamWriter(fileCPath))
        using (StreamReader fileAReader = new StreamReader(fileAPath)) {
            while (!fileAReader.EndOfStream) {
                string? line = fileAReader.ReadLine();
                if (line == null)
                    return;

                int number = int.Parse(line);
                if ((i / seriesSize) % 2 == 0)
                    fileBWriter.WriteLine(number);
                else
                    fileCWriter.WriteLine(number);

                i++;
            }
        }
    }

    private static void MergeSeries(string fileBPath, string fileCPath, string fileAPath, int seriesSize) {
        using (StreamReader fileBReader = new StreamReader(fileBPath))
        using (StreamReader fileCReader = new StreamReader(fileCPath))
        using (BufferedStreamWriter fileAWriter = new BufferedStreamWriter(fileAPath)) {
            List<int> seriesB = new(seriesSize);
            List<int> seriesC = new(seriesSize);

            while (!fileBReader.EndOfStream || !fileCReader.EndOfStream) {
                seriesB.Clear();
                seriesC.Clear();
                    
                ReadSeries(seriesSize, fileBReader, seriesB);
                    
                ReadSeries(seriesSize, fileCReader, seriesC);

                WriteSortedSeriesToFile(seriesB, seriesC, fileAWriter, out int indexB, out int indexC);

                AddLeftoverSeries(indexB, seriesB, fileAWriter);
                    
                AddLeftoverSeries(indexC, seriesC, fileAWriter);
            }
        }
    }

    private static void WriteSortedSeriesToFile(List<int> seriesB, List<int> seriesC, BufferedStreamWriter fileAWriter, out int indexB, out int indexC) {
        indexB = 0;
        indexC = 0;
        while (indexB < seriesB.Count && indexC < seriesC.Count)
            if (seriesB[indexB] <= seriesC[indexC]) {
                fileAWriter.WriteLine(seriesB[indexB]);
                indexB++;
            }
            else {
                fileAWriter.WriteLine(seriesC[indexC]);
                indexC++;
            }
    }

    private static void AddLeftoverSeries(int indexB, List<int> seriesB, BufferedStreamWriter fileAWriter) {
        while (indexB < seriesB.Count) {
            fileAWriter.WriteLine(seriesB[indexB]);
            indexB++;
        }
    }

    private static void ReadSeries(int seriesSize, StreamReader fileBReader, List<int> seriesB) {
        for (int i = 0; i < seriesSize && !fileBReader.EndOfStream; i++) {
            string? line = fileBReader.ReadLine();
            if (line != null)
                seriesB.Add(int.Parse(line));
        }
    }

    private static bool IsFileEmpty(string filePath) {
        using (StreamReader reader = new StreamReader(filePath)) {
            return reader.EndOfStream;
        }
    }
}