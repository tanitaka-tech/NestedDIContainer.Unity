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
        void LoadScene(string sceneName, LoadSceneMode loadSceneMode, object config = null);

        void LoadScene(Action loadSceneAction, object config = null);

        /// <summary>
        /// Load and inject config a Scene.
        /// </summary>
        /// <remarks>
        /// Note: Calling this method in parallel may result in improper injection of the Configuration.
        /// </remarks>
        UniTask LoadSceneAsync(string sceneName, LoadSceneMode loadSceneMode, CancellationToken cancellationToken, object config = null);
        UniTask LoadSceneAsync(Func<CancellationToken, UniTask> loadSceneFunc, CancellationToken cancellationToken, object config = null);
    }

    public interface IChildSceneScopeLoader : ISceneScopeLoader {}
}