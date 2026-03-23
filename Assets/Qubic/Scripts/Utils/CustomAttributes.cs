using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text.RegularExpressions;
#if UNITY_EDITOR
using UnityEditor;
#endif
using UnityEngine;

namespace QubicNS
{
    public class WideCheckboxAttribute : PropertyAttribute
    {
        public bool Right;
        public string Label;
        public WideCheckboxAttribute(bool right = false) { Right = right; }
    }

    /// <example>
    /// [TagSet(nameof(GetPossibleTags))]
    /// public string TagSet;
    ///    
    /// private IEnumerable<string> GetPossibleTags() => new List<string> { this.name, "Wall", "Window", "Door" };
    /// 
    /// </example>
    /// <!-- method GetPossibleTags can be in target object OR in parent monobehaviour  -->
    [AttributeUsage(AttributeTargets.Field, Inherited = true, AllowMultiple = false)]
    public class TagSetAttribute : PropertyAttribute
    {
        public string MethodName { get; }

        public TagSetAttribute(string stringSourceMethodName)
        {
            MethodName = stringSourceMethodName;
        }
    }

    /// <example>
    /// [ButtonList("OnButtonClicked")]
    /// public string buttonLabels = "Start, Stop, Reset";
    /// 
    /// private void OnButtonClicked(int index)
    /// {
    ///     Debug.Log($"Button {index} clicked!");
    /// }
    /// </example>
    [AttributeUsage(AttributeTargets.Field, AllowMultiple = false)]
    public class ButtonListAttribute : PropertyAttribute
    {
        public string MethodName { get; }
        public float ButtonWidth { get; }
        public string Title { get; }
        public ArgumentTypeEnum ArgumentType { get; }

        public ButtonListAttribute(string methodName, float buttonWidth = 80f, string title = null, ArgumentTypeEnum argumentType = ArgumentTypeEnum.Index)
        {
            MethodName = methodName;
            ButtonWidth = buttonWidth;
            ArgumentType = argumentType;
            Title = title;
        }

        public enum ArgumentTypeEnum
        {
            None = 0, Index, String
        }
    }

    /// <example>
    /// [ExpandedList("values", AddButtonText = "Add", DrawBox = true)]
    /// public class SomeClass : MonoBehaviour
    /// {
    ///     //[SerializeReference]
    ///     public List<SomeValue> values; // Custom rendering in Inspector
    /// }
    /// </example>
    [AttributeUsage(AttributeTargets.Class | AttributeTargets.Field, Inherited = true)]
    public class ExpandedListAttribute : Attribute
    {
        public string FieldName { get; }
        public bool DrawLabel = true;
        public bool DrawBox;
        public bool DrawFoldout;
        public string AddButtonText = "";
        public bool DrawDeleteButton = true;
        public int InnserSpace = 5;
        public bool DrawBeforeOtherFields = false;
        public int IndentLevel = 1;

        public ExpandedListAttribute(string fieldName)
        {
            FieldName = fieldName;
        }
    }

    /// <example>
    /// [FieldButtons("{1}: <color=white>{0:Up Vote <color=#00FF00FF> +0</color>;Down Vote<color=red> -0</color>;Default Vote}</color>", "#404040A0", Min = -5, Max = 5)]
    /// </example>
    public class FieldButtonsAttribute : PropertyAttribute
    {
        public string Format { get; }
        public float Step { get; set; } = 1f;
        public bool FullRow { get; set; }
        public Color BackgroundColor { get; } = default;
        public float Min { get; set; }
        public float Max { get; set; }

        public FieldButtonsAttribute(string format = "")
        {
            Format = format;
        }

        public FieldButtonsAttribute(string format, string bgColor) //for example "#FF0000"
        {
            Format = format;

            if (ColorUtility.TryParseHtmlString(bgColor, out var newCol))
                BackgroundColor = newCol;
        }
    }

    /// <example>
    ///    [Popup(nameof(PossibleStyles), IsEditable = true)]
    ///    public string DefaultStyle = "Default";
    ///    
    ///    IEnumerable<string> PossibleStyles => AssemblerDatabase.Instance.Styles;
    /// </example>
    public class PopupAttribute : PropertyAttribute
    {
        public string listSourcePropertyName;
        public bool IsEditable;

        public PopupAttribute(string listSourcePropertyName, bool isEditable = false)
        {
            this.listSourcePropertyName = listSourcePropertyName;
            IsEditable = isEditable;
        }
    }

    [AttributeUsage(AttributeTargets.Field, AllowMultiple = false, Inherited = true)]
    public class LabelAttribute : PropertyAttribute
    {
        public string Label;

        public LabelAttribute(string label)
        {
            this.Label = label;
        }
    }

    [AttributeUsage(AttributeTargets.Field, AllowMultiple = true, Inherited = true)]
    public class CommentAttribute : PropertyAttribute
    {
        public readonly string text;

        public CommentAttribute(string text)
        {
            this.text = text;
        }
    }

    [AttributeUsage(AttributeTargets.Property | AttributeTargets.Field)]
    public class HighlightAttribute : ShowIfAttribute
    {
        public Color Color = Color.red;

        public HighlightAttribute(float r, float g, float b) : base(null)
        {
            Color = new Color(r, g, b);
        }

        public HighlightAttribute(float r, float g, float b, string condPropertyName, params object[] condValues) : base(condPropertyName, condValues)
        {
            Color = new Color(r, g, b);
        }

        public HighlightAttribute(float r, float g, float b, string condPropertyName) : base(condPropertyName, true)
        {
            Color = new Color(r, g, b);
        }
    }

    [AttributeUsage(AttributeTargets.Property | AttributeTargets.Field)]
    public class PreviewIconAttribute : PropertyAttribute
    {
        public float Height = 0;
        public bool Transparent = true;
        public string InfoPropertyName;

        public PreviewIconAttribute(float height = 100)
        {
            this.Height = height;
        }
    }

    [AttributeUsage(AttributeTargets.Property | AttributeTargets.Field)]
    public class InfoAttribute : PropertyAttribute
    {
        public float MinHeight = 0;
        public enum MessageType { NONE, INFO, WARNING, ERROR }
        public MessageType messageType = MessageType.INFO;
    }

    [AttributeUsage(AttributeTargets.Property | AttributeTargets.Field)]
    public class ReadOnlyAttribute : ShowIfAttribute
    {
        public ReadOnlyAttribute() : base(null)
        {
        }

