using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using Cysharp.Threading.Tasks;
using UnityEngine.AddressableAssets;
using UnityEngine.ResourceManagement.AsyncOperations;
using Object = UnityEngine.Object;

namespace JasonSkillman.AsyncAddressablesManager
{
	/// <summary>
	/// Scene loader utility class that helps load/unload multiple scenes asynchronously using Unity's Addressables system.
	/// </summary>
	public static partial class AddressablesManager
	{
		public struct AssetRefCount
		{
			public Object asset;
			public uint referenceCount;

#if UNITY_EDITOR
			public string assetName;
#endif
			
			public bool HasReferences => referenceCount > 0;
		}

		private static readonly Dictionary<object, AssetRefCount> loadedAssets = new Dictionary<object, AssetRefCount>();

		public static Dictionary<object, AssetRefCount> LoadedAssets => loadedAssets;

		#region Load
		
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static async UniTask<LoadedContextKey<T>> LoadAssetAsync<T>(AssetReferenceT<T> assetReference) where T : Object
		{
			LoadedContextKey<T> context = await LoadAssetByKeyAsync<T>(assetReference.RuntimeKey);
		
#if UNITY_EDITOR
			if (loadedAssets.TryGetValue(assetReference.RuntimeKey, out AssetRefCount assetRefCount))
			{
				assetRefCount.assetName = assetReference.editorAsset.name;
				loadedAssets[assetReference.RuntimeKey] = assetRefCount;
			}
#endif
			
			return context;
		}

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static async UniTask<LoadedContextKey<T>> LoadAssetByKeyAsync<T>(string key) where T : Object
		{
			return await LoadAssetByKeyAsync<T>((object)key);
		}

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static async UniTask<LoadedContextKey<T>> LoadAssetByKeyAsync<T>(object key) where T : Object
		{
			T asset = await LoadAssetByKeyInternalAsync<T>(key);
			return new LoadedContextKey<T>(key, asset);
		}

		private static async UniTask<T> LoadAssetByKeyInternalAsync<T>(object key) where T : Object
		{
			// Check if the asset is already loaded.
			if (loadedAssets.TryGetValue(key, out AssetRefCount assetRefCount))
			{
				if (assetRefCount.HasReferences)
				{
					// Asset is already loaded.
					assetRefCount.referenceCount++;
					loadedAssets[key] = assetRefCount;
					
					return (T)assetRefCount.asset;
				}
			}
			// Else asset is not loaded yet so load it.

			AsyncOperationHandle<T> handle = Addressables.LoadAssetAsync<T>(key);
			await handle.ToUniTask();

			if (!handle.IsDone)
			{
				throw new Exception("Failed to load asset");
			}

			T result = handle.Result;

			// Add to loaded
			assetRefCount.asset = result;
			assetRefCount.referenceCount++;
			loadedAssets[key] = assetRefCount;

			return result;
		}

		#endregion

		#region Unload

		public static UnloadResult UnloadAsset<T>(ref LoadedContextKey<T> loadedAssetContext) where T : Object
		{
			if (!loadedAssetContext.IsValid)
			{
				return UnloadResult.Invalid;
			}

			object key = loadedAssetContext.Key;

			UnloadResult result = UnloadByKeyAsset(key);
			
			if (result == UnloadResult.ReturnedAndReleased)
			{
				loadedAssetContext.Clear();
			}
			
			return result;
		}

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static UnloadResult UnloadAsset<T>(AssetReferenceT<T> assetReference) where T : Object
		{
			return UnloadByKeyAsset(assetReference.RuntimeKey);
		}

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static UnloadResult UnloadByKeyAsset(string key)
		{
			return UnloadByKeyAsset((object)key);
		}
		
		public static UnloadResult UnloadByKeyAsset(object key)
		{
			// Check if the asset is already unloaded.
			loadedAssets.TryGetValue(key, out AssetRefCount container);
			
			if (!container.HasReferences)
			{
				// Asset is already unloaded.
				return UnloadResult.AlreadyUnloaded;
			}

			container.referenceCount--;

			UnloadResult result;

			// Check if this was the last reference. 
			if (!container.HasReferences)
			{
				Addressables.Release(container.asset);
				
				container.asset = null;

				result = UnloadResult.ReturnedAndReleased;
			}
			else
			{
				result = UnloadResult.Returned;
			}

			loadedAssets[key] = container;
			
			return result;
		}
		
		#endregion
	}
}
