namespace Application.Services
{
    public readonly struct PerformanceMetrics
    {
        public readonly long Allocated, Reserved, Managed;
        public readonly int Width, Height, TargetFrameRate;
        public PerformanceMetrics(long allocated, long reserved, long managed, int width, int height, int targetFrameRate)
        { Allocated = allocated; Reserved = reserved; Managed = managed; Width = width; Height = height; TargetFrameRate = targetFrameRate; }
    }
    public interface IPerformanceMetrics { PerformanceMetrics Capture(); }
}
