using NestedDIContainer.Unity.Runtime;
using UnityEditor;
using UnityEngine;

namespace NestedDIContainer.Unity.Editor
{
    [CustomEditor(typeof(ProjectScopeReference))]
    public class ProjectScopeReferenceEditor : UnityEditor.Editor
    {
        public override void OnInspectorGUI()
        {
            serializedObject.Update();

            var scopeProperty = serializedObject.FindProperty("_projectScope");

            if (scopeProperty.objectReferenceValue != null)
            {
                EditorGUILayout.PropertyField(scopeProperty);
                serializedObject.ApplyModifiedProperties();
                return;
            }

            GameObject gameObject = null;
            gameObject = EditorGUILayout.ObjectField("Project Scope", gameObject, typeof(GameObject), false) as GameObject;

            if (gameObject == null)
            {
                return;
            }

            var projectScope = gameObject.GetComponent<ProjectScope>();
            if (projectScope != null)
            {
                scopeProperty.objectReferenceValue = projectScope;
                serializedObject.ApplyModifiedProperties();
            }
            else
            {
                Debug.LogError($"ProjectScope is not found in the {gameObject.name}");
            }
        }
    }
}