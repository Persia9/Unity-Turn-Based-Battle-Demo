using System;
using System.Text;
using UnityEngine;
using UnityEngine.Profiling;

namespace Presentation.Battle.Performance
{
    public sealed class PerformanceView : MonoBehaviour
    {
        private const float RefreshInterval = 0.25f;
        private const float Width = 270f;
        private const float Height = 158f;
        private readonly StringBuilder _builder = new(192);
        private float _elapsed;
        private int _frames;
        private string _displayText = "Collecting performance data...";
        private GUIStyle _labelStyle;
        private GUIStyle _boxStyle;

        private void Awake()
        {
#if !UNITY_EDITOR && !DEVELOPMENT_BUILD
            gameObject.SetActive(false);
#endif
        }

        private void Update()
        {
            _elapsed += Time.unscaledDeltaTime;
            _frames++;
            if (_elapsed < RefreshInterval) return;

            var fps = _elapsed > 0f ? _frames / _elapsed : 0f;
            var frameMilliseconds = fps > 0f ? 1000f / fps : 0f;
            BuildDisplayText(fps, frameMilliseconds);
            _elapsed = 0f;
            _frames = 0;
        }

        private void BuildDisplayText(float fps, float frameMilliseconds)
        {
            _builder.Clear();
            _builder.AppendLine("PERFORMANCE");
            _builder.Append("FPS          ").Append(fps.ToString("F1")).AppendLine();
            _builder.Append("Frame        ").Append(frameMilliseconds.ToString("F2")).AppendLine(" ms");
            _builder.Append("Unity alloc  ").AppendLine(FormatBytes(Profiler.GetTotalAllocatedMemoryLong()));
            _builder.Append("Unity reserve ").AppendLine(FormatBytes(Profiler.GetTotalReservedMemoryLong()));
            _builder.Append("Managed heap ").AppendLine(FormatBytes(GC.GetTotalMemory(false)));
            _builder.Append("Resolution   ").Append(Screen.width).Append(" x ").Append(Screen.height);
            _displayText = _builder.ToString();
        }

        private void OnGUI()
        {
            EnsureStyles();
            GUI.depth = -1000;
            var previousMatrix = GUI.matrix;
            var scale = Mathf.Clamp(Screen.height / 720f, 1f, 2f);
            GUI.matrix = Matrix4x4.Scale(new Vector3(scale, scale, 1f));
            var safeArea = Screen.safeArea;
            var safeXMin = safeArea.xMin / scale;
            var safeXMax = safeArea.xMax / scale;
            var safeY = (Screen.height - safeArea.yMax) / scale;
            var rect = new Rect(Mathf.Max(safeXMin + 8f, safeXMax - Width - 12f), safeY + 12f, Width, Height);
            GUI.Box(rect, GUIContent.none, _boxStyle);
            GUI.Label(new Rect(rect.x + 12f, rect.y + 8f, rect.width - 24f, rect.height - 16f),
                _displayText, _labelStyle);
            GUI.matrix = previousMatrix;
        }

        private void EnsureStyles()
        {
            if (_labelStyle != null) return;
            _labelStyle = new GUIStyle(GUI.skin.label)
            {
                fontSize = 14,
                alignment = TextAnchor.UpperLeft
            };
            _labelStyle.normal.textColor = Color.white;
            _boxStyle = new GUIStyle(GUI.skin.box);
        }

        private static string FormatBytes(long bytes)
        {
            const double megabyte = 1024d * 1024d;
            return $"{bytes / megabyte:F1} MB";
        }
    }
}
