using System;
using System.Collections.Generic;
using TanitakaTech.NestedDIContainer;
using TanitakaTech.NestedDIContainer.Unity.Runtime.Scopes;
using UnityEngine;

namespace TanitakaTech.NestedDIContainer.Unity.Runtime
{
    public abstract class ProjectScope : MonoBehaviourScope
    {
        internal static ProjectScope Scope => _projectScope;
        private static ProjectScope _projectScope;

        internal static List<IAsyncInitializer> Initializers { get; } = new ();

        internal static ProjectScope CreateProjectScope()
        {
            Dispose();

            if (_projectScope != null)
            {
                return _projectScope;
            }
            var loaded = Resources.Load($"ProjectScopeReference");
            var projectScopeReference = loaded as ProjectScopeReference;
            if (projectScopeReference == null)
            {
                throw new Exception("ProjectScopeReference was not found. Please check the ProjectSettings.");
            }
            _projectScope = projectScopeReference.CreateProjectScope();
            _projectScope.ConstructScope(ScopeId.Create(), null, optionExtendScope: new ProjectScopeDefaultExtendScope(Scope));
            return _projectScope;
        }
        
        /// <summary>
        /// 読み込み中のシーンへ渡す config と親スコープ。
        /// NOTE: 単一のスロットで持つと、シーンの読み込みが同時に走ったときに後から積んだ config で上書きされ、
        ///       別のシーンの config を取り出して InvalidCastException になっていた。
        ///       複数積めるようにしたうえで、シーン側が期待する config の型で取り出す
        /// </summary>
        private static readonly List<PendingSceneScope> _pendingSceneScopes = new();

        private static long _nextPendingSceneScopeId = 0;

        /// <summary>
        /// シーンの読み込み前に config と親スコープを積む
        /// </summary>
        /// <returns>取り下げに使うId（<see cref="RemovePendingSceneScope"/>）</returns>
        internal static long PushPendingSceneScope(IScope parentScope, object config)
        {
            var id = _nextPendingSceneScopeId++;
            _pendingSceneScopes.Add(new PendingSceneScope(id, parentScope, config));
            return id;
        }

        /// <summary>
        /// 積んだ config と親スコープを取り出す。
        /// 同時に複数積まれている場合は、シーン側が期待する型の config を優先して選ぶ
        /// </summary>
        /// <param name="configType">シーン側が期待する config の型</param>
        internal static bool TryPopPendingSceneScope(Type configType, out IScope parentScope, out object config)
        {
            // 1. 期待する型の config を積んだ順に探す
            for (var i = 0; i < _pendingSceneScopes.Count; i++)
            {
                var pending = _pendingSceneScopes[i];
                if (pending.Config != null && configType.IsInstanceOfType(pending.Config))
                {
                    return PopAt(i, out parentScope, out config);
                }
            }

            // 2. config を持たないシーン（SceneScope）向けに、config が null のものを積んだ順に探す
            for (var i = 0; i < _pendingSceneScopes.Count; i++)
            {
                if (_pendingSceneScopes[i].Config == null)
                {
                    return PopAt(i, out parentScope, out config);
                }
            }

            // 3. どれとも一致しない場合は、積んだ順で最も古いものを返す（単一スロットだった頃と同じ挙動）
            if (_pendingSceneScopes.Count > 0)
            {
                return PopAt(0, out parentScope, out config);
            }

            parentScope = null;
            config = null;
            return false;

            static bool PopAt(int index, out IScope popedParentScope, out object popedConfig)
            {
                var pending = _pendingSceneScopes[index];
                _pendingSceneScopes.RemoveAt(index);
                popedParentScope = pending.ParentScope;
                popedConfig = pending.Config;
                return true;
            }
        }

        /// <summary>
        /// 積んだ config と親スコープを取り下げる。既に取り出されている場合は何もしない。
        /// NOTE: 読み込みに失敗するとシーンのAwakeが走らず、積んだままの config を別のシーンが拾ってしまうため取り下げる
        /// </summary>
        internal static void RemovePendingSceneScope(long id)
        {
            for (var i = 0; i < _pendingSceneScopes.Count; i++)
            {
                if (_pendingSceneScopes[i].Id == id)
                {
                    _pendingSceneScopes.RemoveAt(i);
                    return;
                }
            }
        }

        private readonly struct PendingSceneScope
        {
            public long Id { get; }
            public IScope ParentScope { get; }
            public object Config { get; }

            public PendingSceneScope(long id, IScope parentScope, object config)
            {
                Id = id;
                ParentScope = parentScope;
                Config = config;
            }
        }
        
        protected void Awake()
        {
            _projectScope = this;
#if UNITY_EDITOR
            if (gameObject.scene.name != "DontDestroyOnLoad")
                throw new ConstructException("ProjectScope must not be in a scene");
#endif
        }
        
        protected void OnDestroy()
        {
            Dispose();
        }

        private static void Dispose()
        {
            _projectScope = null;
            _pendingSceneScopes.Clear();
        }
    }
}