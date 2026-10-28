using UnityEngine;

namespace Infrastructure.UI
{
    public static class SafeAreaUtility
    {
        public static void Apply(RectTransform target)
        {
            if (target == null) return;

            var width = Mathf.Max(1f, Screen.width);
            var height = Mathf.Max(1f, Screen.height);
            var safeArea = Screen.safeArea;
            target.anchorMin = new Vector2(safeArea.xMin / width, safeArea.yMin / height);
            target.anchorMax = new Vector2(safeArea.xMax / width, safeArea.yMax / height);
            target.offsetMin = Vector2.zero;
            target.offsetMax = Vector2.zero;
        }
    }
}
