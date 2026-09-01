using System;
using Cysharp.Threading.Tasks;

namespace Core.ServicesManager
{
    /// <summary>Lifecycle contract for services discovered and owned by <see cref="ServicesLocator"/>.</summary>
    /// <remarks>
    /// Locator creates each service, initializes declared dependencies first, then calls
    /// <see cref="Reset"/> in reverse order during teardown. Implementations own their runtime state
    /// and must cancel or dispose owned asynchronous work in <see cref="Reset"/>.
    /// </remarks>
    public interface IService
    {
        /// <summary>Initializes runtime state after declared dependencies are available.</summary>
        /// <returns>
        /// <see langword="true"/> when initialization succeeded; <see langword="false"/> stops
        /// locator initialization.
        /// </returns>
        UniTask<bool> Initialize();

        /// <summary>Gets concrete service types that must initialize before this service.</summary>
        /// <returns>Dependency types, an empty array, or <see langword="null"/> when none are required.</returns>
        Type[] GetDependencies();

        /// <summary>Releases service-owned runtime resources during locator teardown.</summary>
        UniTask Reset();
    }
}
