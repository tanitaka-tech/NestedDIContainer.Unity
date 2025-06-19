using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using Cysharp.Threading.Tasks;
using Cysharp.Threading.Tasks.Triggers;
using TanitakaTech.NestedDIContainer;
using TanitakaTech.NestedDIContainer.Unity.Runtime.Scopes;
using UnityEngine;
using UnityEngine.SceneManagement;
using IInjectable = TanitakaTech.NestedDIContainer.IInjectable;

namespace TanitakaTech.NestedDIContainer.Unity.Runtime.Core
{
    [DefaultExecutionOrder(-5000)]
    public abstract class MonoBehaviourScopeBase : MonoBehaviour, IScope, IChildSceneScopeLoader, IPrefabScopeInstantiator
    {
        [SerializeField] protected List<ScriptableObjectExtendScope> _extendScopes;
#if USE_STATIC_DI_RESOLUTION
        [SerializeField] protected List<Component> _childInjectables;
#endif

        public ScopeId ScopeId { get; private set; }
        public IScope ParentScope => ScopeContainer.ParentScope;
        public ScopeContainer ScopeContainer { get; private set; }
        
        void IScope.Construct(DependencyBinder binder, object config)
        {
            Construct(binder, config);
        }
        protected abstract void Construct(DependencyBinder binder, object config);

        public T Instantiate<T>(T prefab, Transform parent, object config = null, Func<T, Transform, T> instantiateFunc = null) where T : Component
        {
            instantiateFunc ??= UnityEngine.Object.Instantiate;
            var instance = instantiateFunc(prefab, parent);
            if (instance is MonoBehaviourScopeBase monoBehaviourScope)
            {
                monoBehaviourScope.ConstructScope(
                    ScopeId.Create(), 
                    ScopeContainer, 
                    config,
                    new MonoBehaviourScopeDefaultExtendScope(monoBehaviourScope, monoBehaviourScope)
                    );
            }
            else
            {
                DynamicInjectOrInitializeChildrenRecursive(instance.transform);
            }
            return instance;
        }
        
        public async UniTask<T> InstantiateAsync<T>(Func<UniTask<T>> instantiateFunc, object config = null) where T : Component
        {
            var instance = await instantiateFunc();
            if (instance is MonoBehaviourScopeBase monoBehaviourScope)
            {
                monoBehaviourScope.ConstructScope(
                    ScopeId.Create(),
                    ScopeContainer,
                    config,
                    new MonoBehaviourScopeDefaultExtendScope(monoBehaviourScope, monoBehaviourScope)
                );
            }
            else
            {
                DynamicInjectOrInitializeChildrenRecursive(instance.transform);
            }
            return instance;
        }
        
        public MonoBehaviourScopeWithConfig<TConfig> InstantiateWithConfig<TConfig>(MonoBehaviourScopeWithConfig<TConfig> prefab, TConfig config, Transform parent) 
            where TConfig : class
        {
            var instance = UnityEngine.Object.Instantiate(prefab, parent);
            instance.ConstructScope(
                ScopeId.Create(), 
                ScopeContainer, 
                config,
                new MonoBehaviourScopeDefaultExtendScope(instance, instance)
                );
            return instance;
        }
        
        internal void ConstructScope(ScopeId scopeId, ScopeContainer parentScopeContainer, object config = null, IExtendScope optionExtendScope = null)
        {
            ScopeId = scopeId;
            ScopeContainer = new ScopeContainer(this, parentScopeContainer);

            var childBinder = new DependencyBinder(ScopeContainer);
            if (optionExtendScope != null)
            {
                childBinder.ExtendScope(optionExtendScope);
            }
            foreach (var extendScope in _extendScopes)
            {
                ScopeContainer.Inject(extendScope);
                childBinder.ExtendScope(extendScope);
            }

            ScopeContainer.Inject(this);
            IScope scope = this;
            scope.Construct(childBinder, config);

            var cancellationTokenOnDestroy = this.GetCancellationTokenOnDestroy();
            if (this is IAsyncInitializer asyncInitializer)
            {
                IScope parentScope = scope;
                IAsyncInitializer parentAsyncInitializer = null;
                ProjectScope.Initializers.Add(asyncInitializer);

                while (parentScope.ParentScope != ProjectScope.Scope.ParentScope)
                {
                    parentScope = parentScope.ParentScope;
                    if (parentScope is IAsyncInitializer parent)
                    {
                        parentAsyncInitializer = parent;
                        break;
                    }
                }

                this.StartAsync()
                    .ContinueWith(async () =>
                    {
                        if (parentAsyncInitializer != null && ProjectScope.Initializers.Any(x => x == parentAsyncInitializer))
                        {
                            await UniTask.WaitWhile(() => ProjectScope.Initializers.Any(x => x == parentAsyncInitializer), cancellationToken: cancellationTokenOnDestroy);
                        }
                        await asyncInitializer.InitializeAsync(cancellationTokenOnDestroy);
                        ProjectScope.Initializers.Remove(asyncInitializer);
                    })
                    .Forget();
            }

            InjectOrInitializeChildrenRecursive(this.gameObject.transform);
        }

