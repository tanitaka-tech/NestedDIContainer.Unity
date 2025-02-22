using System.Collections.Generic;
using NestedDIContainer.Unity.Runtime;
using UnityEditor;
using UnityEngine;

namespace _Hierarchy.Common.NestedDIContainer.Unity.Editor
{
    public class NestedDIContainerProjectSettingsProvider : SettingsProvider
    {
        public NestedDIContainerProjectSettingsProvider(string path, SettingsScope scopes,
            IEnumerable<string> keywords = null) : base(path, scopes, keywords)
        {
        }

        [SettingsProvider]
        public static SettingsProvider CreateProvider()
        {
            var keywords = new[] { "NestedDIContainer" };
            return new NestedDIContainerProjectSettingsProvider("Project/TanitakaTech/NestedDIContainer",
                SettingsScope.Project, keywords);
        }

        public override void OnGUI(string searchContext)
        {
            using (new GUIScope())
            {
                var projectSettings = NestedDIContainerProjectSettings.instance;

                if (projectSettings.ProjectScopeReference == null)
                {
                    if(GUI.Button(new Rect(0, 0, 500, 100), "Create ProjectScopeReference"))
                    {
                        var projectScopeReference = ScriptableObject.CreateInstance<ProjectScopeReference>();
                        AssetDatabase.CreateAsset(projectScopeReference, "Assets/Resources/ProjectScopeReference.asset");
                        projectSettings.ProjectScopeReference = projectScopeReference;
                    }
                }
                else
                {
                    projectSettings.ProjectScope = EditorGUILayout.ObjectField("ProjectScope",
                        projectSettings.ProjectScope, typeof(ProjectScope), false) as ProjectScope;
                }
            }
        }

        private sealed class GUIScope : GUI.Scope
        {
            private const float LabelWidth = 250;
            private const float MarginLeft = 10;
            private const float MarginTop = 10;

            private readonly float _labelWidth;

            public GUIScope()
            {
                _labelWidth = EditorGUIUtility.labelWidth;
                EditorGUIUtility.labelWidth = LabelWidth;
                GUILayout.BeginHorizontal();
                GUILayout.Space(MarginLeft);
                GUILayout.BeginVertical();
                GUILayout.Space(MarginTop);
            }

            protected override void CloseScope()
            {
                GUILayout.EndVertical();
                GUILayout.EndHorizontal();
                EditorGUIUtility.labelWidth = _labelWidth;
            }
        }
    }
}