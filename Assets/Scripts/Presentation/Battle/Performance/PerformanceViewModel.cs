using System;
using System.Globalization;
using System.Text;
using Application.Services;
using R3;

namespace Presentation.Battle.Performance
{
    public sealed class PerformanceViewModel : IDisposable
    {
        private const float RefreshIntervalSeconds = 0.25f;

        private readonly IPerformanceMetrics _metrics;
        private readonly StringBuilder _builder = new(192);
        private float _elapsed;
        private int _frames;
        private bool _disposed;
        public ReactiveProperty<bool> IsVisible { get; } = new(false);
        public ReactiveProperty<string> ToggleLabel { get; } = new("PERF");
        public ReactiveProperty<string> Text { get; } = new("Collecting performance data...");
        public PerformanceViewModel(IPerformanceMetrics metrics) => _metrics = metrics;

        public void Toggle()
        {
            if (_disposed) return;
            IsVisible.Value = !IsVisible.Value;
            ToggleLabel.Value = IsVisible.Value ? "CLOSE" : "PERF";
        }

        public void Tick(float deltaTime)
        {
            if (_disposed || deltaTime <= 0f) return;
            _elapsed += deltaTime;
            _frames++;
            if (_elapsed < RefreshIntervalSeconds) return;
            var fps = _frames / _elapsed;
            var sample = _metrics.Capture();
            _builder.Clear();
            _builder.AppendLine("PERFORMANCE");
            _builder.Append("FPS          ").Append(fps.ToString("F1", CultureInfo.InvariantCulture)).Append(" / ")
                .Append(sample.TargetFrameRate).AppendLine();
            _builder.Append("Frame        ").Append((1000f / fps).ToString("F2", CultureInfo.InvariantCulture))
                .AppendLine(" ms");
            _builder.Append("Unity alloc  ").AppendLine(Megabytes(sample.Allocated));
            _builder.Append("Unity reserve ").AppendLine(Megabytes(sample.Reserved));
            _builder.Append("Managed heap ").AppendLine(Megabytes(sample.Managed));
            _builder.Append("Resolution   ").Append(sample.Width).Append(" x ").Append(sample.Height);
            Text.Value = _builder.ToString();
            _elapsed = 0f;
            _frames = 0;
        }

        public void Dispose()
        {
            if (_disposed) return;
            _disposed = true;
            IsVisible.Dispose();
            ToggleLabel.Dispose();
            Text.Dispose();
        }

        private static string Megabytes(long bytes) =>
            (bytes / (1024d * 1024d)).ToString("F1", CultureInfo.InvariantCulture) + " MB";
    }
}
