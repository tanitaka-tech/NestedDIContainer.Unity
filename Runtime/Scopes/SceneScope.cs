using TanitakaTech.NestedDIContainer;
using TanitakaTech.NestedDIContainer.Unity.Runtime.Core;
using TanitakaTech.NestedDIContainer.Unity.Runtime.Scopes;

namespace TanitakaTech.NestedDIContainer.Unity.Runtime
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
            // Initialize ScopeId
            // NOTE: シーンの読み込みが同時に走っても取り違えないよう、このシーンが期待する config の型で取り出す
            ProjectScope.TryPopPendingSceneScope(typeof(TConfig), out var pendingParentScope, out var config);
            var parentScope = pendingParentScope ?? ProjectScope.Scope ?? ProjectScope.CreateProjectScope();
            ConstructScope(ScopeId.Create(), parentScope.ScopeContainer, config, new SceneScopeDefaultExtendScope(this, this));
        }

        protected override void Construct(DependencyBinder binder, object config) => Construct(binder, (TConfig)config);
        protected abstract void Construct(DependencyBinder binder, TConfig config);
    }
}