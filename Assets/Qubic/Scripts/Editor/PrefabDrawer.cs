using System;
using UnityEditor;
using UnityEngine;

namespace QubicNS
{
    [CustomPropertyDrawer(typeof(Prefab))]
    public class PrefabDrawer : PropertyDrawer
    {
        private static readonly GUIContent removeButtonContent = new GUIContent("✖", "Remove Rule");
        private static readonly GUIContent addRuleButtonContent = new GUIContent("✚", "Add Rule");
        static GUIStyle titleStyle;
        // Red delete button on top of the element
        static GUIStyle redButtonStyle;
        static GUIStyle greenButtonStyle;

        static PrefabDrawer()
        {
            titleStyle = new GUIStyle(EditorStyles.largeLabel)
            {
                normal = { textColor = Color.white * 0.96f, background = MakeTex(2, 2, new Color(0.2f, 0.2f, 0.2f, 1)) },
                fontStyle = FontStyle.Bold,
            };

            redButtonStyle = new GUIStyle(EditorStyles.toolbarButton)
            {
                normal = { textColor = Color.red }
            };

            greenButtonStyle = new GUIStyle(EditorStyles.toolbarButton)
            {
                normal = { textColor = Color.green },
            };
        }

        public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
        {
            HandleGUI(position, property, isCalculatingHeight: false);
        }

        public override float GetPropertyHeight(SerializedProperty property, GUIContent label)
        {
            return HandleGUI(new Rect(0, 0, 0, 0), property, isCalculatingHeight: true);
        }

        bool Collapse;