        private void InjectOrInitializeChildrenRecursive(Transform current)
        {
#if USE_STATIC_DI_RESOLUTION
            for (int i = 0; i < _childInjectables.Count; i++)
            {
                var injectable = _childInjectables[i];
                if (injectable == null)
                {
                    continue;
                }
                if (injectable is MonoBehaviourScopeBase monoBehaviourScope)
                {
                    var scopeId = ScopeId.Create();
                    monoBehaviourScope.ConstructScope(scopeId: scopeId, parentScopeContainer: ScopeContainer);
                }
                else
                {
                    ScopeContainer.Inject(injectable);
                }
            }
#else
            DynamicInjectOrInitializeChildrenRecursive(current);
#endif
        }

        private void DynamicInjectOrInitializeChildrenRecursive(Transform current)
        {
            var injectables = current.GetComponents<IInjectable>();
            bool needToInjectChildren = true;
            for (int i = 0; i < injectables.Length; i++)
            {
                var injectable = injectables[i];
                if (injectable is MonoBehaviourScopeBase monoBehaviourScope && monoBehaviourScope != this)
                {
                    var scopeId = ScopeId.Create();
                    monoBehaviourScope.ConstructScope(scopeId: scopeId, parentScopeContainer: ScopeContainer);
                    needToInjectChildren = false;
                }
                else
                {
                    ScopeContainer.Inject(injectable);
                }
            }
            if (!needToInjectChildren)
            {
                return;
            }
            foreach (Transform child in current)
            {
                DynamicInjectOrInitializeChildrenRecursive(child);
            }
        }

        // ISceneLoader implementation -----
        void ISceneScopeLoader.LoadScene(Action loadSceneAction, object config = null)
        {
            ProjectScope.PushParentScope(this);
            ProjectScope.PushConfig(config);
            loadSceneAction();
        }

        async UniTask ISceneScopeLoader.LoadSceneAsync(Func<CancellationToken, UniTask> loadSceneFunc, CancellationToken cancellationToken, object config = null)
        {
            ProjectScope.PushParentScope(this);
            ProjectScope.PushConfig(config);
            await loadSceneFunc(cancellationToken);
        }

        async UniTask ISceneScopeLoader.LoadSceneAsync(string sceneName, LoadSceneMode loadSceneMode, CancellationToken cancellationToken, object config = null)
        {
            ProjectScope.PushParentScope(this);
            ProjectScope.PushConfig(config);
            await SceneManager.LoadSceneAsync(sceneName, loadSceneMode).ToUniTask(cancellationToken: cancellationToken);
        }

        void ISceneScopeLoader.LoadScene(string sceneName, LoadSceneMode loadSceneMode, object config = null)
        {
            ProjectScope.PushParentScope(this);
            ProjectScope.PushConfig(config);
            SceneManager.LoadScene(sceneName, loadSceneMode);
        }
        
#if USE_STATIC_DI_RESOLUTION && UNITY_EDITOR
        /// <summary>
        /// 同じGameObjectと子オブジェクトからIInjectableを実装したMonoBehaviourを収集する
        /// </summary>
        [ContextMenu("Collect Child Injectables")]
        public void CollectChildInjectables()
        {
            _childInjectables.Clear();

            // 1. 同じGameObjectの他のIInjectableコンポーネントを収集
            var sameGameObjectInjectables = gameObject.GetComponents<Component>()
                .Where(component => component is IInjectable && component != this)
                .ToList();
            _childInjectables.AddRange(sameGameObjectInjectables);

            // 2. 子オブジェクトから IInjectable を実装した MonoBehaviour を収集
            var childInjectables = CollectChildInjectablesRecursive(transform);
            _childInjectables.AddRange(childInjectables);

            Debug.Log($"Collected {_childInjectables.Count} injectables for {name} (Same GameObject: {sameGameObjectInjectables.Count}, Children: {childInjectables.Count})");

            // エディター時のみDirtyフラグを設定
            UnityEditor.EditorUtility.SetDirty(this);
        }
        
        private List<MonoBehaviour> CollectChildInjectablesRecursive(Transform current)
        {
            var result = new List<MonoBehaviour>();

            foreach (Transform child in current)
            {
                CollectInjectablesFromChildComponents(child);
            }

            return result;

            void CollectInjectablesFromChildComponents(Transform child)
            {
                // 現在の子オブジェクトの IInjectable を収集
                var injectables = child.GetComponents<MonoBehaviour>()
                    .Where(component => component is IInjectable && component != this)
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
        
        /// <summary>
        /// 全てのInjectableを削除
        /// </summary>
        public void ClearChildInjectables()
        {
            _childInjectables.Clear();
            UnityEditor.EditorUtility.SetDirty(this);
        }

        private void OnValidate()
        {
            if (Application.isPlaying)
            {
                return;
            }
            CollectChildInjectables();
        }
#endif
    }
}