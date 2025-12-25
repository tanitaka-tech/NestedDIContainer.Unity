using System.Threading;
using TanitakaTech.NestedDIContainer;

namespace TanitakaTech.NestedDIContainer.Unity.Runtime.Scopes
{
    public class SceneScopeDefaultExtendScope : IExtendScope
    {
        private readonly IChildSceneScopeLoader _childSceneScopeLoader;
        private readonly IPrefabScopeInstantiator _prefabScopeInstantiator;

        public SceneScopeDefaultExtendScope(IChildSceneScopeLoader childSceneScopeLoader, IPrefabScopeInstantiator prefabScopeInstantiator)
        {
            _childSceneScopeLoader = childSceneScopeLoader;
            _prefabScopeInstantiator = prefabScopeInstantiator;
        }

        void IExtendScope.Construct(DependencyBinder binder, CancellationToken scopeLifetime)
        {
            binder.Bind<IChildSceneScopeLoader>(_childSceneScopeLoader);
            binder.Bind<IPrefabScopeInstantiator>(_prefabScopeInstantiator);
        }
    }
}