namespace SortingAlgorithm.SortingAlgorithm;

public class FibonacciCalculator
{
    public long[] ComputeFibonacciDistribution(int totalRuns, int length, int m)
    {
        int order = m - 2;
        List<long> fib = new();

        for (int i = 0; i < order; i++)
            fib.Add(0);
        fib.Add(1);

        while (fib.Count < 1000) // generate enough numbers
        {
            long sum = 0;
            for (int i = 0; i < order + 1; i++)
            {
                sum += fib[fib.Count - i - 1];
            }

            fib.Add(sum);
        }

        long[] best = null;
        for (int start = order; start < fib.Count - length; start++)
        {
            var seg = fib.Skip(start).Take(length).ToArray();
            if (seg.Sum() >= totalRuns)
            {
                best = seg;
                Console.WriteLine("Fibonacci segment: " + string.Join(", ", fib.Take(start+length)));
                break;
            }
            
        }

        return best ?? fib.Skip(fib.Count - length).Take(length).ToArray();
    }
}