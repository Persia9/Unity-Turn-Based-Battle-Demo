using Zenject;
using UnityEngine;

namespace Infrastructure.Platform
{
    public sealed class MobileRuntimeSettings : IInitializable
    {
        public void Initialize()
        {
            if (!UnityEngine.Application.isMobilePlatform) return;

            QualitySettings.vSyncCount = 0;
            UnityEngine.Application.targetFrameRate = 60;
            Screen.sleepTimeout = SleepTimeout.SystemSetting;
        }
    }
}
