namespace SortingAlgorithm.SortingAlgorithm;

public class ConsoleLogger : ILogger {
    public void Log(string message) =>
        Console.WriteLine(message);

    public void Dispose() { }
}

public class FileLogger : ILogger {
    private readonly StreamWriter _writer;

    public FileLogger(string fileName) =>
        _writer = new StreamWriter(fileName);

    public void Log(string message) =>
        _writer.WriteLine(message);

    public void Dispose() =>
        _writer.Dispose();
}

public class CombinedLogger : ILogger {
    private readonly ILogger[] _loggers;

    public CombinedLogger(params ILogger[] loggers) =>
        _loggers = loggers;

    public void Dispose() {
        foreach (ILogger logger in _loggers)
            logger.Dispose();
    }

    public void Log(string message) {
        foreach (ILogger logger in _loggers) {
            logger.Log(message);
        }
    }
}