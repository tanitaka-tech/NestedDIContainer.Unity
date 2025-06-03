#if USE_STATIC_DI_RESOLUTION
using UnityEngine;
using UnityEditor;
using TanitakaTech.NestedDIContainer.Unity.Runtime.Core;

namespace TanitakaTech.NestedDIContainer.Unity.Editor
{
    [CustomEditor(typeof(MonoBehaviourScopeBase), true)]
    public class MonoBehaviourScopeBaseEditor : UnityEditor.Editor
    {
        private SerializedProperty _extendScopesProperty;
        private SerializedProperty _childInjectablesProperty;

        private void OnEnable()
        {
            _extendScopesProperty = serializedObject.FindProperty("_extendScopes");
            _childInjectablesProperty = serializedObject.FindProperty("_childInjectables");
        }

        public override void OnInspectorGUI()
        {
            serializedObject.Update();

            // デフォルトのプロパティを描画
            DrawDefaultInspector();

            EditorGUILayout.Space();
            EditorGUILayout.LabelField("Child Injectables Management", EditorStyles.boldLabel);

            // 子のInjectableを収集するボタン
            if (GUILayout.Button("Collect Child Injectables"))
            {
                var targetScope = target as MonoBehaviourScopeBase;
                targetScope?.CollectChildInjectables();
                serializedObject.Update(); // SerializedObjectを更新
            }

            // 現在の_childInjectablesの内容を表示
            if (_childInjectablesProperty != null && _childInjectablesProperty.arraySize > 0)
            {
                // 全てクリアするボタン
                if (GUILayout.Button("Clear All Child Injectables"))
                {
                    var targetScope = target as MonoBehaviourScopeBase;
                    targetScope?.ClearChildInjectables();
                    serializedObject.Update();
                }
            }
            serializedObject.ApplyModifiedProperties();
        }
    }
}
#endif