using UnityEditor;
using UnityEngine;

[InitializeOnLoad]
public static class HierarchyHighlighter
{
    static HierarchyHighlighter()
    {
        EditorApplication.hierarchyWindowItemOnGUI += OnHierarchyGUI;
    }

    private static void OnHierarchyGUI(int instanceID, Rect rect)
    {
#if UNITY_6000_0_OR_NEWER
        GameObject obj = EditorUtility.EntityIdToObject(instanceID) as GameObject;
#else
        GameObject obj = EditorUtility.InstanceIDToObject(instanceID) as GameObject;
#endif

        if (obj == null) return;

        string name = obj.name;

        // Separator
        if (name.StartsWith("---"))
        {
            DrawSeparator(rect, name);
            return;
        }

        DrawRowLine(rect);

        if (!obj.activeInHierarchy)
            DrawInactive(rect);

        DrawTag(rect, obj);
        DrawComponents(rect, obj);

        if (PrefabUtility.GetPrefabInstanceStatus(obj) == PrefabInstanceStatus.Connected)
            DrawPrefabDot(rect);

        if (HasMissing(obj))
            DrawWarning(rect);
    }

    #region Draw

    private static void DrawSeparator(Rect rect, string name)
    {
        EditorGUI.DrawRect(rect, LoadColor(
            "Hierarchy_Separator_Color",
            new Color(0.26f, 0.26f, 0.26f)
        ));

        string title = name.Replace("-", "").Trim();

        GUIStyle style = new GUIStyle(EditorStyles.boldLabel);
        style.normal.textColor = Color.white;
        style.alignment = TextAnchor.MiddleCenter;

        EditorGUI.LabelField(rect, title, style);

        DrawRowLine(rect);
    }

    private static void DrawRowLine(Rect rect)
    {
        EditorGUI.DrawRect(
            new Rect(rect.x, rect.yMax - 1, rect.width, 1),
            new Color(0f, 0f, 0f, 0.35f)
        );
    }

    private static void DrawInactive(Rect rect)
    {
        EditorGUI.DrawRect(rect, new Color(0f, 0f, 0f, 0.25f));
    }

    private static void DrawTag(Rect rect, GameObject obj)
    {
        if (obj.CompareTag("Untagged")) return;

        string tag = obj.tag;

        Color color = LoadColor(
            $"Hierarchy_Tag_Color_{tag}",
            LoadColor("Hierarchy_Tag_Color_Default", Color.white)
        );

        Rect r = new Rect(rect.xMax - 200, rect.y, 80, rect.height);

        GUIStyle style = new GUIStyle(EditorStyles.label);
        style.normal.textColor = color;
        style.alignment = TextAnchor.MiddleLeft;
        style.fontSize = 10;
        style.fontStyle = FontStyle.Bold;

        EditorGUI.LabelField(r, tag, style);
    }

    private static void DrawComponents(Rect rect, GameObject obj)
    {
        Component[] comps = obj.GetComponents<Component>();

        float x = rect.xMax - 40;
        int count = 0;

        foreach (Component c in comps)
        {
            if (c == null) continue;
            if (c is Transform) continue;

            Texture icon = EditorGUIUtility.ObjectContent(c, c.GetType()).image;
            if (icon == null) continue;

            GUI.DrawTexture(new Rect(x, rect.y, 16, 16), icon);

            x -= 18;
            count++;

            if (count >= 5) break;
        }
    }

    private static void DrawPrefabDot(Rect rect)
    {
        EditorGUI.DrawRect(
            new Rect(rect.xMax - 10, rect.y + 5, 5, 5),
            new Color(0.25f, 0.55f, 1f)
        );
    }

    private static void DrawWarning(Rect rect)
    {
        GUIStyle style = new GUIStyle(EditorStyles.label);
        style.normal.textColor = Color.red;

        EditorGUI.LabelField(
            new Rect(rect.xMax - 20, rect.y, 20, rect.height),
            "!",
            style
        );
    }

    #endregion

    #region Utils

    private static bool HasMissing(GameObject obj)
    {
        foreach (var c in obj.GetComponents<Component>())
            if (c == null) return true;

        return false;
    }

    private static Color LoadColor(string key, Color defaultColor)
    {
        string value = EditorPrefs.GetString(key, ColorUtility.ToHtmlStringRGBA(defaultColor));

        if (ColorUtility.TryParseHtmlString("#" + value, out Color color))
            return color;

        return defaultColor;
    }

    #endregion
}