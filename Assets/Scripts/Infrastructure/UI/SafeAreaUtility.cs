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
            // 左右・上下それぞれの余白を揃え、回転後もコンテンツの中心を保つ。
            var horizontalInset = Mathf.Max(safeArea.xMin, width - safeArea.xMax);
            var verticalInset = Mathf.Max(safeArea.yMin, height - safeArea.yMax);
            target.anchorMin = new Vector2(horizontalInset / width, verticalInset / height);
            target.anchorMax = new Vector2(1f - horizontalInset / width, 1f - verticalInset / height);
            target.offsetMin = Vector2.zero;
            target.offsetMax = Vector2.zero;
        }
    }
}