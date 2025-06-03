#if USE_STATIC_DI_RESOLUTION && UNITY_EDITOR
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine.SceneManagement;
using NestedDIContainer.Unity.Runtime.Core;

namespace TanitakaTech.NestedDIContainer.Unity.Editor
{
    public class MonoBehaviourScopeBaseBatchProcessor : EditorWindow
    {
        private Vector2 scrollPosition;
        private List<MonoBehaviourScopeBase> foundScopes = new List<MonoBehaviourScopeBase>();
        private Dictionary<MonoBehaviourScopeBase, bool> scopeSelection = new Dictionary<MonoBehaviourScopeBase, bool>();
        private Dictionary<MonoBehaviourScopeBase, string> scopeLocations = new Dictionary<MonoBehaviourScopeBase, string>();
        
        private bool selectAll = true;
        private bool includePrefabs = true;
        private bool includeScenes = true;

        [MenuItem("Tools/NestedDI/Batch Collect Child Injectables")]
        public static void ShowWindow()
        {
            GetWindow<MonoBehaviourScopeBaseBatchProcessor>("Batch Collect Child Injectables");
        }

        private void OnGUI()
        {
            EditorGUILayout.LabelField("Batch Collect Child Injectables", EditorStyles.boldLabel);
            EditorGUILayout.Space();

            // 検索設定
            EditorGUILayout.LabelField("Search Settings", EditorStyles.boldLabel);
            includePrefabs = EditorGUILayout.Toggle("Include Prefabs", includePrefabs);
            includeScenes = EditorGUILayout.Toggle("Include Scenes", includeScenes);
            EditorGUILayout.Space();

            // 検索ボタン
            if (GUILayout.Button("Search All MonoBehaviourScopeBase"))
            {
                SearchAllMonoBehaviourScopeBase();
            }
            
            EditorGUILayout.Space();

            if (foundScopes.Count > 0)
            {
                // 全選択/全解除
                EditorGUILayout.BeginHorizontal();
                if (GUILayout.Button("Select All"))
                {
                    SetAllSelection(true);
                }
                if (GUILayout.Button("Deselect All"))
                {
                    SetAllSelection(false);
                }
                EditorGUILayout.EndHorizontal();

                EditorGUILayout.Space();

                // 一括実行ボタン
                GUI.backgroundColor = Color.green;
                if (GUILayout.Button("Collect Child Injectables for Selected", GUILayout.Height(30)))
                {
                    CollectChildInjectablesForSelected();
                }
                GUI.backgroundColor = Color.white;

                EditorGUILayout.Space();
                EditorGUILayout.LabelField($"Found {foundScopes.Count} MonoBehaviourScopeBase components:", EditorStyles.helpBox);

                // リスト表示
                scrollPosition = EditorGUILayout.BeginScrollView(scrollPosition);
                
                foreach (var scope in foundScopes)
                {
                    if (scope == null) continue;

                    EditorGUILayout.BeginHorizontal();
                    
                    // チェックボックス
                    bool isSelected = scopeSelection.ContainsKey(scope) ? scopeSelection[scope] : false;
                    bool newSelection = EditorGUILayout.Toggle(isSelected, GUILayout.Width(20));
                    scopeSelection[scope] = newSelection;

                    // オブジェクト参照
                    EditorGUI.BeginDisabledGroup(true);
                    EditorGUILayout.ObjectField(scope, typeof(MonoBehaviourScopeBase), true);
                    EditorGUI.EndDisabledGroup();

                    // 場所の表示
                    if (scopeLocations.ContainsKey(scope))
                    {
                        EditorGUILayout.LabelField(scopeLocations[scope], GUILayout.Width(200));
                    }

                    // 個別実行ボタン
                    if (GUILayout.Button("Collect", GUILayout.Width(60)))
                    {
                        CollectChildInjectablesForScope(scope);
                    }

                    EditorGUILayout.EndHorizontal();
                }
                
                EditorGUILayout.EndScrollView();
            }
        }

        private void SearchAllMonoBehaviourScopeBase()
        {
            foundScopes.Clear();
            scopeSelection.Clear();
            scopeLocations.Clear();

            // Prefabを検索
            if (includePrefabs)
            {
                SearchInPrefabs();
            }

            // Sceneを検索
            if (includeScenes)
            {
                SearchInScenes();
            }

            Debug.Log($"Found {foundScopes.Count} MonoBehaviourScopeBase components in total");
        }

        private void SearchInPrefabs()
        {
            string[] prefabGuids = AssetDatabase.FindAssets("t:Prefab");
            
            foreach (string guid in prefabGuids)
            {
                string path = AssetDatabase.GUIDToAssetPath(guid);
                
                // パッケージ内のアセットをスキップ
                if (IsInPackageFolder(path))
                {
                    continue;
                }

                GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(path);
                
                if (prefab != null)
                {
                    MonoBehaviourScopeBase[] scopes = prefab.GetComponentsInChildren<MonoBehaviourScopeBase>(true);
                    foreach (var scope in scopes)
                    {
                        foundScopes.Add(scope);
                        scopeSelection[scope] = selectAll;
                        scopeLocations[scope] = $"Prefab: {path}";
                    }
                }
            }
        }

        private void SearchInScenes()
        {
            // Assetsフォルダ内のすべてのシーンファイルを検索
            string[] sceneGuids = AssetDatabase.FindAssets("t:Scene");
            foreach (string guid in sceneGuids)
            {
                string scenePath = AssetDatabase.GUIDToAssetPath(guid);
                
                // パッケージ内のシーンをスキップ
                if (IsInPackageFolder(scenePath))
                {
                    Debug.Log($"Skipping package scene: {scenePath}");
                    continue;
                }

                SearchInScene(scenePath);
            }
        }

