using System.Threading;
using TanitakaTech.NestedDIContainer;

namespace TanitakaTech.NestedDIContainer.Unity.Runtime.Scopes
{
    public class ProjectScopeDefaultExtendScope : IExtendScope
    {
        private readonly ISceneScopeLoader _sceneScopeLoader;

        public ProjectScopeDefaultExtendScope(ISceneScopeLoader sceneScopeLoader)
        {
            _sceneScopeLoader = sceneScopeLoader;
        }

        void IExtendScope.Construct(DependencyBinder binder, CancellationToken scopeLifetime)
        {
            binder.Bind<ISceneScopeLoader>(_sceneScopeLoader);
        }
    }
}