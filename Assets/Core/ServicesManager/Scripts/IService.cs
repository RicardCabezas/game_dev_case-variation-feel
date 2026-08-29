using System;
using Cysharp.Threading.Tasks;

namespace Core.ServicesManager
{
	public interface IService
	{
		UniTask<bool> Initialize();
		Type[] GetDependencies();
		UniTask Reset();
	}
}