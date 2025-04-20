using NestedDIContainer.Unity.Runtime.Core;
using NestedDIContainer.Unity.Runtime.Scopes;
using TanitakaTech.NestedDIContainer;

namespace NestedDIContainer.Unity.Runtime
{
    public abstract class SceneScope : SceneScopeWithConfig<SceneScope.EmptyConfig>
    {
        public abstract class EmptyConfig { }
        protected override void Construct(DependencyBinder binder, EmptyConfig config) => Construct(binder);
        protected abstract void Construct(DependencyBinder binder);
    }
    
    public abstract class SceneScopeWithConfig<TConfig> : MonoBehaviourScopeBase
    {
        protected void Awake()
        {
            // Init ScopeId
            ScopeId = ScopeId.Create();
            var parentScope = ProjectScope.PopParentId() ?? ProjectScope.Scope?.ScopeId ?? ProjectScope.CreateProjectScope().ScopeId;
            ParentScopeId = ScopeId.Equals(parentScope) 
                ? ScopeId.Create()
                : parentScope;

            ConstructScope(ScopeId, ScopeContainer, ProjectScope.PopConfig(), new SceneScopeDefaultExtendScope(this));
        }

        protected override void Construct(DependencyBinder binder, object config) => Construct(binder, (TConfig)config);
        protected abstract void Construct(DependencyBinder binder, TConfig config);
    }
}