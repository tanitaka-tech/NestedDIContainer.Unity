using System;
using System.Threading;
using Cysharp.Threading.Tasks;
using UnityEngine.SceneManagement;

namespace NestedDIContainer.Unity.Runtime
{
    public interface ISceneScopeLoader
    {
        /// <summary>
        /// Load and inject config a Scene.
        /// </summary>
        public void LoadScene<TConfig>(string sceneName, LoadSceneMode loadSceneMode, TConfig config = null) where TConfig : class;
        void LoadScene<TConfig>(Action loadSceneAction, TConfig config = null) where TConfig : class;

        /// <summary>
        /// Load and inject config a Scene.
        /// </summary>
        /// <remarks>
        /// Note: Calling this method in parallel may result in improper injection of the Configuration.
        /// </remarks>
        UniTask LoadSceneAsync<TConfig>(string sceneName, LoadSceneMode loadSceneMode, CancellationToken cancellationToken, TConfig config = null) where TConfig : class;
        public UniTask LoadSceneAsync<TConfig>(Func<CancellationToken, UniTask> loadSceneFunc, CancellationToken cancellationToken, TConfig config = null) where TConfig : class;
    }

    public interface IChildSceneScopeLoader : ISceneScopeLoader {}
}