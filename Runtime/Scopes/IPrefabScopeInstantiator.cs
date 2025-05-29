using System;
using Cysharp.Threading.Tasks;
using UnityEngine;

namespace NestedDIContainer.Unity.Runtime
{
    public interface IPrefabScopeInstantiator
    {
        public T Instantiate<T>(T prefab, Transform parent, object config = null, Func<T, Transform, T> instantiateFunc = null) where T : Component;
        public UniTask<T> InstantiateAsync<T>(Func<UniTask<T>> instantiateFunc, object config = null) where T : Component;
        public MonoBehaviourScopeWithConfig<TConfig> InstantiateWithConfig<TConfig>(MonoBehaviourScopeWithConfig<TConfig> prefab, TConfig config, Transform parent) where TConfig : class;
    }
}