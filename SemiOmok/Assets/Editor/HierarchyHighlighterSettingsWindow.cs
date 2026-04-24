using UnityEditor;
using UnityEngine;

public class HierarchyHighlighterSettingsWindow : EditorWindow
{
    private Color backgroundColor;
    private Color separatorColor;
    private Color defaultTagColor;

    private Vector2 scrollPos;

    [MenuItem("Tools/Hierarchy Highlighter Settings")]
    public static void OpenWindow()
    {
        GetWindow<HierarchyHighlighterSettingsWindow>("Hierarchy Settings");
    }

    private void OnEnable()
    {
        backgroundColor = LoadColor(
            "Hierarchy_Background_Color",
            new Color(0.18f, 0.18f, 0.18f, 0.35f)
        );

        separatorColor = LoadColor(
            "Hierarchy_Separator_Color",
            new Color(0.26f, 0.26f, 0.26f, 1f)
        );

        defaultTagColor = LoadColor(
            "Hierarchy_Tag_Color_Default",
            Color.white
        );
    }

    private void OnGUI()
    {
        EditorGUILayout.LabelField("Hierarchy Highlighter Settings", EditorStyles.boldLabel);
        EditorGUILayout.Space(10);

        DrawRowColorSettings();

        EditorGUILayout.Space(10);

        DrawTagColorSettings();

        EditorGUILayout.Space(15);

        DrawButtons();
    }

    private void DrawRowColorSettings()
    {
        EditorGUILayout.LabelField("Row Colors", EditorStyles.boldLabel);

        backgroundColor = EditorGUILayout.ColorField(
            "Row Background Color",
            backgroundColor
        );

        separatorColor = EditorGUILayout.ColorField(
            "Separator Color",
            separatorColor
        );
    }

    private void DrawTagColorSettings()
    {
        EditorGUILayout.LabelField("Unity Tag Text Colors", EditorStyles.boldLabel);

        defaultTagColor = EditorGUILayout.ColorField(
            "Default Tag Color",
            defaultTagColor
        );

        EditorGUILayout.Space(5);

        string[] tags = UnityEditorInternal.InternalEditorUtility.tags;

        if (tags == null || tags.Length == 0)
        {
            EditorGUILayout.HelpBox("등록된 Unity Tag가 없습니다.", MessageType.Info);
            return;
        }

        scrollPos = EditorGUILayout.BeginScrollView(scrollPos, GUILayout.Height(220));

        foreach (string tag in tags)
        {
            if (tag == "Untagged")
                continue;

            string key = GetTagColorKey(tag);

            Color currentColor = LoadColor(key, defaultTagColor);
            Color newColor = EditorGUILayout.ColorField($"{tag} Tag", currentColor);

            if (newColor != currentColor)
            {
                SaveColor(key, newColor);
                EditorApplication.RepaintHierarchyWindow();
            }
        }

        EditorGUILayout.EndScrollView();
    }

    private void DrawButtons()
    {
        if (GUILayout.Button("Save Settings"))
        {
            SaveColor("Hierarchy_Background_Color", backgroundColor);
            SaveColor("Hierarchy_Separator_Color", separatorColor);
            SaveColor("Hierarchy_Tag_Color_Default", defaultTagColor);

            EditorApplication.RepaintHierarchyWindow();
        }

        if (GUILayout.Button("Reset Settings"))
        {
            EditorPrefs.DeleteKey("Hierarchy_Background_Color");
            EditorPrefs.DeleteKey("Hierarchy_Separator_Color");
            EditorPrefs.DeleteKey("Hierarchy_Tag_Color_Default");

            string[] tags = UnityEditorInternal.InternalEditorUtility.tags;

            foreach (string tag in tags)
            {
                EditorPrefs.DeleteKey(GetTagColorKey(tag));
            }

            OnEnable();
            Repaint();
            EditorApplication.RepaintHierarchyWindow();
        }
    }

    private static string GetTagColorKey(string tag)
    {
        return $"Hierarchy_Tag_Color_{tag}";
    }

    private static Color LoadColor(string key, Color defaultColor)
    {
        string value = EditorPrefs.GetString(
            key,
            ColorUtility.ToHtmlStringRGBA(defaultColor)
        );

        if (ColorUtility.TryParseHtmlString("#" + value, out Color color))
            return color;

        return defaultColor;
    }

    private static void SaveColor(string key, Color color)
    {
        EditorPrefs.SetString(
            key,
            ColorUtility.ToHtmlStringRGBA(color)
        );
    }
}