        private float HandleGUI(Rect position, SerializedProperty property, bool isCalculatingHeight)
        {
            Collapse = (DateTime.Now - Prefab.collapseRequestTime).TotalSeconds < 1;

            float yOffset = 0f;

            if (!isCalculatingHeight)
                EditorGUI.BeginProperty(position, GUIContent.none, property);

            SerializedProperty rules = property.FindPropertyRelative("Rules");

            int indent = EditorGUI.indentLevel;
            //EditorGUI.indentLevel++;

            Rect rect = position;
            rect.height = EditorGUIUtility.singleLineHeight;
            rect.position = new Vector2(rect.position.x + 5, rect.position.y);
            rect.width -= 5;

            SerializedProperty type = property.FindPropertyRelative(nameof(Prefab.Type));
            var isContent = (PrefabType)type.intValue == PrefabType.Content;
            var isColumn = (PrefabType)type.intValue == PrefabType.Column;

            SerializedProperty inspectorName = property.FindPropertyRelative("_inspector_name");
            if (inspectorName != null)
            {
                if (!isCalculatingHeight)
                {
                    var buttonWidth = 25;
                    Rect labelRect = new Rect(rect.x, rect.y, rect.width - buttonWidth - 5, EditorGUIUtility.singleLineHeight);
                    Rect helpButtonRect = new Rect(rect.x + rect.width - buttonWidth - 2, rect.y, buttonWidth, EditorGUIUtility.singleLineHeight);

                    EditorGUI.LabelField(labelRect, inspectorName.stringValue, titleStyle);

                    if (GUI.Button(helpButtonRect, addRuleButtonContent, greenButtonStyle))
                    {
                        rules.arraySize++;
                    }
                }
                yOffset += EditorGUIUtility.singleLineHeight + EditorGUIUtility.standardVerticalSpacing;
                rect.y += EditorGUIUtility.singleLineHeight + EditorGUIUtility.standardVerticalSpacing;
            }

            // Type
            yOffset += DrawProperty(ref rect, property.FindPropertyRelative(nameof(Prefab.Type)), isCalculatingHeight);

            // PrefabInfo
            yOffset += DrawProperty(ref rect, property.FindPropertyRelative(nameof(Prefab.PrefabInfo)), isCalculatingHeight);

            // Levels
            yOffset += DrawProperty(ref rect, property.FindPropertyRelative(nameof(Prefab.Levels)), isCalculatingHeight);

            // SetTags
            yOffset += DrawProperty(ref rect, property.FindPropertyRelative(nameof(Prefab.SetTags)), isCalculatingHeight);

            // Type - dependent properties
            if (isContent)
            {
                yOffset += DrawProperty(ref rect, property.FindPropertyRelative(nameof(Prefab.SetTagsRadius)), isCalculatingHeight);
                yOffset += DrawProperty(ref rect, property.FindPropertyRelative(nameof(Prefab.ContentFeatures)), isCalculatingHeight);
            }
            else
            if (isColumn)
            {
                yOffset += DrawProperty(ref rect, property.FindPropertyRelative(nameof(Prefab.ColumnFeatures)), isCalculatingHeight);
            }
            else
            {
                yOffset += DrawProperty(ref rect, property.FindPropertyRelative(nameof(Prefab.WallFeatures)), isCalculatingHeight);
            }


            // Rules
            yOffset += DrawHeader(ref rect, new GUIContent("Rules"), isCalculatingHeight);

            for (int i = 0; i < rules.arraySize; i++)
            {
                SerializedProperty rule = rules.GetArrayElementAtIndex(i);

                if (Collapse)
                    rule.isExpanded = false;

                float propertyHeight = EditorGUI.GetPropertyHeight(rule, true);

                if (!isCalculatingHeight)
                {
                    float removeButtonWidth = 25f;
                    Rect ruleRect = new Rect(rect.x, rect.y, rect.width - removeButtonWidth - 4, EditorGUIUtility.singleLineHeight);
                    Rect removeButtonRect = new Rect(rect.x + rect.width - removeButtonWidth, rect.y, removeButtonWidth, EditorGUIUtility.singleLineHeight);

                    var r = (Rule)rule.boxedValue;
                    EditorGUI.PropertyField(ruleRect, rule, new GUIContent(r.GetTitle()), true);

                    // GUI.backgroundColor = Color.red;
                    if (GUI.Button(removeButtonRect, removeButtonContent, redButtonStyle))
                    {
                        rules.DeleteArrayElementAtIndex(i);
                        GUI.backgroundColor = Color.white;
                        break;
                    }
                    GUI.backgroundColor = Color.white;
                }

                yOffset += propertyHeight + EditorGUIUtility.standardVerticalSpacing;
                rect.y += propertyHeight + EditorGUIUtility.standardVerticalSpacing;
            }

            // space
            yOffset += EditorGUIUtility.singleLineHeight + EditorGUIUtility.standardVerticalSpacing;

            EditorGUI.indentLevel = indent;

            if (!isCalculatingHeight)
                EditorGUI.EndProperty();

            return yOffset;
        }

        private float DrawProperty(ref Rect rect, SerializedProperty property, bool isCalculatingHeight, GUIContent label = null)
        {
            if (property == null)
                return 0f;

            if (Collapse)
                property.isExpanded = false;

            float height = EditorGUI.GetPropertyHeight(property, true);

            if (!isCalculatingHeight)
            {
                EditorGUI.PropertyField(rect, property, label,  true);
            }

            rect.y += height + EditorGUIUtility.standardVerticalSpacing;
            return height + EditorGUIUtility.standardVerticalSpacing;
        }

        private float DrawHeader(ref Rect rect, GUIContent label, bool isCalculatingHeight)
        {
            if (!isCalculatingHeight)
            {
                EditorGUI.LabelField(rect, label, EditorStyles.boldLabel);
            }

            rect.y += EditorGUIUtility.singleLineHeight + EditorGUIUtility.standardVerticalSpacing;
            return EditorGUIUtility.singleLineHeight + EditorGUIUtility.standardVerticalSpacing;
        }

        // Function to create a colored texture
        private static Texture2D MakeTex(int width, int height, Color col)
        {
            Color[] pix = new Color[width * height];
            for (int i = 0; i < pix.Length; i++) pix[i] = col;
            Texture2D texture = new Texture2D(width, height);
            texture.SetPixels(pix);
            texture.Apply();
            return texture;
        }
    }
}