        public ReadOnlyAttribute(string condPropertyName, params object[] condValues) : base(condPropertyName, condValues)
        {
        }

        public ReadOnlyAttribute(string condPropertyName) : base(condPropertyName, true)
        {
        }
    }

    [AttributeUsage(AttributeTargets.Property | AttributeTargets.Field, AllowMultiple = true)]
    public class HideIfAttribute : ShowIfAttribute
    {
        public HideIfAttribute(string condPropertyName, params object[] condValues) : base(condPropertyName, condValues)
        {
        }

        public HideIfAttribute(string condPropertyName) : base(condPropertyName, true)
        {
        }
    }

    [AttributeUsage(AttributeTargets.Property | AttributeTargets.Field, AllowMultiple = true)]
    public class ShowIfAttribute : PropertyAttribute
    {
        public string CondPropertyName;// property, field or method name
        public System.Object[] CondValues;
        public DrawIfOp Op = DrawIfOp.AnyTrue;

        public ShowIfAttribute(string condPropertyName, params object[] condValues)
        {
            this.CondPropertyName = condPropertyName;
            this.CondValues = condValues;
        }

        public ShowIfAttribute(string condPropertyName) : this(condPropertyName, true)
        {
        }
    }

    public enum DrawIfOp
    {
        AnyTrue, AllTrue, AnyFalse, AllFalse
    }

    //[AttributeUsage(AttributeTargets.Field, AllowMultiple = false, Inherited = true)]
    //public class ButtonFieldAttribute : PropertyAttribute
    //{
    //    public string Method;
    //    public string[] Texts { get; private set; }
    //    public Color Color = Color.clear;

    //    public ButtonFieldAttribute()
    //    {
    //    }

    //    public ButtonFieldAttribute(string method, params string[] texts)
    //    {
    //        this.Method = method;
    //        this.Texts = texts;
    //        if (Texts.Length == 0)
    //            Texts = new string[] { Method };
    //    }
    //}

    [AttributeUsage(AttributeTargets.Property | AttributeTargets.Field)]
    public class FlagsAsCheckboxesAttribute : PropertyAttribute
    {
        public float CheckboxWidth = 150;
    }

    [AttributeUsage(AttributeTargets.Field, AllowMultiple = false, Inherited = true)]
    public class LayerAttribute : PropertyAttribute
    {
    }

    /// <example>
    /// [SerializeField] InspectorButton _AddFloor_AddFloor_RemoveFloor;
    /// 
    /// void AddFloor(int i)
    /// {
    ///    switch (i)
    ///    {
    ///        case 0: FloorCount++; break;
    ///        case 1: FloorCount--; break;
    ///    }
    /// }
    /// </example>
    /// 
    /// <example>
    /// [SerializeField] InspectorButton _AddFloor;
    /// 
    /// void AddFloor() => FloorCount++;
    /// </example>
    [Serializable]
    public enum InspectorButton : byte
    {
    }

#if UNITY_EDITOR

    [CustomPropertyDrawer(typeof(WideCheckboxAttribute))]
    public class WideCheckboxDrawer : PropertyDrawer
    {
        static GUIStyle styleOn;
        static GUIStyle styleOff;

        static WideCheckboxDrawer()
        {
            styleOn = new GUIStyle(EditorStyles.label)
            {
                //normal = { textColor = Color.white },
            };

            styleOff = new GUIStyle(EditorStyles.label)
            {
                //normal = { textColor = Color.gray },
            };
        }

        public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
        {
            if (property.propertyType != SerializedPropertyType.Boolean)
            {
                EditorGUI.LabelField(position, label.text, "Use WideCheckboxDrawer with bool fields only");
                return;
            }

            WideCheckboxAttribute attr = attribute as WideCheckboxAttribute;

            var checkboxWidth = attr.Right ? 50 : 20;
            Rect checkboxRect = new Rect(position.x, position.y, checkboxWidth, position.height);
            Rect labelRect = new Rect(position.x + checkboxWidth, position.y, position.width - checkboxWidth, position.height);
            if (!string.IsNullOrWhiteSpace(attr.Label))
                label.text = attr.Label;

            if (attr.Right)
            {
                labelRect = new Rect(position.x, position.y, position.width - checkboxWidth, position.height);
                checkboxRect = new Rect(position.x + position.width - checkboxWidth, position.y, checkboxWidth, position.height);
            }

            property.boolValue = EditorGUI.Toggle(checkboxRect, GUIContent.none, property.boolValue);
            EditorGUI.LabelField(labelRect, label, property.boolValue ? styleOn : styleOff);
        }

        public override float GetPropertyHeight(SerializedProperty property, GUIContent label)
        {
            return EditorGUI.GetPropertyHeight(property, label);
        }
    }

    [CustomPropertyDrawer(typeof(TagSetAttribute))]
    public class TagSetDrawer : PropertyDrawer
    {
        static string lastFoldoutObject;
        private IEnumerable<string> possibleTags = Array.Empty<string>();
        private HashSet<string> selectedTags = new HashSet<string>();

