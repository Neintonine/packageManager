using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

namespace Package_Manager
{
    public static class EditorListDisplay
    {
        
        
        public static bool Display<TSource>(bool isFoldout, string name, IList<TSource> list, Action<int> contentDisplay, Action add = null, Action<TSource> remove = null, Action repaint = null)
        {
            return Display(isFoldout, name, EditorStyles.foldoutHeader, list, contentDisplay, add, remove, repaint);
        }
        public static bool Display<TSource>(bool isFoldout, string name, GUIStyle style, IList<TSource> list, Action<int> contentDisplay, Action add = null, Action<TSource> remove = null, Action repaint = null)
        {
            if (list == null) return isFoldout;
            
            string countStr = list.Count.ToString();

            EditorGUILayout.BeginHorizontal();
            bool foldout = EditorGUILayout.BeginFoldoutHeaderGroup(isFoldout, name, style);
            EditorGUILayout.LabelField(countStr, EditorStyles.miniLabel, GUILayout.MaxWidth(10 + ((countStr.Length - 1) * 5)));
            if (add != null && GUILayout.Button("+", GUILayout.MaxWidth(20)))
            {
                add();
                repaint?.Invoke();
            }
            EditorGUILayout.EndHorizontal();


            if (foldout)
            {
                if (list.Count < 1)
                {
                    EditorGUI.indentLevel = 2;

                    EditorGUILayout.LabelField("No items", EditorStyles.miniLabel);

                    EditorGUI.indentLevel = 0;
                }
                else
                {
                    for (int i = 0; i < list.Count; i++)
                    {
                        EditorGUI.indentLevel = 1;
                        EditorGUILayout.BeginHorizontal();
                        EditorGUILayout.BeginVertical();
                        contentDisplay(i);
                        EditorGUILayout.EndVertical();
                        if (remove != null && GUILayout.Button("-", GUILayout.MaxWidth(20)))
                        {
                            remove(list[i]);
                            repaint?.Invoke();
                        }
                        EditorGUILayout.EndHorizontal();
                    }
                    EditorGUI.indentLevel = 0;

                }
            }

            EditorGUILayout.EndFoldoutHeaderGroup();

            return foldout;
        }
    }
}