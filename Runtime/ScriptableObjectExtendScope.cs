using TanitakaTech.NestedDIContainer;
using UnityEngine;

namespace TanitakaTech.NestedDIContainer.Unity.Runtime
{
    public abstract class ScriptableObjectExtendScope : ScriptableObject, IExtendScope
    {
        void IExtendScope.Construct(DependencyBinder binder) => Construct(binder);
        protected abstract void Construct(DependencyBinder binder);
    }
}