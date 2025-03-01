using TanitakaTech.NestedDIContainer;

namespace NestedDIContainer.Unity.Runtime.Scopes
{
    public class SceneScopeDefaultExtendScope : IExtendScope
    {
        private IChildSceneScopeLoader ChildSceneScopeLoader { get; }

        public SceneScopeDefaultExtendScope(IChildSceneScopeLoader childSceneScopeLoader)
        {
            ChildSceneScopeLoader = childSceneScopeLoader;
        }

        void IExtendScope.Construct(DependencyBinder binder)
        {
            binder.Bind<IChildSceneScopeLoader>(ChildSceneScopeLoader);
        }
    }
}