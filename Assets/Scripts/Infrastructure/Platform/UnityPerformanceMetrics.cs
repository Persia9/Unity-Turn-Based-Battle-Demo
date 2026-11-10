using System;
using Application.Services;
using UnityEngine;
using UnityEngine.Profiling;

namespace Infrastructure.Platform
{
    public sealed class UnityPerformanceMetrics : IPerformanceMetrics
    {
        public PerformanceMetrics Capture() => new(Profiler.GetTotalAllocatedMemoryLong(),
            Profiler.GetTotalReservedMemoryLong(), GC.GetTotalMemory(false),
            Screen.width, Screen.height, MobileRuntimeSettings.TargetFrameRate);
    }
}
