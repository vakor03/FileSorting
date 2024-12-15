using System.Text;

namespace SortingAlgorithm.SortingAlgorithm {
    public interface IFileSortingAlgorithm {
        public void SortFile(string path);
    }

    public interface ILogger {
        public void Log(string message);
    }
    
    public class ConsoleLogger : ILogger {
        public void Log(string message) =>
            Console.WriteLine(message);
    }
    
    public class BufferedStreamWriter : IDisposable {
        private readonly StreamWriter _streamWriter;
        private readonly string _path;
        
        private readonly int _lineBufferSize;
        private readonly List<string> _lineBuffer;
        
        private readonly StringBuilder _stringBuilder = new();
        
        public BufferedStreamWriter(string path, int lineBufferSize = 1000) {
            _path = path;
            _lineBufferSize = lineBufferSize;
            _lineBuffer = new(lineBufferSize);
            _streamWriter = new(path);
        }
        
        public void WriteLine(string line) {
            _lineBuffer.Add(line);
            if (_lineBuffer.Count >= _lineBufferSize)
            Flush();
            // _streamWriter.WriteLine(line);
        }
        
        public void WriteLine(int number) =>
            WriteLine(number.ToString());

        private void Flush() {
            foreach (string line in _lineBuffer)
                _streamWriter.WriteLine(line);
                // _stringBuilder.AppendLine(line);
            // _streamWriter.Write(_stringBuilder.ToString());
            // _stringBuilder.Clear();
            _lineBuffer.Clear();
        }

        public void Dispose() {
            Flush();
            _streamWriter.Dispose();
        }
    }
}