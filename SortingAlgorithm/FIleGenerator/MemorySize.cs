namespace SortingAlgorithm.FIleGenerator;

public struct MemorySize {
    public float Size { get; set; }
    public MemorySizeUnit Unit { get; set; }

    public MemorySize(float size, MemorySizeUnit unit) {
        Size = size;
        Unit = unit;
    }
}