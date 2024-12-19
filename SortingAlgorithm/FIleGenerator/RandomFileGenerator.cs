using System.Text;

namespace SortingAlgorithm.FIleGenerator;

public class RandomFileGenerator(Random random) : IRandomFileGenerator
{
    public void GenerateFileWithRandomInts(string path, MemorySize size)
{
    // Calculate target file size in bytes
    long targetBytes = (long)(size.Size * Math.Pow(1024, (int)size.Unit));

    // Use a known encoding (UTF-8 without BOM) for consistent measurement
    var encoding = new UTF8Encoding(encoderShouldEmitUTF8Identifier: false);
    byte[] newlineBytes = encoding.GetBytes(Environment.NewLine);

    using (var fs = new FileStream(path, FileMode.Create, FileAccess.Write, FileShare.None))
    using (var writer = new StreamWriter(fs, encoding))
    {
        long bytesWritten = 0;
        Random rnd = new Random();

        while (bytesWritten < targetBytes)
        {
            int nextInt = rnd.Next(0, Int32.MaxValue);
            string stringToWrite = nextInt.ToString();

            // Convert the line + newline to bytes
            byte[] lineWithNewlineBytes = encoding.GetBytes(stringToWrite);
            long lineLengthWithNewline = lineWithNewlineBytes.Length + newlineBytes.Length;

            // Check if we can fit the line with a newline
            if (bytesWritten + lineLengthWithNewline <= targetBytes)
            {
                // Fits with newline
                fs.Write(lineWithNewlineBytes, 0, lineWithNewlineBytes.Length);
                fs.Write(newlineBytes, 0, newlineBytes.Length);
                bytesWritten += lineLengthWithNewline;
            }
            else
            {
                // Can't fit line + newline fully. 
                // Try just the line without newline:
                if (bytesWritten + lineWithNewlineBytes.Length <= targetBytes)
                {
                    // Fits without the newline
                    fs.Write(lineWithNewlineBytes, 0, lineWithNewlineBytes.Length);
                    bytesWritten += lineWithNewlineBytes.Length;
                }
                // If we wanted to be very precise, we could consider partial writes of the line here,
                // but typically we just stop once we can't fit a full line.
                break;
            }
        }
    }
}

}