        private void SearchInScene(string scenePath)
        {
            if (string.IsNullOrEmpty(scenePath)) return;

            // 現在開いているシーンを保存
            Scene originalScene = SceneManager.GetActiveScene();
            string originalScenePath = originalScene.path;

            try
            {
                // 既に開いているシーンの場合は、シーンを開き直さずに検索
                if (originalScene.path == scenePath && originalScene.IsValid())
                {
                    SearchInCurrentScene(originalScene, scenePath);
                    return;
                }

                // シーンを開く前に、読み取り専用かチェック
                if (!IsSceneEditable(scenePath))
                {
                    Debug.Log($"Skipping read-only scene: {scenePath}");
                    return;
                }

                // シーンを開く
                Scene scene = EditorSceneManager.OpenScene(scenePath, OpenSceneMode.Single);
                
                if (scene.IsValid())
                {
                    SearchInCurrentScene(scene, scenePath);
                }
            }
            catch (System.Exception e)
            {
                Debug.LogWarning($"Failed to open scene {scenePath}: {e.Message}");
            }
            finally
            {
                // 元のシーンに戻す（異なるシーンを開いた場合のみ）
                if (!string.IsNullOrEmpty(originalScenePath) && originalScenePath != scenePath)
                {
                    try
                    {
                        EditorSceneManager.OpenScene(originalScenePath, OpenSceneMode.Single);
                    }
                    catch (System.Exception e)
                    {
                        Debug.LogWarning($"Failed to restore original scene {originalScenePath}: {e.Message}");
                    }
                }
            }
        }

        private void SearchInCurrentScene(Scene scene, string scenePath)
        {
            // ルートオブジェクトから検索
            GameObject[] rootObjects = scene.GetRootGameObjects();
            foreach (var rootObj in rootObjects)
            {
                MonoBehaviourScopeBase[] scopes = rootObj.GetComponentsInChildren<MonoBehaviourScopeBase>(true);
                foreach (var scope in scopes)
                {
                    if (!foundScopes.Contains(scope))
                    {
                        foundScopes.Add(scope);
                        scopeSelection[scope] = selectAll;
                        scopeLocations[scope] = $"Scene: {scenePath}";
                    }
                }
            }
        }

        private bool IsInPackageFolder(string assetPath)
        {
            // パッケージ内のアセットかどうかを判定
            return assetPath.StartsWith("Packages/") || 
                   assetPath.StartsWith("Library/PackageCache/");
        }

        private bool IsSceneEditable(string scenePath)
        {
            // シーンが編集可能かどうかを判定
            if (IsInPackageFolder(scenePath))
            {
                return false;
            }

            // アセットが読み取り専用かチェック
            var assetImporter = AssetImporter.GetAtPath(scenePath);
            return assetImporter != null && assetImporter.importSettingsMissing == false;
        }

        private void SetAllSelection(bool selected)
        {
            var keys = scopeSelection.Keys.ToList();
            foreach (var key in keys)
            {
                scopeSelection[key] = selected;
            }
        }

        private void CollectChildInjectablesForSelected()
        {
            var selectedScopes = scopeSelection.Where(kvp => kvp.Value).Select(kvp => kvp.Key).ToList();
            
            if (selectedScopes.Count == 0)
            {
                EditorUtility.DisplayDialog("Warning", "No scopes selected!", "OK");
                return;
            }

            int processedCount = 0;
            int totalCount = selectedScopes.Count;

            try
            {
                foreach (var scope in selectedScopes)
                {
                    if (scope != null)
                    {
                        EditorUtility.DisplayProgressBar("Collecting Child Injectables", 
                            $"Processing {scope.name}...", 
                            (float)processedCount / totalCount);

                        CollectChildInjectablesForScope(scope);
                        processedCount++;
                    }
                }

                AssetDatabase.SaveAssets();
                EditorUtility.DisplayDialog("Complete", 
                    $"Successfully processed {processedCount} MonoBehaviourScopeBase components!", "OK");
            }
            finally
            {
                EditorUtility.ClearProgressBar();
            }
        }

        private void CollectChildInjectablesForScope(MonoBehaviourScopeBase scope)
        {
            if (scope == null) return;

            // パッケージ内のアセットは編集不可なのでスキップ
            string assetPath = AssetDatabase.GetAssetPath(scope);
            if (IsInPackageFolder(assetPath))
            {
                Debug.LogWarning($"Skipping package asset: {scope.name} at {assetPath}");
                return;
            }

            try
            {
                scope.CollectChildInjectables();
                
                // Prefabの場合
                if (PrefabUtility.IsPartOfPrefabAsset(scope))
                {
                    EditorUtility.SetDirty(scope);
                }
                // Sceneオブジェクトの場合
                else if (scope.gameObject.scene.IsValid())
                {
                    EditorUtility.SetDirty(scope);
                    EditorSceneManager.MarkSceneDirty(scope.gameObject.scene);
                }

                Debug.Log($"Collected child injectables for {scope.name} (Location: {scopeLocations.GetValueOrDefault(scope, "Unknown")})");
            }
            catch (System.Exception e)
            {
                Debug.LogError($"Failed to collect child injectables for {scope.name}: {e.Message}");
            }
        }
    }
}
#endif