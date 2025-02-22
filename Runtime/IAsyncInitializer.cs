using System.Threading;
using Cysharp.Threading.Tasks;

namespace NestedDIContainer.Unity.Runtime
{
    public interface IAsyncInitializer
    {
        UniTask InitializeAsync(CancellationToken cancellationToken);
    }
}