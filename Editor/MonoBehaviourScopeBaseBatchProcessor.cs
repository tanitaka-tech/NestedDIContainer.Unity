#if USE_STATIC_DI_RESOLUTION && UNITY_EDITOR
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine.SceneManagement;
using TanitakaTech.NestedDIContainer.Unity.Runtime.Core;

namespace TanitakaTech.NestedDIContainer.Unity.Editor
{
    // Scene内オブジェクトの情報を保持するクラス
    [System.Serializable]
    public class ScopeInfo
    {
        public string scenePath;
        public string objectPath; // Hierarchy内でのパス
        public string objectName;
        public MonoBehaviourScopeBase scopeReference; // Prefab用（Sceneの場合はnull）
        public bool isFromScene;

        public string DisplayLocation => isFromScene ? $"Scene: {scenePath}" : $"Prefab: {scenePath}";
    }

    public class MonoBehaviourScopeBaseBatchProcessor : EditorWindow
    {
        private Vector2 scrollPosition;
        private List<ScopeInfo> foundScopeInfos = new List<ScopeInfo>();
        private Dictionary<ScopeInfo, bool> scopeSelection = new Dictionary<ScopeInfo, bool>();
        
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

        public static int BatchCollectAllChildInjectables(bool includePrefabs = true, bool includeScenes = true, bool showProgressBar = true)
        {
            var foundScopeInfos = new List<ScopeInfo>();

            Debug.Log("Starting batch collection of child injectables...");

            try
            {
                if (showProgressBar)
                {
                    EditorUtility.DisplayProgressBar("Batch Collect Child Injectables", "Searching for MonoBehaviourScopeBase components...", 0f);
                }

                if (includePrefabs)
                {
                    SearchInPrefabsStatic(foundScopeInfos);
                }

                if (includeScenes)
                {
                    SearchInScenesStatic(foundScopeInfos);
                }

                Debug.Log($"Found {foundScopeInfos.Count} MonoBehaviourScopeBase components");

                if (foundScopeInfos.Count == 0)
                {
                    Debug.Log("No MonoBehaviourScopeBase components found to process");
                    return 0;
                }

                return ProcessFoundScopeInfos(foundScopeInfos, showProgressBar);
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

        public static int BatchCollectChildInjectablesForPaths(string[] assetPaths, bool showProgressBar = true)
        {
            var foundScopeInfos = new List<ScopeInfo>();

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
                        SearchInSinglePrefab(path, foundScopeInfos);
                    }
                    else if (path.EndsWith(".unity"))
                    {
                        SearchInSingleScene(path, foundScopeInfos);
                    }
                }

                return ProcessFoundScopeInfos(foundScopeInfos, showProgressBar);
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

           private static int ProcessFoundScopeInfos(List<ScopeInfo> foundScopeInfos, bool showProgressBar)
        {
            if (foundScopeInfos.Count == 0) return 0;

            int processedCount = 0;
            var processedScenes = new HashSet<string>();
            Scene originalScene = SceneManager.GetActiveScene();
            string originalScenePath = originalScene.path;

            try
            {
                for (int i = 0; i < foundScopeInfos.Count; i++)
                {
                    var scopeInfo = foundScopeInfos[i];

                    if (showProgressBar)
                    {
                        float progress = (float)i / foundScopeInfos.Count;
                        EditorUtility.DisplayProgressBar("Collecting Child Injectables", 
                            $"Processing {scopeInfo.objectName}... ({i + 1}/{foundScopeInfos.Count})", progress);
                    }

                    if (CollectChildInjectablesForScopeInfo(scopeInfo, processedScenes))
                    {
                        processedCount++;
                    }
                }

                AssetDatabase.SaveAssets();
                Debug.Log($"Successfully processed {processedCount}/{foundScopeInfos.Count} MonoBehaviourScopeBase components");
                return processedCount;
            }
            finally
            {
                // プログレスバーをクリア
                if (showProgressBar)
                {
                    EditorUtility.ClearProgressBar();
                }
                
                // 元のSceneに戻す
                if (!string.IsNullOrEmpty(originalScenePath))
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

private static bool CollectChildInjectablesForScopeInfo(ScopeInfo scopeInfo, HashSet<string> processedScenes)
        {
            if (scopeInfo == null) return false;

            try
            {
                MonoBehaviourScopeBase scope = null;

                if (scopeInfo.isFromScene)
                {
                    // Scene内のオブジェクトの場合
                    if (!processedScenes.Contains(scopeInfo.scenePath))
                    {
                        Scene scene = EditorSceneManager.OpenScene(scopeInfo.scenePath, OpenSceneMode.Single);
                        if (!scene.IsValid())
                        {
                            Debug.LogWarning($"Failed to open scene: {scopeInfo.scenePath}");
                            return false;
                        }
                        processedScenes.Add(scopeInfo.scenePath);
                    }

                    // オブジェクトパスからオブジェクトを取得
                    scope = FindScopeByPath(scopeInfo.objectPath, scopeInfo.objectName);
                }
                else
                {
                    // Prefabの場合
                    scope = scopeInfo.scopeReference;
                }

                if (scope == null)
                {
                    Debug.LogWarning($"Failed to find scope: {scopeInfo.objectName} in {scopeInfo.scenePath}");
                    return false;
                }

                return CollectChildInjectablesForScope(scope, scopeInfo.isFromScene);
            }
            catch (System.Exception e)
            {
                Debug.LogError($"Failed to process scope {scopeInfo.objectName}: {e.Message}");
                return false;
            }
        }

        private static bool CollectChildInjectablesForScope(MonoBehaviourScopeBase scope, bool isFromScene = false)
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
                    
                    // Scene内のオブジェクトの場合、即座にSceneを保存してgit差分に反映
                    if (isFromScene)
                    {
                        EditorSceneManager.SaveScene(scope.gameObject.scene);
                    }
                }

                Debug.Log($"Collected child injectables for {scope.name}");
                return true;
            }
            catch (System.Exception e)
            {
                Debug.LogError($"Failed to collect child injectables for {scope.name}: {e.Message}");
                return false;
            }
        }