        private void UpdateTagLists(SerializedProperty property)
        {
            var tagSetAttribute = (TagSetAttribute)attribute;

            var methodInfo = property.serializedObject.GetType().GetMethod(tagSetAttribute.MethodName, BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
            if (methodInfo != null)
            {
                object target = property.serializedObject.targetObject;
                possibleTags = methodInfo.Invoke(target, null) as IEnumerable<string>;
            }
            else
            {
                methodInfo = fieldInfo.DeclaringType.GetMethod(tagSetAttribute.MethodName, BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
                if (methodInfo != null)
                {
                    object target = MemberInfoCache.GetObject(property);
                    possibleTags = methodInfo.Invoke(target, null) as IEnumerable<string>;
                }
            }

            selectedTags.Clear();
            selectedTags.AddRange(property.stringValue.Split(',').Select(t => t.Trim()).Where(t => !string.IsNullOrEmpty(t)));
        }

        public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
        {
            EditorGUI.BeginProperty(position, label, property);
            UpdateTagLists(property);

            Rect textFieldRect = new Rect(position.x + EditorGUIUtility.labelWidth, position.y, position.width - EditorGUIUtility.labelWidth - 23, EditorGUIUtility.singleLineHeight);
            Rect buttonRect = new Rect(position.x + EditorGUIUtility.labelWidth + position.width - EditorGUIUtility.labelWidth - 20, position.y, 20, EditorGUIUtility.singleLineHeight);

            Rect labelRect = new Rect(position.x, position.y, EditorGUIUtility.labelWidth, EditorGUIUtility.singleLineHeight);
            EditorGUI.PrefixLabel(labelRect, label);

            var foldout = lastFoldoutObject == property.propertyPath;

            if (Event.current.type == EventType.MouseDown && labelRect.Contains(Event.current.mousePosition))
            {
                OnFoldOutClick();
                Event.current.Use();
            }

            var oldIndent = EditorGUI.indentLevel;
            EditorGUI.indentLevel = 0;
            string newTags = EditorGUI.DelayedTextField(textFieldRect, property.stringValue);
            if (newTags != property.stringValue)
            {
                property.stringValue = newTags;
            }
            EditorGUI.indentLevel = oldIndent;

            if (GUI.Button(buttonRect, foldout ? "▾" : "▸", EditorStyles.miniButton))
                OnFoldOutClick();

            if (foldout)
            {
                EditorGUI.indentLevel++;
                Rect toggleRect = new Rect(position.x, position.y + EditorGUIUtility.singleLineHeight, position.width, EditorGUIUtility.singleLineHeight);
                var prevColor = GUI.color;

                foreach (string tag in possibleTags.Union(selectedTags).ToArray())
                {
                    bool isSelected = selectedTags.Contains(tag);
                    bool wasSelected = isSelected;

                    //EditorGUI.BeginDisabledGroup();
                    GUI.color = !possibleTags.Contains(tag) ? Color.yellow : isSelected ? Color.white * 2 : Color.white * 0.8f;

                    isSelected = EditorGUI.Toggle(toggleRect, tag, isSelected, EditorStyles.toggle);

                    //EditorGUI.EndDisabledGroup();

                    if (isSelected != wasSelected)
                    {
                        if (isSelected)
                            selectedTags.Add(tag);
                        else
                            selectedTags.Remove(tag);

                        property.stringValue = string.Join(", ", selectedTags);
                    }

                    // Смещаем rect вниз для следующего чекбокса
                    toggleRect.y += EditorGUIUtility.singleLineHeight;
                }
                EditorGUI.indentLevel--;
                GUI.color = prevColor;
            }

            EditorGUI.EndProperty();

            void OnFoldOutClick()
            {
                foldout = !foldout;

                if (foldout)
                    lastFoldoutObject = property.propertyPath;
                else
                    lastFoldoutObject = null;
            }
        }

        public override float GetPropertyHeight(SerializedProperty property, GUIContent label)
        {
            UpdateTagLists(property);
            var foldout = lastFoldoutObject == property.propertyPath;
            int extraLines = foldout ? possibleTags.Union(selectedTags).Count() : 0;
            return EditorGUIUtility.singleLineHeight * (1 + extraLines);
        }
    }

    public class TextInputWindow : EditorWindow
    {
        private string inputText = "";
        private string label = "";
        private Action<string> onConfirm;
        private bool firstFocus = true;

        public static void ShowWindow(string label, string defaultText, Action<string> onConfirm)
        {
            var window = GetWindow<TextInputWindow>("Enter", true);
            window.minSize = new Vector2(200, 70);
            window.maxSize = new Vector2(200, 70);
            window.onConfirm = onConfirm;
            window.label = label;
            window.inputText = defaultText;
            window.ShowUtility(); // Окно поверх других
        }

        private void OnGUI()
        {
            GUILayout.Label(label, EditorStyles.boldLabel);

            GUI.SetNextControlName("TextField");
            inputText = EditorGUILayout.TextField(inputText);

            if (firstFocus)
            {
                EditorGUI.FocusTextInControl("TextField");
                firstFocus = false;
            }

            GUILayout.Space(10);

            GUILayout.BeginHorizontal();

            if (GUILayout.Button("OK"))
            {
                onConfirm?.Invoke(inputText);
                SceneView.RepaintAll();
                Close();
            }

            if (GUILayout.Button("Cancel"))
            {
                Close();
            }

            GUILayout.EndHorizontal();
        }
    }

    [CustomPropertyDrawer(typeof(ButtonListAttribute))]
    public class ButtonListDrawer : PropertyDrawer
    {
        private const float ButtonHeight = 18f, ButtonSpacing = 3f, RightPadding = 20;

        public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
        {
            if (property.propertyType != SerializedPropertyType.String) return;
            if (property.stringValue.IsNullOrEmpty()) return;

            var attr = (ButtonListAttribute)attribute;
            object target = property.serializedObject.targetObject;
            MethodInfo method = target.GetType().GetMethod(attr.MethodName,
                BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
            if (method == null) return;

            if (!string.IsNullOrEmpty(attr.Title))
            {
                var titleRect = new Rect(position.x, position.y, position.width, EditorGUIUtility.singleLineHeight);
                GUI.Label(titleRect, new GUIContent(attr.Title), EditorStyles.boldLabel);
                position.y += titleRect.height;
            }

            string[] buttons = property.stringValue.Split(',');
            float inspectorWidth = EditorGUIUtility.currentViewWidth - RightPadding;
            int buttonsPerRow = Mathf.Max(1, Mathf.FloorToInt(inspectorWidth / (attr.ButtonWidth + ButtonSpacing)));
            Rect buttonRect = new(position.x, position.y, attr.ButtonWidth, ButtonHeight);

            for (int i = 0; i < buttons.Length; i++)
            {
                var name = buttons[i].Trim();
                if (GUI.Button(buttonRect, name))
                {
                    object[] args = null;
                    switch (attr.ArgumentType)
                    {
                        case ButtonListAttribute.ArgumentTypeEnum.Index: args = new object[] { i }; break;
                        case ButtonListAttribute.ArgumentTypeEnum.String: args = new object[] { name }; break;
                    }
                    
                    method.Invoke(target, args);
                }

                buttonRect.x += attr.ButtonWidth + ButtonSpacing;
                if ((i + 1) % buttonsPerRow == 0) 
                    buttonRect = new(position.x, buttonRect.y + ButtonHeight + ButtonSpacing, attr.ButtonWidth, ButtonHeight);
            }
        }

        public override float GetPropertyHeight(SerializedProperty property, GUIContent label)
        {
            if (property.propertyType != SerializedPropertyType.String) return EditorGUIUtility.singleLineHeight;
            if (property.stringValue.IsNullOrEmpty()) return 0;

            var attr = (ButtonListAttribute)attribute;
            int buttonCount = property.stringValue.Split(',').Length;
            int buttonsPerRow = Mathf.Max(1, Mathf.FloorToInt((EditorGUIUtility.currentViewWidth - RightPadding) / (attr.ButtonWidth + ButtonSpacing)));
            var titleHeight = string.IsNullOrEmpty(attr.Title) ? 0 : EditorGUIUtility.singleLineHeight;
            return Mathf.CeilToInt(buttonCount / (float)buttonsPerRow) * (ButtonHeight + ButtonSpacing) + titleHeight;
        }
    }


    //[CustomEditor(typeof(SomeClass))]
    //[CustomEditor(typeof(MonoBehaviour), true)]
    public class ExpandedListEditor : Editor
    {
        ExpandedListAttribute attr;
        GUIStyle buttonStyle;
        GUIStyle ButtonStyle => buttonStyle ??= new GUIStyle(EditorStyles.miniButton) { normal = { textColor = Color.red } };

        private void OnEnable()
        {
        }

        public override void OnInspectorGUI() 
        {
            // Check if the class has the ExpandedListAttribute
            var attr = target.GetType().GetCustomAttribute<ExpandedListAttribute>();

            // Check if the ExpandedListAttribute
            if (attr != null)
                // Custom GUI for ExpandedListAttribute objects
                DrawCustomInspector(attr);
            else
                // Default inspector for non-ExpandedListAttribute objects
                DrawDefaultInspector();
        }

        public void DrawCustomInspector(ExpandedListAttribute attr)
        {
            // Get the field name from the attribute
            var fieldName = attr.FieldName;
            var valuesProperty = serializedObject.FindProperty(fieldName);

            if (valuesProperty == null)
            {
                DrawDefaultInspector();
                return;
            }

            // Multiselect?
            if (serializedObject.isEditingMultipleObjects)
            {
                EditorGUILayout.HelpBox("Multi-editing is not supported for this property.", MessageType.Warning);
                return;
            }

            this.attr = attr;

            serializedObject.Update();

            if (!attr.DrawBeforeOtherFields)
                DrawPropertiesExcluding(serializedObject, fieldName); // Draw everything except the array

            //EditorGUILayout.LabelField(valuesProperty.displayName, EditorStyles.boldLabel);

            int deleteIndex = -1; // To delete an element
            EditorGUI.indentLevel = attr.IndentLevel;

            // Indentation between elements
            if (attr.InnserSpace > 0)
                GUILayout.Space(attr.InnserSpace);

            for (int i = 0; i < valuesProperty.arraySize; i++)
            {
                SerializedProperty element = valuesProperty.GetArrayElementAtIndex(i);
                var helpAttr = element.boxedValue?.GetType().GetCustomAttribute<HelpURLAttribute>();

                var elementRect = attr.DrawFoldout ? DrawElementFoldout(element) : DrawElementNoFoldout(element);

                if (attr.DrawDeleteButton)
                    DrawDeleteButton(ref deleteIndex, i, elementRect);

                if (helpAttr != null)
                    DrawHelpButton(helpAttr, elementRect);

                // Indentation between elements
                if (attr.InnserSpace > 0)
                    GUILayout.Space(attr.InnserSpace);
            }

            // Remove the element after the loop is complete
            if (deleteIndex >= 0)
                valuesProperty.DeleteArrayElementAtIndex(deleteIndex);

            if (!string.IsNullOrWhiteSpace(attr.AddButtonText))
                if (GUILayout.Button(attr.AddButtonText))
                {
                    valuesProperty.arraySize++;
                    serializedObject.ApplyModifiedProperties();
                }

            // 
            EditorGUI.indentLevel = 0;

            if (attr.DrawBeforeOtherFields)
                DrawPropertiesExcluding(serializedObject, fieldName); // Draw everything except the array

            serializedObject.ApplyModifiedProperties();
        }

        private static Rect DrawElementFoldout(SerializedProperty element)
        {
            // Get the standard name, like in Unity
            GUIContent elementLabel = new GUIContent(MemberInfoCache.ToReadableFormat(element.displayName));

            // Draw the element in the inspector with foldout
            Rect elementRect = EditorGUILayout.BeginVertical();
            EditorGUILayout.PropertyField(element, elementLabel, true);
            EditorGUILayout.EndVertical();
            return elementRect;
        }

        private Rect DrawElementNoFoldout(SerializedProperty element)
        {
            Rect headerRect = EditorGUILayout.BeginHorizontal();

            if (attr.DrawBox)
                headerRect = EditorGUILayout.BeginVertical(EditorStyles.helpBox);

            if (attr.DrawLabel)
            {
                EditorGUILayout.LabelField(new GUIContent(MemberInfoCache.ToReadableFormat(element.displayName)), EditorStyles.boldLabel);
                headerRect = GUILayoutUtility.GetLastRect();
            }

            SerializedProperty field = element.Copy();
            SerializedProperty end = element.GetEndProperty();

            field.NextVisible(true);

            while (!SerializedProperty.EqualContents(field, end))
            {
                EditorGUILayout.PropertyField(field, true);
                field.NextVisible(false);
            }

            if (attr.DrawBox)
                EditorGUILayout.EndVertical();

            EditorGUILayout.EndHorizontal();
            return headerRect;
        }

        private int DrawDeleteButton(ref int deleteIndex, int i, Rect elementRect)
        {
            // Area for the delete button
            Rect headerRect = new Rect(elementRect.xMax - 23, elementRect.y, 22, 20);

            if (GUI.Button(headerRect, "✖", ButtonStyle))
                deleteIndex = i;
            return deleteIndex;
        }

        private void DrawHelpButton(HelpURLAttribute help, Rect elementRect)
        {
            // Area for the delete button
            Rect headerRect = new Rect(elementRect.xMax - 23 * 2, elementRect.y, 22, 20);

            if (GUI.Button(headerRect, "?", EditorStyles.miniButton))
                Application.OpenURL(help.URL);
                
        }
    }

    [CustomPropertyDrawer(typeof(FieldButtonsAttribute))]
    public class FieldButtonsDrawer : PropertyDrawer
    {
        static GUIStyle richTextStyle;
        static Dictionary<Color, Texture2D> colorToTexture = new Dictionary<Color, Texture2D>();

        public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
        {
            // Multiselect?
            if (property.serializedObject.isEditingMultipleObjects)
            {
                EditorGUI.HelpBox(position, "Multi-editing is not supported for this property.", MessageType.Warning);
                return;
            }

            if (property.propertyType != SerializedPropertyType.Float && property.propertyType != SerializedPropertyType.Integer && property.propertyType != SerializedPropertyType.Enum)
            {
                EditorGUI.LabelField(position, label.text, "Use FieldButtons with float, int or enum only");
                return;
            }

            FieldButtonsAttribute attr = (FieldButtonsAttribute)attribute;

            EditorGUI.BeginProperty(position, label, property);

            float buttonWidth = 22f;
            float spacing = 1f;
            var isReadOnly = !string.IsNullOrEmpty(attr.Format);

            // Divide area
            Rect labelRect = new Rect(position.x, position.y, EditorGUIUtility.labelWidth, position.height);
            Rect valueRect = new Rect(labelRect.xMax, position.y, position.width - EditorGUIUtility.labelWidth - (buttonWidth * 2) - (spacing * 2), position.height);
            Rect leftButtonRect = new Rect(valueRect.xMax + spacing, position.y, buttonWidth, position.height);
            Rect rightButtonRect = new Rect(leftButtonRect.xMax + spacing, position.y, buttonWidth, position.height);
            if (attr.FullRow)
                valueRect = new Rect(position.x, position.y, position.width - (buttonWidth * 2) - (spacing * 2), position.height);

            // Style
            if (richTextStyle == null)
                richTextStyle = new GUIStyle(GUI.skin.label) { richText = true };
            if (attr.BackgroundColor != default)
            {
                if (!colorToTexture.TryGetValue(attr.BackgroundColor, out var tex))
                    tex = colorToTexture[attr.BackgroundColor] = MakeTex(2, 2, attr.BackgroundColor);
                richTextStyle.normal.background = tex;
            }

            // Use rich text in the label
            if (!attr.FullRow)
                EditorGUI.LabelField(labelRect, label);

            var oldIndent = EditorGUI.indentLevel;
            if (!attr.FullRow)
                EditorGUI.indentLevel = 0;

            // Если поле не ReadOnly, можно редактировать значение
            if (!isReadOnly)
            {
                if (property.propertyType == SerializedPropertyType.Integer)
                    property.intValue = EditorGUI.IntField(valueRect, property.intValue);
                if (property.propertyType == SerializedPropertyType.Float)
                    property.floatValue = EditorGUI.FloatField(valueRect, property.floatValue);
                if (property.propertyType == SerializedPropertyType.Enum)
                    property.enumValueIndex = EditorGUI.Popup(valueRect, property.enumValueIndex, property.enumDisplayNames);
            }
            else
            {
                // Save current GUI background color
                Color previousColor = GUI.backgroundColor;
                if (attr.BackgroundColor != default)
                    GUI.backgroundColor = attr.BackgroundColor;

                if (property.propertyType == SerializedPropertyType.Float)
                    EditorGUI.LabelField(valueRect, string.Format(attr.Format, property.floatValue, property.displayName), richTextStyle);

                if (property.propertyType == SerializedPropertyType.Integer)
                    EditorGUI.LabelField(valueRect, string.Format(attr.Format, property.intValue, property.displayName), richTextStyle);

                if (property.propertyType == SerializedPropertyType.Enum)
                {
                    var labels = property.enumDisplayNames;
                    var index = property.enumValueIndex;
                    if ( index >=0 && index < labels.Length)
                        EditorGUI.LabelField(valueRect, string.Format(attr.Format, labels[property.enumValueIndex], property.displayName), richTextStyle);
                }

                GUI.backgroundColor = previousColor;
            }

            var delta = 0f;
            if (GUI.Button(leftButtonRect, "▲")) delta = +attr.Step;
            if (GUI.Button(rightButtonRect, "▼")) delta = -attr.Step;

            //if (GUI.Button(rightButtonRect, "▲")) delta = +attr.Step;
            //if (GUI.Button(leftButtonRect, "▼")) delta = -attr.Step;

            if (property.propertyType == SerializedPropertyType.Float)
                property.floatValue += delta;

            if (property.propertyType == SerializedPropertyType.Integer)
                property.intValue += Mathf.RoundToInt(delta);

            if (property.propertyType == SerializedPropertyType.Enum)
            {
                var min = attr.Min != attr.Max ? (int)attr.Min : 0;
                var max = attr.Min != attr.Max ? (int)attr.Max : property.enumDisplayNames.Length - 1;
                property.enumValueIndex = Mathf.Min(max, Mathf.Max(min, property.enumValueIndex + Mathf.RoundToInt(delta)));
            }

            if (attr.Min != attr.Max)
            {
                if (property.propertyType == SerializedPropertyType.Float)
                {
                    if (property.floatValue < attr.Min) property.floatValue = attr.Min;
                    if (property.floatValue > attr.Max) property.floatValue = attr.Max;
                }

                if (property.propertyType == SerializedPropertyType.Integer)
                {
                    if (property.intValue < attr.Min) property.intValue = Mathf.RoundToInt(attr.Min);
                    if (property.intValue > attr.Max) property.intValue = Mathf.RoundToInt(attr.Max);
                }
            }

            EditorGUI.indentLevel = oldIndent;

            EditorGUI.EndProperty();
        }

        // Function to create a colored texture
        private Texture2D MakeTex(int width, int height, Color col)
        {
            Color[] pix = new Color[width * height];
            for (int i = 0; i < pix.Length; i++) pix[i] = col;
            Texture2D texture = new Texture2D(width, height);
            texture.SetPixels(pix);
            texture.Apply();
            return texture;
        }
    }


    [CustomPropertyDrawer(typeof(PopupAttribute))]
    public class PopupDrawer : PropertyDrawer
    {
        public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
        {
            var attr = attribute as PopupAttribute;
            var list = MemberInfoCache.GetPropertyValue(fieldInfo.DeclaringType, property, attr.listSourcePropertyName) as IEnumerable<string>;

            if (!attr.IsEditable)
            {
                FixedPopup(position, property, label, list);
                return;
            }

            const int W = 20;

            var rect = new Rect(position.x, position.y, position.width - W - 4, position.height);
            EditorGUI.PropertyField(rect, property, label);

            if (list != null && list.Any())
            {
                var array = Enumerable.Range(0, 1).Select(_ => "<Custom>").Union(list).ToArray();
                var selectedIndex = Mathf.Max(Array.IndexOf(array, property.stringValue), 0);

                var oldIndent = EditorGUI.indentLevel;
                EditorGUI.indentLevel = 0;
                var oldWidth = EditorStyles.popup.fixedWidth;
                EditorStyles.popup.fixedWidth = W;
                rect = new Rect(position.x + position.width - W, position.y, W, position.height);
                selectedIndex = EditorGUI.Popup(rect, "", selectedIndex, array);
                EditorStyles.popup.fixedWidth = oldWidth;
                EditorGUI.indentLevel = oldIndent;
                if (selectedIndex > 0)
                    property.stringValue = array[selectedIndex];
            }
        }

        private static void FixedPopup(Rect position, SerializedProperty property, GUIContent label, IEnumerable<string> list)
        {
            if (list != null && list.Any())
            {
                var array = list.ToArray();
                var selectedIndex = Mathf.Max(Array.IndexOf(array, property.stringValue), 0);
                selectedIndex = EditorGUI.Popup(position, property.name, selectedIndex, array);
                property.stringValue = array[selectedIndex];
            }
            else
            {
                EditorGUI.PropertyField(position, property, label);
            }
        }
    }

    [CustomPropertyDrawer(typeof(CommentAttribute))]
    public class CommentDrawer : DecoratorDrawer
    {
        private const float HEIGHT_PADDING = 4f;

        private CommentAttribute Target => attribute as CommentAttribute;

        public override void OnGUI(Rect _rect)
        {
            float indent = GetIndentLength(_rect);

            _rect.Set(
                _rect.x + indent, _rect.y,
                _rect.width - indent, GetBoxHeight() - HEIGHT_PADDING * 0.5f);

            EditorGUI.HelpBox(_rect, Target.text, MessageType.None);
        }

        public static float GetIndentLength(Rect _sourceRect)
        {
            Rect indentRect = EditorGUI.IndentedRect(_sourceRect);
            float indentLength = indentRect.x - _sourceRect.x;

            return indentLength;
        }

        //How tall the GUI is for this decorator
        public override float GetHeight()
        {
            return GetBoxHeight() + HEIGHT_PADDING;
        }

        private float GetBoxHeight()
        {
            float width = EditorGUIUtility.currentViewWidth;
            float minHeight = EditorGUIUtility.singleLineHeight * 2f;

            // Icon, Scrollbar, Indent
            width -= 68;

            //Need a little extra for correct sizing of InfoBox
            float actualHeight = EditorStyles.helpBox.CalcHeight(new GUIContent(Target.text), width);
            return Mathf.Max(minHeight, actualHeight);
        }
    }

    [CanEditMultipleObjects]
    [CustomEditor(typeof(UnityEngine.Object), true, isFallback = true)]
    public class Inspector1Editor : UnityEditor.Editor
    {
        public override void OnInspectorGUI()
        {
            this.DrawDefaultInspector();
        }
    }

    [CustomPropertyDrawer(typeof(LayerAttribute))]
    public class LayerPropertyDrawer : PropertyDrawer
    {
        public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
        {
            if (property.propertyType == SerializedPropertyType.Integer)
            {
                // generate the taglist + custom tags
                List<string> tagList = new List<string>();
                tagList.AddRange(UnityEditorInternal.InternalEditorUtility.layers);

                int index = tagList.IndexOf(LayerMask.LayerToName(property.intValue));

                // Draw the popup box with the current selected index
                index = EditorGUI.Popup(position, property.displayName, index, tagList.ToArray());
                //index = EditorGUILayout.Popup(property.displayName, index, tagList.ToArray());

                // Adjust the actual string value of the property based on the selection
                property.intValue = LayerMask.NameToLayer(tagList[index]);
            }
        }
    }

    [CustomPropertyDrawer(typeof(LabelAttribute))]
    public class LabelDrawer : PropertyDrawer
    {
        public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
        {
            var labelAttribute = attribute as LabelAttribute;
            var guiContent = new GUIContent(labelAttribute.Label);
            EditorGUI.PropertyField(position, property, guiContent, true);
        }
    }

    [CustomPropertyDrawer(typeof(InfoAttribute))]
    public class InfoDrawer : PropertyDrawer
    {
        private const float HEIGHT_PADDING = 4f;

        public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
        {
            var attr = (attribute as InfoAttribute);
            var val = property.stringValue;
            if (string.IsNullOrWhiteSpace(val))
                return;

            float indent = GetIndentLength(position);

            position.Set(
                position.x + indent, position.y,
                position.width - indent, GetBoxHeight(property) - HEIGHT_PADDING * 0.5f);

            var type = GetMessageType(val);

            EditorGUI.HelpBox(position, val, (UnityEditor.MessageType)(int)type);
        }

        private static InfoAttribute.MessageType GetMessageType(string val)
        {
            var res = InfoAttribute.MessageType.INFO;
            if (val.Contains("!"))
            {
                res = InfoAttribute.MessageType.WARNING;
                if (val.Contains("!!"))
                    res = InfoAttribute.MessageType.ERROR;
            }

            return res;
        }

        public override float GetPropertyHeight(SerializedProperty property, GUIContent label)
        {
            return GetBoxHeight(property);
        }

        public static float GetIndentLength(Rect _sourceRect)
        {
            Rect indentRect = EditorGUI.IndentedRect(_sourceRect);
            float indentLength = indentRect.x - _sourceRect.x;

            return indentLength;
        }

        private float GetBoxHeight(SerializedProperty property)
        {
            var attr = (attribute as InfoAttribute);
            var val = property.stringValue;
            if (string.IsNullOrWhiteSpace(val))
                return -EditorGUIUtility.standardVerticalSpacing;

            float width = EditorGUIUtility.currentViewWidth;
            float minHeight = Mathf.Max(attr.MinHeight, EditorGUIUtility.singleLineHeight * 2f);

            // Icon, Scrollbar, Indent
            width -= 68;

            //Need a little extra for correct sizing of InfoBox
            float actualHeight = EditorStyles.helpBox.CalcHeight(new GUIContent(val), width);
            return Mathf.Max(minHeight, actualHeight);
        }
    }

    [CustomPropertyDrawer(typeof(PreviewIconAttribute))]
    public class PreviewIconPropertyDrawer : PropertyDrawer
    {
        const float _space = 10;

        public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
        {
            var initRect = position;
            var attr = (attribute as PreviewIconAttribute);
            var _height = attr.Height;
            var _width = attr.Height;
            //EditorGUI.indentLevel = 0;

            if (property.objectReferenceValue == null)
            {
                EditorGUI.PropertyField(position, property, label, true);
                return;
            }

            Texture2D texture = AssetPreview.GetAssetPreview(property.objectReferenceValue);
            if (texture)
            {
                var imageAspect = texture.width / (float)texture.height;
                _width = texture.width;
                GUI.DrawTexture(new Rect(position.position, new Vector2(_width, _height)), texture, ScaleMode.ScaleToFit, attr.Transparent, imageAspect);
            }

            if (attr.InfoPropertyName.IsNotNullOrEmpty())
            {
                var val = MemberInfoCache.GetPropertyValue(fieldInfo.DeclaringType, property, attr.InfoPropertyName)?.ToString();
                if (val != null)
                {
                    position.x = initRect.x + _width;
                    position.height = _height;
                    position.width = initRect.width - _width;
                    GUI.Label(position, new GUIContent(val));
                }
            }

            position = initRect;
            position.y += _height + _space;
            position.height += -_height - _space;
            EditorGUI.PropertyField(position, property, label, true);
        }

        public override float GetPropertyHeight(SerializedProperty property, GUIContent label)
        {
            var has = property.objectReferenceValue;
            var _height = (attribute as PreviewIconAttribute).Height;
            return base.GetPropertyHeight(property, label) + (has ? (_height + _space) : 0);
        }
    }

    [CustomPropertyDrawer(typeof(ShowIfAttribute), true)]
    public class ShowIfDrawer : PropertyDrawer
    {
        protected float _height;
        bool isConditionOK;

        public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
        {
            if (!ShouldDraw())
                return;

            var attr = (attribute as ShowIfAttribute);
            var type = attribute.GetType();

            var prevColor = GUI.color;
            var prevEnabled = GUI.enabled;

            GUI.enabled &= isConditionOK || type != typeof(ReadOnlyAttribute);

            if (type == typeof(HighlightAttribute) && !isConditionOK)
                GUI.color = (attribute as HighlightAttribute).Color;

            EditorGUI.PropertyField(position, property, label, true);

            GUI.enabled = prevEnabled;
            GUI.color = prevColor;
        }

        public override float GetPropertyHeight(SerializedProperty property, GUIContent label)
        {
            var attr = (attribute as ShowIfAttribute);
            var type = attribute.GetType();

            isConditionOK = EvaluateCondition(property, attr);

            // Якщо потрібно малювати — повертаємо реальну висоту
            if (isConditionOK || type == typeof(ReadOnlyAttribute) || type == typeof(HighlightAttribute))
            {
                return EditorGUI.GetPropertyHeight(property, label, true);
            }

            // Якщо property — це об'єкт і розгорнутий, все одно треба повернути висоту
            if (property.propertyType == SerializedPropertyType.Generic && property.isExpanded)
            {
                return EditorGUI.GetPropertyHeight(property, label, true);
            }

            return 0;
        }

        private bool ShouldDraw()
        {
            var type = attribute.GetType();
            return isConditionOK || type == typeof(ReadOnlyAttribute) || type == typeof(HighlightAttribute);
        }

        private bool EvaluateCondition(SerializedProperty property, ShowIfAttribute attr)
        {
            if (string.IsNullOrWhiteSpace(attr.CondPropertyName))
                return false;

            var val = MemberInfoCache.GetPropertyValue(fieldInfo.DeclaringType, property, attr.CondPropertyName);
            switch (attr.Op)
            {
                case DrawIfOp.AnyTrue: return attr.CondValues.Any(v => Equals(val, v));
                case DrawIfOp.AllTrue: return attr.CondValues.All(v => Equals(val, v));
                case DrawIfOp.AnyFalse: return attr.CondValues.Any(v => !Equals(val, v));
                case DrawIfOp.AllFalse: return attr.CondValues.All(v => !Equals(val, v));
            }

            return false;
        }
    }

    [CustomPropertyDrawer(typeof(InspectorButton))]
    public class InspectorButtonDrawer : PropertyDrawer
    {
        public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
        {
            var parts = property.name.Trim('_').Split('_');
            var methodName = parts[0];
            var title = MemberInfoCache.ToReadableFormat(parts.Length > 1 ? parts[1] : methodName);
            var mi = MemberInfoCache.Get<MethodInfo>(fieldInfo.DeclaringType, methodName);

            if (mi != null)
            {
                if (parts.Length > 2)
                {
                    var w = position.width / (parts.Length - 1);
                    var x = position.x;
                    for (int i = 1; i < parts.Length; i++)
                    {
                        if (GUI.Button(new Rect(x, position.y, w, position.height), MemberInfoCache.ToReadableFormat(parts[i])))
                            ExecuteMethod(property, methodName, i - 1);
                        x += w;
                    }
                }
                else
                if (GUI.Button(position, title))
                    ExecuteMethod(property, methodName);
            }
            else
            {
                GUI.Label(position, "Unknown method: " + methodName);
            }
        }

        void ExecuteMethod(SerializedProperty property, string methodName, int parameter = -1)
        {
            var obj = MemberInfoCache.GetObject(property);
            var info = MemberInfoCache.Get<MemberInfo>(fieldInfo.DeclaringType, methodName);
            if (obj == null || info == null)
                return;

            switch (info)
            {
                case MethodInfo mInfo:
                    if (parameter >= 0)
                        mInfo.Invoke(obj, new object[] { parameter });
                    else
                        mInfo.Invoke(obj, null);
                    break;
            }
        }
    }

    //[CustomPropertyDrawer(typeof(ButtonFieldAttribute))]
    //public class ButtonFieldDrawer : PropertyDrawer
    //{
    //    public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
    //    {
    //        var but = attribute as ButtonFieldAttribute;
    //        var prevColor = GUI.color;

    //        if (but.Color != Color.clear)
    //            GUI.color = but.Color;

    //        if (but.Texts.Length == 1)
    //        {
    //            if (GUI.Button(position, but.Texts[0]))
    //                ExecuteMethod(property, but.Method);
    //        }else
    //        {
    //            var w = position.width / but.Texts.Length;
    //            var x = position.x;
    //            for (int i = 0; i < but.Texts.Length; i++)
    //            {
    //                if (GUI.Button(new Rect(x, position.y, w, position.height), but.Texts[i]))
    //                    ExecuteMethod(property, but.Method, i);
    //                x += w;
    //            }
    //        }

    //        GUI.color = prevColor;
    //    }

    //    void ExecuteMethod(SerializedProperty property, string methodName, int parameter = -1)
    //    {
    //        var obj = MemberInfoCache.GetObject(fieldInfo.DeclaringType, property);
    //        var info = MemberInfoCache.Get<MemberInfo>(fieldInfo.DeclaringType, methodName);
    //        if (obj == null || info == null)
    //            return;

    //        switch (info)
    //        {
    //            case MethodInfo mInfo:
    //                if (parameter >= 0)
    //                    mInfo.Invoke(obj, new object[] { parameter });
    //                else
    //                    mInfo.Invoke(obj, null);
    //                break;
    //        }
    //    }

    //    private static void ShowPropertyField(SerializedProperty property)
    //    {
    //        property.serializedObject.Update();

    //        EditorGUILayout.PropertyField(property);

    //        property.serializedObject.ApplyModifiedProperties();
    //    }
    //}

    [CustomPropertyDrawer(typeof(FlagsAsCheckboxesAttribute))]
    public class FlagsAsCheckboxesDrawer : PropertyDrawer
    {
        Array values;
        float rowHeight;
        int perRow;

        public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
        {
            var attr = (attribute as FlagsAsCheckboxesAttribute);

            var itemWidth = position.width / perRow;
            var init = position;
            var resValue = property.enumValueFlag;

            for (int i = 0; i < values.Length - 1; i++)
            {
                var iCol = i % perRow;
                var iRow = i / perRow;
                var val = values.GetValue(i + 1);
                var rect = new Rect(init.x + iCol * itemWidth, init.y + iRow * rowHeight, itemWidth, rowHeight);
                var name = property.enumDisplayNames[i + 1];
                var res = EditorGUI.ToggleLeft(rect, name, (resValue & (int)val) == (int)val);
                resValue = res ? resValue | (int)val : resValue & ~(int)val;
                //EditorGUI.PropertyField(new Rect(init.x + iCol * itemWidth, init.y + iRow * rowHeight, itemWidth, rowHeight), property, label, true);
            }

            if (property.enumValueFlag != resValue)
                property.enumValueFlag = resValue;
        }

        public override float GetPropertyHeight(SerializedProperty property, GUIContent label)
        {
            var attr = (attribute as FlagsAsCheckboxesAttribute);
            var obj = MemberInfoCache.GetPropertyValue(fieldInfo.ReflectedType, property, property.name);
            values = MemberInfoCache.GetEnumValues(obj.GetType());
            var valCount = values.Length - 1;
            perRow = (int)(EditorGUIUtility.currentViewWidth / attr.CheckboxWidth);
            if (perRow < 1) perRow = 1;
            var rows = valCount / perRow + (valCount % perRow == 0 ? 0 : 1);
            rowHeight = base.GetPropertyHeight(property, label);
            return rowHeight * rows;
        }
    }

    public static class MemberInfoCache
    {
        public static readonly Dictionary<(Type, string), MemberInfo> Cache = new Dictionary<(Type, string), MemberInfo>();
        public static readonly Dictionary<Type, Array> EnumValuesCache = new Dictionary<Type, Array>();

        public static Array GetEnumValues(Type enumType)
        {
            if (EnumValuesCache.TryGetValue(enumType, out var arr))
                return arr;

            return EnumValuesCache[enumType] = Enum.GetValues(enumType);
        }

        public static string ToReadableFormat(string fieldName)
        {
            string result = Regex.Replace(fieldName, "(\\B[A-Z])", " $1");
            return char.ToUpper(result[0]) + result.Substring(1);
        }

        public static T Get<T>(Type type, string name) where T : MemberInfo
        {
            if (Cache.TryGetValue((type, name), out var info))
                return info as T;

            var info2 = type.GetMember(name, BindingFlags.Public | BindingFlags.Instance | BindingFlags.NonPublic).FirstOrDefault();
            Cache[(type, name)] = info2;

            return info2 as T;
        }

        public static System.Object GetObject(SerializedProperty property)
        {
            //Look for the sourcefield within the object that the property belongs to
            string propertyPath = property.propertyPath; //returns the property path of the property we want to apply the attribute to
            string conditionPath = propertyPath.Replace(property.name, "").TrimEnd('.'); //changes the path to the conditionalsource property path
            SerializedProperty sourcePropertyValue = property.serializedObject.FindProperty(conditionPath);

            if (sourcePropertyValue == null)
                return property.serializedObject.targetObject;

#if UNITY_2022_1_OR_NEWER
            return sourcePropertyValue.boxedValue;
#else
            return GetTargetObjectOfProperty(property, 1);
#endif      
        }

        public static object GetTargetObjectOfProperty(SerializedProperty prop, int skipLast = 0)
        {
            if (prop == null) return null;

            var path = prop.propertyPath.Replace(".Array.data[", "[");
            object obj = prop.serializedObject.targetObject;
            var elements = path.Split('.');
            foreach (var element in elements.SkipLast(skipLast))
            {
                if (element.Contains("["))
                {
                    var elementName = element.Substring(0, element.IndexOf("["));
                    var index = System.Convert.ToInt32(element.Substring(element.IndexOf("[")).Replace("[", "").Replace("]", ""));
                    obj = GetValue_Imp(obj, elementName, index);
                }
                else
                {
                    obj = GetValue_Imp(obj, element);
                }
            }
            return obj;
        }

        private static object GetValue_Imp(object source, string name)
        {
            if (source == null)
                return null;
            var type = source.GetType();

            while (type != null)
            {
                var f = type.GetField(name, BindingFlags.NonPublic | BindingFlags.Public | BindingFlags.Instance);
                if (f != null)
                    return f.GetValue(source);

                var p = type.GetProperty(name, BindingFlags.NonPublic | BindingFlags.Public | BindingFlags.Instance | BindingFlags.IgnoreCase);
                if (p != null)
                    return p.GetValue(source, null);

                type = type.BaseType;
            }
            return null;
        }

        private static object GetValue_Imp(object source, string name, int index)
        {
            var enumerable = GetValue_Imp(source, name) as System.Collections.IEnumerable;
            if (enumerable == null) return null;
            var enm = enumerable.GetEnumerator();
            //while (index-- >= 0)
            //    enm.MoveNext();
            //return enm.Current;

            for (int i = 0; i <= index; i++)
            {
                if (!enm.MoveNext()) return null;
            }
            return enm.Current;
        }

        public static System.Object GetPropertyValue(Type type, SerializedProperty property, string fieldName)
        {
            var obj = GetObject(property);
            var info = Get<MemberInfo>(type, fieldName);
            if (obj == null || info == null)
                return null;

            switch (info)
            {
                case PropertyInfo propertyInfo: return propertyInfo.GetValue(obj, null);
                case FieldInfo fInfo: return fInfo.GetValue(obj);
                case MethodInfo mInfo: return mInfo.Invoke(obj, null);
            }

            return null;
        }
    }
#endif
}
