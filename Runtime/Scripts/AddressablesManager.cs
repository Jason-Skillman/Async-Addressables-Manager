namespace JasonSkillman.AsyncAddressablesManager
{
	using System;
	using System.Collections.Generic;
	using System.Linq;
	using Cysharp.Threading.Tasks;
	using UnityEngine;
	using UnityEngine.AddressableAssets;
	using UnityEngine.ResourceManagement.AsyncOperations;
	using UnityEngine.ResourceManagement.ResourceProviders;
	using UnityEngine.SceneManagement;

	/// <summary>
	/// Scene loader utility class that helps load/unload multiple scenes asynchronously using Unity's Addressables system.
	/// </summary>
	public static class AddressablesManager
	{
		/// <summary>Stores the loaded scenes by the runtime key.</summary>
		private static Dictionary<int, AsyncOperationHandle<SceneInstance>> loadedScenes = new();	//Handle, AsyncOperationHandle<SceneInstance>
		
		public static void SetActiveScene(string activeScene)
		{
			Scene scene = UnityEngine.SceneManagement.SceneManager.GetSceneByName(activeScene);
			if(!scene.IsValid())
			{
				Debug.LogError($"[{nameof(AddressablesManager)}] Could not set active scene to an invalid scene: {activeScene}");
				return;
			}
			UnityEngine.SceneManagement.SceneManager.SetActiveScene(scene);
			Debug.Log($"[{nameof(AddressablesManager)}] Set active scene: {activeScene}");
		}

		#region LoadScenes

		/// <inheritdoc cref="LoadScenesAsync"/>
		public static void LoadScenes(string[] scenes, Action onFinish = null, string activeScene = null, bool recalculateLightProbes = true)
		{
			async UniTaskVoid Load()
			{
				await LoadScenesAsync(scenes, activeScene, recalculateLightProbes);
				onFinish?.Invoke();
			}

#pragma warning disable CS4014
			Load();
#pragma warning restore CS4014
		}
		
		/// <summary>
		/// Loads all scenes in <see cref="scenes"/> asynchronously using Unity Addressables.
		/// </summary>
		/// <param name="scenes">The list of scenes to load.</param>
		/// <param name="activeScene">The scene to be set as the active scene. This is optional.</param>
		/// <param name="recalculateLightProbes">Recalculate light probes if true. True by default.</param>
		/// <returns></returns>
		public static async UniTask<SceneInstance[]> LoadScenesAsync(string[] scenes, string activeScene = null, bool recalculateLightProbes = true)
		{
			if(scenes == null || scenes.Length <= 0)
				return Array.Empty<SceneInstance>();

			int sceneCount = scenes.Length;
			UniTask<SceneInstance>[] tasks = new UniTask<SceneInstance>[sceneCount];
        
			for(int i = 0; i < sceneCount; i++)
			{
				async UniTask<SceneInstance> Load(int sceneIndex)
				{
					AsyncOperationHandle<SceneInstance> asyncHandle = Addressables.LoadSceneAsync(scenes[sceneIndex], LoadSceneMode.Additive);
					SceneInstance sceneInstance = await asyncHandle.ToUniTask();
					int handle = sceneInstance.Scene.handle;
					
					//Cache all scenes loaded through Addressables. This is so the scene can be unloaded with Addressables.
					loadedScenes.Add(handle, asyncHandle);
					
					return sceneInstance;
				}
				
				tasks[i] = Load(i);
			}

			SceneInstance[] sceneInstances = await UniTask.WhenAll(tasks);

			if(!string.IsNullOrEmpty(activeScene))
				SetActiveScene(activeScene);

			if(recalculateLightProbes)
				LightProbes.TetrahedralizeAsync();
			
			Debug.Log($"[{nameof(AddressablesManager)}] Scenes loaded: {string.Join(", ", scenes)}");

			return sceneInstances;
		}

		#endregion

		#region UnloadScenes

		/// <inheritdoc cref="UnloadScenesAsync"/>
		public static void UnloadScenes(string[] scenes, Action onFinish = null, bool recalculateLightProbes = true)
		{
			async UniTaskVoid Unload()
			{
				await UnloadScenesAsync(scenes, recalculateLightProbes);
				onFinish?.Invoke();
			}

#pragma warning disable CS4014 // Because this call is not awaited, execution of the current method continues before the call is completed
			Unload();
#pragma warning restore CS4014 // Because this call is not awaited, execution of the current method continues before the call is completed
		}
		
		/// <summary>
		/// Unloads all scenes in <see cref="scenes"/> asynchronously using Unity Addressables.
		/// </summary>
		/// <param name="scenes">The list of scenes to unload.</param>
		/// <param name="recalculateLightProbes">Recalculate light probes if true. True by default.</param>
		public static async UniTask<SceneInstance[]> UnloadScenesAsync(string[] scenes, bool recalculateLightProbes = true)
		{
			if(scenes == null || scenes.Length <= 0)
				return Array.Empty<SceneInstance>();
			
			bool hasDuplicates = scenes.GroupBy(x => x).Any(g => g.Count() > 1);
			if(hasDuplicates)
			{
				Debug.LogError($"[{nameof(AddressablesManager)}] Unloading multiple scenes with the same name is not supported.");
				return Array.Empty<SceneInstance>();
			}
			
			int sceneCount = scenes.Length;
			UniTask<SceneInstance>[] tasks = new UniTask<SceneInstance>[sceneCount];
        
			for(int i = 0; i < sceneCount; i++)
			{
				async UniTask<SceneInstance> Unload(int sceneIndex)
				{
					string sceneName = scenes[sceneIndex];
					
					//Find the first AsyncOperationHandle with a matching scene name. This might occur if multiple scenes were loaded with the same name.
					AsyncOperationHandle<SceneInstance> asyncHandle = loadedScenes.Values.FirstOrDefault(s => s.Result.Scene.name == sceneName);

					if(!asyncHandle.IsValid())
					{
						Debug.LogError($"[{nameof(AddressablesManager)}] Can't unload scene because it is not loaded, or was not loaded by addressables: {sceneName}");
						return new SceneInstance();
					}
					
					SceneInstance sceneInstance = await Addressables.UnloadSceneAsync(asyncHandle).ToUniTask();
					int handle = sceneInstance.Scene.handle;

					//Remove the scene from the loaded scenes cache
					loadedScenes.Remove(handle);

					return sceneInstance;
				}

				tasks[i] = Unload(i);
			}
        
			SceneInstance[] sceneInstances = await UniTask.WhenAll(tasks);

			if(recalculateLightProbes)
				LightProbes.TetrahedralizeAsync();
			
			Debug.Log($"[{nameof(AddressablesManager)}] Scenes unloaded: {string.Join(", ", scenes)}");

			return sceneInstances;
		}
		
		/// <inheritdoc cref="UnloadAllScenesExceptForAsync(string[],System.Action,bool)"/>
		public static void UnloadAllScenesExceptFor(string[] scenes, Action onFinish = null, bool recalculateLightProbes = true)
		{
			async UniTaskVoid UnloadAll()
			{
				await UnloadAllScenesExceptForAsync(scenes, recalculateLightProbes);
				onFinish?.Invoke();
			}

#pragma warning disable CS4014 // Because this call is not awaited, execution of the current method continues before the call is completed
			UnloadAll();
#pragma warning restore CS4014 // Because this call is not awaited, execution of the current method continues before the call is completed
		}
		
		/// <summary>
		/// Unloads all <see cref="scenes"/> asynchronously except for <see cref="scenesToKeep"/> using Unity Addressables.
		/// </summary>
		/// <param name="scenes">The list of scenes to not unload.</param>
		/// <param name="recalculateLightProbes">Recalculate light probes if true. True by default.</param>
		public static async UniTask UnloadAllScenesExceptForAsync(string[] scenesToKeep, bool recalculateLightProbes = true)
		{
			int sceneCount = UnityEngine.SceneManagement.SceneManager.sceneCount;
			
			if(sceneCount <= 1)
			{
				Debug.LogError($"[{nameof(AddressablesManager)}] Can't unload the only scene left.");
				return;
			}
			
			Scene activeScene = UnityEngine.SceneManagement.SceneManager.GetActiveScene();

			//Create a list of every scene to unload.
			List<string> scenesToRemove = new();
			for(int i = 0; i < sceneCount; i++)
			{
				Scene scene = UnityEngine.SceneManagement.SceneManager.GetSceneAt(i);
				string sceneName = scene.name;
				
				bool continueFlag = false;

				//Check if the scene matches any in scenes to keep and if so skip
				for(int j = 0; j < scenesToKeep.Length; j++)
				{
					if(sceneName == scenesToKeep[j])
					{
						continueFlag = true;
						break;
					}
				}
				if(continueFlag) continue;
				
				if(sceneName == activeScene.name)
				{
					Debug.LogError($"[{nameof(AddressablesManager)}] You can't unload the active scene. Active scene: {activeScene}");
					continue;
				}
				
				scenesToRemove.Add(sceneName);
			}
			
			await UnloadScenesAsync(scenesToRemove.ToArray(), recalculateLightProbes);
		}

		#endregion

		#region LoadScenesBatch

		/// <inheritdoc cref="LoadScenesBatchAsync"/>
		public static void LoadScenesBatch(SceneBatch sceneBatch, Action onFinish = null, bool recalculateLightProbes = true)
		{
			async UniTaskVoid Batch()
			{
				await LoadScenesBatchAsync(sceneBatch, recalculateLightProbes);
				onFinish?.Invoke();
			}

#pragma warning disable CS4014 // Because this call is not awaited, execution of the current method continues before the call is completed
			Batch();
#pragma warning restore CS4014 // Because this call is not awaited, execution of the current method continues before the call is completed
		}

		/// <summary>
		/// Loads scenes, unloads scenes, and sets the active scene in one single batch.
		/// </summary>
		/// <param name="sceneBatch">The batch file containing all the scenes.</param>
		/// <param name="recalculateLightProbes">Recalculate light probes if true. True by default.</param>
		public static async UniTask LoadScenesBatchAsync(SceneBatch sceneBatch, bool recalculateLightProbes = true)
		{
			UniTask<SceneInstance[]> unloadTask = UnloadScenesAsync(sceneBatch.scenesToUnload, false);
			UniTask<SceneInstance[]> loadTask = LoadScenesAsync(sceneBatch.scenesToLoad, sceneBatch.activeScene, false);

			await UniTask.WhenAll(unloadTask, loadTask);
			
			if(recalculateLightProbes)
				LightProbes.TetrahedralizeAsync();
		}

		#endregion
	}
}
