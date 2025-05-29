using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using Cysharp.Threading.Tasks;
using Cysharp.Threading.Tasks.Triggers;
using NestedDIContainer.Unity.Runtime.Scopes;
using TanitakaTech.NestedDIContainer;
using UnityEngine;
using UnityEngine.SceneManagement;
using IInjectable = TanitakaTech.NestedDIContainer.IInjectable;

namespace NestedDIContainer.Unity.Runtime.Core
{
    [DefaultExecutionOrder(-5000)]
    public abstract class MonoBehaviourScopeBase : MonoBehaviour, IScope, IChildSceneScopeLoader, IPrefabScopeInstantiator
    {
        [SerializeField] protected List<ScriptableObjectExtendScope> _extendScopes;

        public ScopeId ScopeId { get; private set; }
        public IScope ParentScope => ScopeContainer.ParentScope;
        public ScopeContainer ScopeContainer { get; set; }
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
                InjectOrInitializeChildrenRecursive(instance.transform);
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
                InjectOrInitializeChildrenRecursive(instance.transform);
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
                InjectOrInitializeChildrenRecursive(child);
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
    }
}