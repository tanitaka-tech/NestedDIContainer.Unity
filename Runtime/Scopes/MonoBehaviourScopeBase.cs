using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Threading;
using Cysharp.Threading.Tasks;
using Cysharp.Threading.Tasks.Triggers;
using TanitakaTech.NestedDIContainer;
using UnityEngine;
using UnityEngine.SceneManagement;
using IInjectable = TanitakaTech.NestedDIContainer.IInjectable;

namespace NestedDIContainer.Unity.Runtime.Core
{
    [DefaultExecutionOrder(-5000)]
    public abstract class MonoBehaviourScopeBase : MonoBehaviour, IScope, IChildSceneScopeLoader
    {
        [SerializeField] protected List<ScriptableObjectExtendScope> _extendScopes;

        public ScopeId ScopeId { get; set; }
        public ScopeId? ParentScopeId { get; set; }

        void IScope.Construct(DependencyBinder binder, object config)
        {
            Construct(binder, config);
        }
        protected abstract void Construct(DependencyBinder binder, object config);

        public T Instantiate<T>(T prefab, Transform parent, object config = null) where T : MonoBehaviourScopeBase
        {
            var instance = UnityEngine.Object.Instantiate(prefab, parent);
            instance.ConstructScope(ScopeId.Create(), ScopeId, config);
            return instance;
        }
        
        public MonoBehaviourScopeWithConfig<TConfig> InstantiateWithConfig<TConfig>(MonoBehaviourScopeWithConfig<TConfig> prefab, TConfig config, Transform parent) 
            where TConfig : class
        {
            var instance = UnityEngine.Object.Instantiate(prefab, parent);
            instance.ConstructScope(ScopeId.Create(), ScopeId, config);
            return instance;
        }
        
        internal void ConstructScope(ScopeId scopeId, ScopeId parentScopeId, object config = null, IExtendScope optionExtendScope = null)
        {
            ScopeId = scopeId;
            ParentScopeId = parentScopeId;
            GlobalProjectScope.Scopes.Add(scopeId, this);

            var childBinder = new DependencyBinder(scopeId, GlobalProjectScope.Scopes, GlobalProjectScope.ScopeContainer);
            if (optionExtendScope != null)
            {
                childBinder.ExtendScope(optionExtendScope);
            }
            foreach (var extendScope in _extendScopes)
            {
                GlobalProjectScope.Inject(extendScope, this);
                childBinder.ExtendScope(extendScope);
            }

            GlobalProjectScope.Inject(this, this);
            IScope scope = this;
            scope.Construct(childBinder, config);

            var cancellationTokenOnDestroy = this.GetCancellationTokenOnDestroy();
            if (this is IAsyncInitializer asyncInitializer)
            {
                var parentScope = scope;
                IAsyncInitializer parentAsyncInitializer = null;
                ProjectScope.Initializers.Add(asyncInitializer);

                while (!parentScope.ParentScopeId.Equals(ProjectScope.Scope.ParentScopeId))
                {
                    GlobalProjectScope.Scopes.TryGetValue(parentScope.ParentScopeId.Value, out parentScope);
                    if (parentScope is IAsyncInitializer parent)
                    {
                        parentAsyncInitializer = parent;
                        break;
                    }
                }

                this.StartAsync()
                    .ContinueWith(async () =>
                    {
                        if (parentAsyncInitializer != null)
                        {
                            await UniTask.WaitWhile(() => ProjectScope.Initializers.Any(x => x == parentAsyncInitializer), cancellationToken: cancellationTokenOnDestroy);
                        }
                        await asyncInitializer.InitializeAsync(cancellationTokenOnDestroy);
                        ProjectScope.Initializers.Remove(asyncInitializer);
                    })
                    .Forget();
            }

            cancellationTokenOnDestroy.Register(() =>
            {
                GlobalProjectScope.Scopes.Remove(scopeId);
            });

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
                    monoBehaviourScope.ConstructScope(scopeId: scopeId, parentScopeId: ScopeId);
                    needToInjectChildren = false;
                }
                else
                {
                    GlobalProjectScope.Inject(injectable, this);
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
        void ISceneScopeLoader.LoadScene<TConfig>(Action loadSceneAction, TConfig config = null) where TConfig : class
        {
            ProjectScope.PushParentId(ScopeId);
            ProjectScope.PushConfig(config);
            loadSceneAction();
        }

        async UniTask ISceneScopeLoader.LoadSceneAsync<TConfig>(Func<CancellationToken, UniTask> loadSceneFunc, CancellationToken cancellationToken, TConfig config = null) where TConfig : class
        {
            ProjectScope.PushParentId(ScopeId);
            ProjectScope.PushConfig(config);
            await loadSceneFunc(cancellationToken);
        }

        async UniTask ISceneScopeLoader.LoadSceneAsync<TConfig>(string sceneName, LoadSceneMode loadSceneMode, CancellationToken cancellationToken, TConfig config = null) where TConfig : class
        {
            ProjectScope.PushParentId(ScopeId);
            ProjectScope.PushConfig(config);
            await SceneManager.LoadSceneAsync(sceneName, loadSceneMode).ToUniTask(cancellationToken: cancellationToken);
        }

        void ISceneScopeLoader.LoadScene<TConfig>(string sceneName, LoadSceneMode loadSceneMode, TConfig config = null) where TConfig : class
        {
            ProjectScope.PushParentId(ScopeId);
            ProjectScope.PushConfig(config);
            SceneManager.LoadScene(sceneName, loadSceneMode);
        }
    }
}