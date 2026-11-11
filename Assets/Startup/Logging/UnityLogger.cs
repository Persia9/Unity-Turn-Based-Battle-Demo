using UnityEngine;

namespace Core.Logging
{
    public sealed class UnityLogger : ILogger
    {
        public void Log(string message) => Debug.Log(message);
        public void Warning(string message) => Debug.LogWarning(message);
        public void Error(string message) => Debug.LogError(message);
    }
}