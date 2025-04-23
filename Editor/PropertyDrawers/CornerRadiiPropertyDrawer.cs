using Tactile.Core.Editor.Utility;
using Tactile.Core.Extensions;
using Tactile.Core.Utility;
using Tactile.UI.Utility;
using UnityEditor;
using UnityEngine;

namespace Tactile.UI.Editor.PropertyDrawers
{
    [CustomPropertyDrawer(typeof(CornerRadii))]
    public class CornerRadiiPropertyDrawer : PropertyDrawer
    {
        public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
        {
            var topLeft = property.FindPropertyRelative(nameof(CornerRadii.topLeft));
            var topRight = property.FindPropertyRelative(nameof(CornerRadii.topRight));
            var bottomLeft = property.FindPropertyRelative(nameof(CornerRadii.bottomLeft));
            var bottomRight = property.FindPropertyRelative(nameof(CornerRadii.bottomRight));

            position = EditorGUI.PrefixLabel(position, label);
            
            var rects = position.HorizontalLayout(RectLayout.SingleLineHeight,
                RectLayout.Repeat(4, RectLayout.Flex()));

            var topLeftName = $"{property.name}-topLeft";
            var topRightName = $"{property.name}-topRight";
            var bottomLeftName = $"{property.name}-bottomLeft";
            var bottomRightName = $"{property.name}-bottomRight";

            GUI.SetNextControlName(topLeftName);
            EditorGUI.PropertyField(rects[1], topLeft, GUIContent.none);
            
            GUI.SetNextControlName(topRightName);
            EditorGUI.PropertyField(rects[2], topRight, GUIContent.none);
            
            GUI.SetNextControlName(bottomLeftName);
            EditorGUI.PropertyField(rects[3], bottomLeft, GUIContent.none);
            
            GUI.SetNextControlName(bottomRightName);
            EditorGUI.PropertyField(rects[4], bottomRight, GUIContent.none);
            
            
            var currentSelected = GUI.GetNameOfFocusedControl();
            var borderRadiusImage = EditorIcons.GetIconTexture(currentSelected switch
            {
                _ when currentSelected == topLeftName => TactileUIEditorIcon.BorderRadiusTopLeft,
                _ when currentSelected == topRightName => TactileUIEditorIcon.BorderRadiusTopRight,
                _ when currentSelected == bottomLeftName => TactileUIEditorIcon.BorderRadiusBottomLeft,
                _ when currentSelected == bottomRightName => TactileUIEditorIcon.BorderRadiusBottomRight,
                _ => TactileUIEditorIcon.BorderRadiusNoneSelected
            });

            GUI.DrawTexture(rects[0], borderRadiusImage, ScaleMode.StretchToFill);
        }

        public override float GetPropertyHeight(SerializedProperty property, GUIContent label)
        {
            return EditorGUIUtility.singleLineHeight;
        }
    }
}