        private static MonoBehaviourScopeBase FindScopeByPath(string objectPath, string objectName)
        {
            // Hierarchy内のパスからオブジェクトを検索
            GameObject[] allObjects = GameObject.FindObjectsOfType<GameObject>();
            
            foreach (var obj in allObjects)
            {
                if (obj.name == objectName)
                {
                    string currentPath = GetGameObjectPath(obj);
                    if (currentPath == objectPath)
                    {
                        return obj.GetComponent<MonoBehaviourScopeBase>();
                    }
                }
            }
            
            return null;
        }

        private static string GetGameObjectPath(GameObject obj)
        {
            string path = obj.name;
            Transform parent = obj.transform.parent;
            
            while (parent != null)
            {
                path = parent.name + "/" + path;
                parent = parent.parent;
            }
            
            return path;
        }

        private static bool CollectChildInjectablesForScope(MonoBehaviourScopeBase scope)
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

                Debug.Log($"Collected child injectables for {scope.name}");
                return true;
            }
            catch (System.Exception e)
            {
                Debug.LogError($"Failed to collect child injectables for {scope.name}: {e.Message}");
                return false;
            }
        }

        private static void SearchInPrefabsStatic(List<ScopeInfo> foundScopeInfos)
        {
            string[] prefabGuids = AssetDatabase.FindAssets("t:Prefab");
            
            foreach (string guid in prefabGuids)
            {
                string path = AssetDatabase.GUIDToAssetPath(guid);
                if (IsInPackageFolder(path)) continue;

                SearchInSinglePrefab(path, foundScopeInfos);
            }
        }

        private static void SearchInSinglePrefab(string path, List<ScopeInfo> foundScopeInfos)
        {
            GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(path);
            
            if (prefab != null)
            {
                MonoBehaviourScopeBase[] scopes = prefab.GetComponentsInChildren<MonoBehaviourScopeBase>(true);
                foreach (var scope in scopes)
                {
                    var scopeInfo = new ScopeInfo
                    {
                        scenePath = path,
                        objectPath = GetGameObjectPath(scope.gameObject),
                        objectName = scope.name,
                        scopeReference = scope,
                        isFromScene = false
                    };
                    foundScopeInfos.Add(scopeInfo);
                }
            }
        }

        private static void SearchInScenesStatic(List<ScopeInfo> foundScopeInfos)
        {
            string[] sceneGuids = AssetDatabase.FindAssets("t:Scene");
            foreach (string guid in sceneGuids)
            {
                string scenePath = AssetDatabase.GUIDToAssetPath(guid);
                if (IsInPackageFolder(scenePath)) continue;

                SearchInSingleScene(scenePath, foundScopeInfos);
            }
        }

  private static void SearchInSingleScene(string scenePath, List<ScopeInfo> foundScopeInfos)
        {
            if (string.IsNullOrEmpty(scenePath) || !IsSceneEditable(scenePath)) return;

            Scene originalScene = SceneManager.GetActiveScene();
            string originalScenePath = originalScene.path;

            try
            {
                Scene scene;
                if (originalScene.path == scenePath && originalScene.IsValid())
                {
                    scene = originalScene;
                }
                else
                {
                    scene = EditorSceneManager.OpenScene(scenePath, OpenSceneMode.Single);
                }

                if (scene.IsValid())
                {
                    GameObject[] rootObjects = scene.GetRootGameObjects();
                    foreach (var rootObj in rootObjects)
                    {
                        MonoBehaviourScopeBase[] scopes = rootObj.GetComponentsInChildren<MonoBehaviourScopeBase>(true);
                        foreach (var scope in scopes)
                        {
                            // Scene内のPrefabインスタンス内のMonoBehaviourScopeBaseは除外
                            if (PrefabUtility.IsPartOfPrefabInstance(scope.gameObject))
                            {
                                continue;
                            }

                            var scopeInfo = new ScopeInfo
                            {
                                scenePath = scenePath,
                                objectPath = GetGameObjectPath(scope.gameObject),
                                objectName = scope.name,
                                scopeReference = null, // Scene内オブジェクトは参照を保存しない
                                isFromScene = true
                            };
                            foundScopeInfos.Add(scopeInfo);
                        }
                    }
                }
            }
            catch (System.Exception e)
            {
                Debug.LogWarning($"Failed to search in scene {scenePath}: {e.Message}");
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

        // UI関連のメソッド
        // UI関連のメソッド
        private void OnGUI()
        {
            EditorGUILayout.LabelField("Batch Collect Child Injectables", EditorStyles.boldLabel);
            EditorGUILayout.Space();

            EditorGUILayout.LabelField("Search Settings", EditorStyles.boldLabel);
            includePrefabs = EditorGUILayout.Toggle("Include Prefabs", includePrefabs);
            includeScenes = EditorGUILayout.Toggle("Include Scenes", includeScenes);
            EditorGUILayout.Space();

            if (GUILayout.Button("Search All MonoBehaviourScopeBase"))
            {
                SearchAllMonoBehaviourScopeBase();
            }
            
            EditorGUILayout.Space();

            if (foundScopeInfos.Count > 0)
            {
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

                GUI.backgroundColor = Color.green;
                if (GUILayout.Button("Collect Child Injectables for Selected", GUILayout.Height(30)))
                {
                    CollectChildInjectablesForSelected();
                }
                GUI.backgroundColor = Color.white;

                EditorGUILayout.Space();
                EditorGUILayout.LabelField($"Found {foundScopeInfos.Count} MonoBehaviourScopeBase components:", EditorStyles.helpBox);

                scrollPosition = EditorGUILayout.BeginScrollView(scrollPosition);
                
                foreach (var scopeInfo in foundScopeInfos)
                {
                    if (scopeInfo == null) continue;

                    EditorGUILayout.BeginHorizontal();
                    
                    bool isSelected = scopeSelection.ContainsKey(scopeInfo) ? scopeSelection[scopeInfo] : false;
                    bool newSelection = EditorGUILayout.Toggle(isSelected, GUILayout.Width(20));
                    scopeSelection[scopeInfo] = newSelection;

                    // Scene内オブジェクトの場合は「Scene名/GameObject名」形式で表示
                    string displayName;
                    if (scopeInfo.isFromScene)
                    {
                        string sceneName = System.IO.Path.GetFileNameWithoutExtension(scopeInfo.scenePath);
                        displayName = $"{sceneName}/{scopeInfo.objectName}";
                    }
                    else
                    {
                        displayName = scopeInfo.objectName;
                    }

                    EditorGUILayout.LabelField(displayName, GUILayout.Width(200));
                    EditorGUILayout.LabelField(scopeInfo.DisplayLocation, GUILayout.Width(200));

                    if (GUILayout.Button("Collect", GUILayout.Width(60)))
                    {
                        CollectChildInjectablesForScopeInfo(scopeInfo, new HashSet<string>());
                    }

                    EditorGUILayout.EndHorizontal();
                }
                
                EditorGUILayout.EndScrollView();
            }
        }

        private void SearchAllMonoBehaviourScopeBase()
        {
            foundScopeInfos.Clear();
            scopeSelection.Clear();

            if (includePrefabs)
            {
                SearchInPrefabsStatic(foundScopeInfos);
            }

            if (includeScenes)
            {
                SearchInScenesStatic(foundScopeInfos);
            }

            foreach (var scopeInfo in foundScopeInfos)
            {
                scopeSelection[scopeInfo] = selectAll;
            }
            
            Debug.Log($"Found {foundScopeInfos.Count} MonoBehaviourScopeBase components in total.");
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
            var selectedScopeInfos = scopeSelection.Where(kvp => kvp.Value).Select(kvp => kvp.Key).ToList();
            
            if (selectedScopeInfos.Count == 0)
            {
                EditorUtility.DisplayDialog("Warning", "No scopes selected!", "OK");
                return;
            }

            try
            {
                int processedCount = ProcessFoundScopeInfos(selectedScopeInfos, true);
                
                EditorUtility.DisplayDialog("Complete", 
                    $"Successfully processed {processedCount} MonoBehaviourScopeBase components!", "OK");
            }
            finally
            {
                // 念のため再度プログレスバーをクリア
                EditorUtility.ClearProgressBar();
            }
        }

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
    }
}
#endif