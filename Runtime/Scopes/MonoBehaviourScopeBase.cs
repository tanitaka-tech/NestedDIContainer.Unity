using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Cysharp.Threading.Tasks;
using Cysharp.Threading.Tasks.Triggers;
using TanitakaTech.NestedDIContainer;
using UnityEngine;
using IInjectable = TanitakaTech.NestedDIContainer.IInjectable;

namespace NestedDIContainer.Unity.Runtime.Core
{
    [DefaultExecutionOrder(-5000)]
    public abstract class MonoBehaviourScopeBase : MonoBehaviour, IScope, IInjectable
    {
        [SerializeField] protected List<ScriptableObjectExtendScope> _extendScopes;

        public ScopeId ScopeId { get; set; }
        public ScopeId? ParentScopeId { get; set; }

        private const BindingFlags MemberBindingFlags =
            BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.DeclaredOnly;

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

            var childBinder = new DependencyBinder(scopeId);
            if (optionExtendScope != null)
            {
                childBinder.ExtendScope(optionExtendScope);
            }
            foreach (var extendScope in _extendScopes)
            {
                childBinder.ExtendScope(extendScope);
            }

            GlobalProjectScope.Scopes.Add(scopeId, this);
            Inject(this, this);
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
                GlobalProjectScope.Modules.RemoveScope(scopeId);
            });

            InjectOrInitializeChildren(this.gameObject);
        }

        private void Inject(object injectableObject, IScope scope)
        {
            var type = injectableObject.GetType();
            var fields = type.GetFields(MemberBindingFlags);
            foreach (var field in fields)
            {
                var injectAttr = field.GetCustomAttribute<InjectAttribute>();
                if (injectAttr != null)
                {
                    field.SetValue(injectableObject, GlobalProjectScope.Modules.Resolve(field.FieldType, scope));
                }
            }
            var props = type.GetProperties(MemberBindingFlags);
            foreach (var prop in props)
            {
                var injectAttr = prop.GetCustomAttribute<InjectAttribute>();
                if (injectAttr != null)
                {
                    prop.SetValue(injectableObject, GlobalProjectScope.Modules.Resolve(prop.PropertyType, scope));
                }
            }
        }

        private void InjectOrInitializeChildren(GameObject parent)
        {
            foreach (Transform child in parent.transform)
            {
                InjectOrInitializeChildrenRecursive(child);
            }
            return;

            void InjectOrInitializeChildrenRecursive(Transform current)
            {
                var injectable = current.GetComponent<IInjectable>();
                if (injectable != null)
                {
                    var scopeId = ScopeId.Create();
                    if (injectable is MonoBehaviourScopeBase monoBehaviourScope)
                    {
                        monoBehaviourScope.ConstructScope(scopeId: scopeId, parentScopeId: ScopeId);
                        return;
                    }
                    else
                    {
                        Inject(injectableObject: injectable, scope: this);
                    }
                }

                foreach (Transform child in current)
                {
                    InjectOrInitializeChildrenRecursive(child);
                }
            }
        }
    }
}