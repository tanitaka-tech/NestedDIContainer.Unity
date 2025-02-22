using NestedDIContainer.Unity.Runtime.Core;
using TanitakaTech.NestedDIContainer;

namespace NestedDIContainer.Unity.Runtime
{
    public abstract class MonoBehaviourScope : MonoBehaviourScopeWithConfig<MonoBehaviourScope.EmptyConfig>
    {
        public abstract class EmptyConfig { }
        protected override void Construct(DependencyBinder binder, EmptyConfig config) => Construct(binder);
        protected abstract void Construct(DependencyBinder binder);
    }
    
    public abstract class MonoBehaviourScopeWithConfig<TConfig> : MonoBehaviourScopeBase
    {
        protected override void Construct(DependencyBinder binder, object config) => Construct(binder, config is TConfig c ? c : default);
        protected abstract void Construct(DependencyBinder binder, TConfig config);
    }
}