using System.Threading;
using UnityEngine;

namespace TanitakaTech.NestedDIContainer.Unity.Runtime
{
    public abstract class ScriptableObjectExtendScope : ScriptableObject, IExtendScope
    {
        void IExtendScope.Construct(DependencyBinder binder, CancellationToken scopeLifetime) => Construct(binder, scopeLifetime);
        protected abstract void Construct(DependencyBinder binder, CancellationToken scopeLifetime);
    }
}