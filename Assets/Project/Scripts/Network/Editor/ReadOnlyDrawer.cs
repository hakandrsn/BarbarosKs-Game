using UnityEditor;
using UnityEngine;

namespace Project.Scripts.Network.Editor
{
    [CustomPropertyDrawer(typeof(ReadOnlyAttribute))]
    public class ReadOnlyDrawer : PropertyDrawer
    {
        public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
        {
            // Salt okunur görünüm için GUI etkinliğini geçici olarak devre dışı bırak
            GUI.enabled = false;

            // Standart property alanını çiz
            EditorGUI.PropertyField(position, property, label, true);

            // GUI etkinliğini geri yükle
            GUI.enabled = true;
        }

        public override float GetPropertyHeight(SerializedProperty property, GUIContent label)
        {
            // Varsayılan yüksekliği kullan
            return EditorGUI.GetPropertyHeight(property, label, true);
        }
    }
}