using Zenject;
using UnityEngine;

namespace Infrastructure.Platform
{
    public sealed class MobileRuntimeSettings : IInitializable
    {
        public const int TargetFrameRate = 60;

        public void Initialize()
        {
            QualitySettings.vSyncCount = 0;
            UnityEngine.Application.targetFrameRate = TargetFrameRate;
        }
    }
}