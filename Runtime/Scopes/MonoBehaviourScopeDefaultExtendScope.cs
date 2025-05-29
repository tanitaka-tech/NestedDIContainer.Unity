using TanitakaTech.NestedDIContainer;

namespace NestedDIContainer.Unity.Runtime.Scopes
{
    public class MonoBehaviourScopeDefaultExtendScope : IExtendScope
    {
        private readonly IChildSceneScopeLoader _childSceneScopeLoader;
        private readonly IPrefabScopeInstantiator _prefabScopeInstantiator;

        public MonoBehaviourScopeDefaultExtendScope(IChildSceneScopeLoader childSceneScopeLoader, IPrefabScopeInstantiator prefabScopeInstantiator)
        {
            _childSceneScopeLoader = childSceneScopeLoader;
            _prefabScopeInstantiator = prefabScopeInstantiator;
        }

        void IExtendScope.Construct(DependencyBinder binder)
        {
            binder.Bind<IChildSceneScopeLoader>(_childSceneScopeLoader);
            binder.Bind<IPrefabScopeInstantiator>(_prefabScopeInstantiator);
        }
    }
}