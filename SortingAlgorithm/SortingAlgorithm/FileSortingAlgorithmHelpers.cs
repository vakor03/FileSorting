public static class FileSortingAlgorithmHelpers
{
    public static void CopyFileContents(string path, string fileAPath) {
        using (StreamWriter fileAWriter = new StreamWriter(fileAPath))
        using (StreamReader streamReader = new StreamReader(path)) {
            while (!streamReader.EndOfStream) {
                string? line = streamReader.ReadLine();
                if (line == null)
                    return;

                fileAWriter.WriteLine(line);
            }
        }
    }
}