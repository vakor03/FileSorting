using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

public class FileChunkSorter {
    private const int ChunkSizeBytes = 500 * 1024 * 1024; // 500MB
    private const int OverReadBytes = 1024 * 1024; // 1MB over-read buffer to find newline boundary

    private readonly Encoding _encoding = new UTF8Encoding(false); // No BOM

    public void SortFileInChunks(string inputPath, string outputPath) {
        using var fsIn = new FileStream(inputPath, FileMode.Open, FileAccess.Read, FileShare.Read);
        using var fsOut = new FileStream(outputPath, FileMode.Create, FileAccess.Write, FileShare.None);
        using var writer = new StreamWriter(fsOut, _encoding);
        using var reader = new StreamReader(fsIn, _encoding);

        int[] integersBuffer = new int[ChunkSizeBytes/sizeof(int)];
        int i = 0;
        while (!reader.EndOfStream) {
            integersBuffer[i] = int.Parse(reader.ReadLine()!);
            i++;
            if (i == integersBuffer.Length) {
                Array.Sort(integersBuffer);
                foreach (int value in integersBuffer)
                    writer.WriteLine(value);
                i = 0;
            }
        }

        Array.Sort(integersBuffer, 0, i);
        for (var j = 0; j < i; j++) {
            if (j == i) {
                writer.Write(integersBuffer[i]);
            }
            else
                writer.WriteLine(integersBuffer[i]);
        }
    }
}