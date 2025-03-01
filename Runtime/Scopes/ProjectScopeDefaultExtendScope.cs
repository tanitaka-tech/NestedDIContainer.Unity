using TanitakaTech.NestedDIContainer;

namespace NestedDIContainer.Unity.Runtime.Scopes
{
    public class ProjectScopeDefaultExtendScope : IExtendScope
    {
        private ISceneScopeLoader SceneScopeLoader { get; }

        public ProjectScopeDefaultExtendScope(ISceneScopeLoader sceneScopeLoader)
        {
            SceneScopeLoader = sceneScopeLoader;
        }

        void IExtendScope.Construct(DependencyBinder binder)
        {
            binder.Bind<ISceneScopeLoader>(SceneScopeLoader);
        }
    }
}