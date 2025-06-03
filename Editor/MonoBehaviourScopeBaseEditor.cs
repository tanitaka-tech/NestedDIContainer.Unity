#if USE_STATIC_DI_RESOLUTION
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEditor;
using NestedDIContainer.Unity.Runtime.Core;

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
                CollectChildInjectables();
            }

            // 現在の_childInjectablesの内容を表示
            if (_childInjectablesProperty != null && _childInjectablesProperty.arraySize > 0)
            {
                // 全てクリアするボタン
                if (GUILayout.Button("Clear All Child Injectables"))
                {
                    _childInjectablesProperty.ClearArray();
                    serializedObject.ApplyModifiedProperties();
                }
            }

            serializedObject.ApplyModifiedProperties();
        }

        private void CollectChildInjectables()
        {
            var target = this.target as MonoBehaviourScopeBase;
            if (target == null) return;

            // 現在のリストをクリア
            _childInjectablesProperty.ClearArray();

            var allInjectables = new List<MonoBehaviour>();

            // 1. 同じGameObjectの他のIInjectableコンポーネントを収集
            var sameGameObjectInjectables = target.gameObject.GetComponents<MonoBehaviour>()
                .Where(component => component is IInjectable && component != target)
                .ToList();
            allInjectables.AddRange(sameGameObjectInjectables);

            // 2. 子オブジェクトから IInjectable を実装した MonoBehaviour を収集
            var childInjectables = CollectChildInjectablesRecursive(target.transform);
            allInjectables.AddRange(childInjectables);

            // SerializedProperty に追加
            foreach (var injectable in allInjectables)
            {
                _childInjectablesProperty.arraySize++;
                var newElement = _childInjectablesProperty.GetArrayElementAtIndex(_childInjectablesProperty.arraySize - 1);
                newElement.objectReferenceValue = injectable;
            }

            serializedObject.ApplyModifiedProperties();
            
            Debug.Log($"Collected {allInjectables.Count} injectables for {target.name} (Same GameObject: {sameGameObjectInjectables.Count}, Children: {childInjectables.Count})");
        }

        private List<MonoBehaviour> CollectChildInjectablesRecursive(Transform current)
        {
            var result = new List<MonoBehaviour>();
            var target = this.target as MonoBehaviourScopeBase;
            
            foreach (Transform child in current)
            {
                CollectInjectablesFromChildComponents(child);
            }

            return result;

            void CollectInjectablesFromChildComponents(Transform child)
            {
                // 現在の子オブジェクトの IInjectable を収集
                var injectables = child.GetComponents<MonoBehaviour>()
                    .Where(component => component is IInjectable && component != target)
                    .ToList();

                result.AddRange(injectables);

                // 子の MonoBehaviourScopeBase がない場合のみ、さらに深く探索
                var childScope = child.GetComponent<MonoBehaviourScopeBase>();
                if (childScope == null)
                {
                    result.AddRange(CollectChildInjectablesRecursive(child));
                }
            }
        }
    }
}
#endif