namespace SortingAlgorithm.FIleGenerator;

public class RandomFileGenerator(Random random) : IRandomFileGenerator
{
    public void GenerateFileWithRandomInts(string path, MemorySize size)
    {
        double bytes = size.Size * Math.Pow(1024, (int)size.Unit);
        int numbersToWrite = (int)Math.Ceiling(bytes / sizeof(int));
        using (StreamWriter writer = new StreamWriter(path))
        {
            for (int i = 0; i < numbersToWrite; i++)
            {
                if (i != numbersToWrite - 1)
                    writer.WriteLine(random.NextInt64(0, 100));
                else
                    writer.Write(random.NextInt64(0, 100));
            }
        }
    }
}