using System;
using UnityEditor;
using UnityEngine;
using Object = UnityEngine.Object;

namespace Package_Manager
{
    public class EditorGUIExt
    {
        public static readonly Color DEFAULT_COLOR = new Color(0f, 0f, 0f, 0.3f);
        public static readonly Vector2 DEFAULT_LINE_MARGIN = new Vector2(2f, 1f);

        public const float DEFAULT_LINE_HEIGHT = 2f;
        
        public static bool CheckedObjectField(string title, Object obj, Type objType, Func<Object, bool> checkAction, out Object result)
        {
            EditorGUI.BeginChangeCheck();
            result = EditorGUILayout.ObjectField(title, obj, objType, false);
            bool changed = EditorGUI.EndChangeCheck();
            if (!changed) return false;

            if (result == null || !checkAction(result)) result = null;
            return true;
        }
        
        public static void HeaderField(string name)
        {
            EditorGUILayout.Space();
            EditorGUILayout.LabelField(name, EditorStyles.boldLabel);
            HorizontalLine(2);
        }
        
        #region HorizontalLine

        public static void HorizontalLine(float height) => HorizontalLine(DEFAULT_COLOR, height, DEFAULT_LINE_MARGIN);
        public static void HorizontalLine(Color color, float height, Vector2 margin)
        {
            GUILayout.Space(margin.x);

            EditorGUI.DrawRect(EditorGUILayout.GetControlRect(false, height), color);

            GUILayout.Space(margin.y);
        }

        #endregion
        
    }
    
}
