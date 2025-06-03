
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

        [MenuItem("Tools/NestedDI/Open Batch Collect Child Injectables Window")]
        public static void ShowWindow()
        {
            GetWindow<MonoBehaviourScopeBaseBatchProcessor>("Batch Collect Child Injectables");
        }

        [MenuItem("Tools/NestedDI/Auto Collect All Child Injectables")]
        public static void AutoCollectAllChildInjectables()
        {
            BatchCollectAllChildInjectables(includePrefabs: true, includeScenes: true, showProgressBar: true);
        }

        /// <summary>
        /// すべてのPrefabとSceneからMonoBehaviourScopeBaseを検索し、CollectChildInjectablesを実行
        /// </summary>
        /// <param name="includePrefabs">Prefabを検索対象に含めるか</param>
        /// <param name="includeScenes">Sceneを検索対象に含めるか</param>
        /// <param name="showProgressBar">プログレスバーを表示するか</param>
        /// <returns>処理したMonoBehaviourScopeBaseの数</returns>
        public static int BatchCollectAllChildInjectables(bool includePrefabs = true, bool includeScenes = true, bool showProgressBar = true)
        {
            var foundScopes = new List<MonoBehaviourScopeBase>();
            var scopeLocations = new Dictionary<MonoBehaviourScopeBase, string>();

            Debug.Log("Starting batch collection of child injectables...");

            try
            {
                // 1. 検索フェーズ
                if (showProgressBar)
                {
                    EditorUtility.DisplayProgressBar("Batch Collect Child Injectables", "Searching for MonoBehaviourScopeBase components...", 0f);
                }

                if (includePrefabs)
                {
                    SearchInPrefabsStatic(foundScopes, scopeLocations);
                }

                if (includeScenes)
                {
                    SearchInScenesStatic(foundScopes, scopeLocations);
                }

                Debug.Log($"Found {foundScopes.Count} MonoBehaviourScopeBase components");

                if (foundScopes.Count == 0)
                {
                    Debug.Log("No MonoBehaviourScopeBase components found to process");
                    return 0;
                }

                // 2. 処理フェーズ
                int processedCount = 0;
                int totalCount = foundScopes.Count;

                for (int i = 0; i < foundScopes.Count; i++)
                {
                    var scope = foundScopes[i];
                    if (scope == null) continue;

                    if (showProgressBar)
                    {
                        float progress = (float)i / totalCount;
                        EditorUtility.DisplayProgressBar("Collecting Child Injectables", 
                            $"Processing {scope.name}... ({i + 1}/{totalCount})", progress);
                    }

                    if (CollectChildInjectablesForScopeStatic(scope, scopeLocations))
                    {
                        processedCount++;
                    }
                }

                // 3. 保存
                if (showProgressBar)
                {
                    EditorUtility.DisplayProgressBar("Batch Collect Child Injectables", "Saving assets...", 1f);
                }

                AssetDatabase.SaveAssets();
                EditorUtility.ClearProgressBar();

                Debug.Log($"Successfully processed {processedCount}/{totalCount} MonoBehaviourScopeBase components");
                return processedCount;
            }
            catch (System.Exception e)
            {
                Debug.LogError($"Error during batch collection: {e.Message}\n{e.StackTrace}");
                return 0;
            }
            finally
            {
                if (showProgressBar)
                {
                    EditorUtility.ClearProgressBar();
                }
            }
        }

        /// <summary>
        /// 特定のパスのアセットのみを対象に処理する
        /// </summary>
        /// <param name="assetPaths">対象のアセットパス配列</param>
        /// <param name="showProgressBar">プログレスバーを表示するか</param>
        /// <returns>処理したMonoBehaviourScopeBaseの数</returns>
        public static int BatchCollectChildInjectablesForPaths(string[] assetPaths, bool showProgressBar = true)
        {
            var foundScopes = new List<MonoBehaviourScopeBase>();
            var scopeLocations = new Dictionary<MonoBehaviourScopeBase, string>();

            Debug.Log($"Starting batch collection for {assetPaths.Length} specified paths...");

            try
            {
                if (showProgressBar)
                {
                    EditorUtility.DisplayProgressBar("Batch Collect Child Injectables", "Searching in specified paths...", 0f);
                }

                foreach (string path in assetPaths)
                {
                    if (IsInPackageFolder(path)) continue;

                    if (path.EndsWith(".prefab"))
                    {
                        SearchInSinglePrefab(path, foundScopes, scopeLocations);
                    }
                    else if (path.EndsWith(".unity"))
                    {
                        SearchInSingleScene(path, foundScopes, scopeLocations);
                    }
                }

                return ProcessFoundScopes(foundScopes, scopeLocations, showProgressBar);
            }
            catch (System.Exception e)
            {
                Debug.LogError($"Error during batch collection for specified paths: {e.Message}");
                return 0;
            }
            finally
            {
                if (showProgressBar)
                {
                    EditorUtility.ClearProgressBar();
                }
            }
        }

        private static int ProcessFoundScopes(List<MonoBehaviourScopeBase> foundScopes, Dictionary<MonoBehaviourScopeBase, string> scopeLocations, bool showProgressBar)
        {
            if (foundScopes.Count == 0) return 0;

            int processedCount = 0;
            for (int i = 0; i < foundScopes.Count; i++)
            {
                var scope = foundScopes[i];
                if (scope == null) continue;

                if (showProgressBar)
                {
                    float progress = (float)i / foundScopes.Count;
                    EditorUtility.DisplayProgressBar("Collecting Child Injectables", 
                        $"Processing {scope.name}... ({i + 1}/{foundScopes.Count})", progress);
                }

                if (CollectChildInjectablesForScopeStatic(scope, scopeLocations))
                {
                    processedCount++;
                }
            }

            AssetDatabase.SaveAssets();
            Debug.Log($"Successfully processed {processedCount}/{foundScopes.Count} MonoBehaviourScopeBase components");
            return processedCount;
        }

        private static void SearchInPrefabsStatic(List<MonoBehaviourScopeBase> foundScopes, Dictionary<MonoBehaviourScopeBase, string> scopeLocations)
        {
            string[] prefabGuids = AssetDatabase.FindAssets("t:Prefab");
            
            foreach (string guid in prefabGuids)
            {
                string path = AssetDatabase.GUIDToAssetPath(guid);
                if (IsInPackageFolder(path)) continue;

                SearchInSinglePrefab(path, foundScopes, scopeLocations);
            }
        }

        private static void SearchInSinglePrefab(string path, List<MonoBehaviourScopeBase> foundScopes, Dictionary<MonoBehaviourScopeBase, string> scopeLocations)
        {
            GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(path);
            
            if (prefab != null)
            {
                MonoBehaviourScopeBase[] scopes = prefab.GetComponentsInChildren<MonoBehaviourScopeBase>(true);
                foreach (var scope in scopes)
                {
                    if (!foundScopes.Contains(scope))
                    {
                        foundScopes.Add(scope);
                        scopeLocations[scope] = $"Prefab: {path}";
                    }
                }
            }
        }

        private static void SearchInScenesStatic(List<MonoBehaviourScopeBase> foundScopes, Dictionary<MonoBehaviourScopeBase, string> scopeLocations)
        {
            string[] sceneGuids = AssetDatabase.FindAssets("t:Scene");
            foreach (string guid in sceneGuids)
            {
                string scenePath = AssetDatabase.GUIDToAssetPath(guid);
                if (IsInPackageFolder(scenePath)) continue;

                SearchInSingleScene(scenePath, foundScopes, scopeLocations);
            }
        }

        private static void SearchInSingleScene(string scenePath, List<MonoBehaviourScopeBase> foundScopes, Dictionary<MonoBehaviourScopeBase, string> scopeLocations)
        {
            if (string.IsNullOrEmpty(scenePath) || !IsSceneEditable(scenePath)) return;

            Scene originalScene = SceneManager.GetActiveScene();
            string originalScenePath = originalScene.path;

            try
            {
                if (originalScene.path == scenePath && originalScene.IsValid())
                {
                    SearchInCurrentSceneStatic(originalScene, scenePath, foundScopes, scopeLocations);
                    return;
                }

                Scene scene = EditorSceneManager.OpenScene(scenePath, OpenSceneMode.Single);
                if (scene.IsValid())
                {
                    SearchInCurrentSceneStatic(scene, scenePath, foundScopes, scopeLocations);
                }
            }
            catch (System.Exception e)
            {
                Debug.LogWarning($"Failed to open scene {scenePath}: {e.Message}");
            }
            finally
            {
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

        private static void SearchInCurrentSceneStatic(Scene scene, string scenePath, List<MonoBehaviourScopeBase> foundScopes, Dictionary<MonoBehaviourScopeBase, string> scopeLocations)
        {
            GameObject[] rootObjects = scene.GetRootGameObjects();
            foreach (var rootObj in rootObjects)
            {
                MonoBehaviourScopeBase[] scopes = rootObj.GetComponentsInChildren<MonoBehaviourScopeBase>(true);
                foreach (var scope in scopes)
                {
                    if (!foundScopes.Contains(scope))
                    {
                        foundScopes.Add(scope);
                        scopeLocations[scope] = $"Scene: {scenePath}";
                    }
                }
            }
        }

        private static bool CollectChildInjectablesForScopeStatic(MonoBehaviourScopeBase scope, Dictionary<MonoBehaviourScopeBase, string> scopeLocations)
        {
            if (scope == null) return false;

            string assetPath = AssetDatabase.GetAssetPath(scope);
            if (IsInPackageFolder(assetPath))
            {
                Debug.LogWarning($"Skipping package asset: {scope.name} at {assetPath}");
                return false;
            }

            try
            {
                scope.CollectChildInjectables();
                
                if (PrefabUtility.IsPartOfPrefabAsset(scope))
                {
                    EditorUtility.SetDirty(scope);
                }
                else if (scope.gameObject.scene.IsValid())
                {
                    EditorUtility.SetDirty(scope);
                    EditorSceneManager.MarkSceneDirty(scope.gameObject.scene);
                }

                string location = scopeLocations.ContainsKey(scope) ? scopeLocations[scope] : "Unknown";
                Debug.Log($"Collected child injectables for {scope.name} (Location: {location})");
                return true;
            }
            catch (System.Exception e)
            {
                Debug.LogError($"Failed to collect child injectables for {scope.name}: {e.Message}");
                return false;
            }
        }

        // 既存のUI関連のメソッドは省略（変更なし）
        // ... OnGUI, SearchAllMonoBehaviourScopeBase, etc.

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

            if (includePrefabs)
            {
                SearchInPrefabs();
            }

            if (includeScenes)
            {
                SearchInScenes();
            }

            Debug.Log($"Found {foundScopes.Count} MonoBehaviourScopeBase components in total");
        }

        private void SearchInPrefabs()
        {
            SearchInPrefabsStatic(foundScopes, scopeLocations);
            
            // UIでの選択状態を設定
            foreach (var scope in foundScopes)
            {
                if (!scopeSelection.ContainsKey(scope))
                {
                    scopeSelection[scope] = selectAll;
                }
            }
        }

        private void SearchInScenes()
        {
            SearchInScenesStatic(foundScopes, scopeLocations);
            
            // UIでの選択状態を設定
            foreach (var scope in foundScopes)
            {
                if (!scopeSelection.ContainsKey(scope))
                {
                    scopeSelection[scope] = selectAll;
                }
            }
        }

        // 残りのprivateメソッドも同様に既存コードを維持
        private static bool IsInPackageFolder(string assetPath)
        {
            return assetPath.StartsWith("Packages/") || 
                   assetPath.StartsWith("Library/PackageCache/");
        }

        private static bool IsSceneEditable(string scenePath)
        {
            if (IsInPackageFolder(scenePath))
            {
                return false;
            }

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

            int processedCount = ProcessFoundScopes(selectedScopes, scopeLocations, true);
            
            EditorUtility.DisplayDialog("Complete", 
                $"Successfully processed {processedCount} MonoBehaviourScopeBase components!", "OK");
        }

        private void CollectChildInjectablesForScope(MonoBehaviourScopeBase scope)
        {
            CollectChildInjectablesForScopeStatic(scope, scopeLocations);
        }
    }
}
#endif