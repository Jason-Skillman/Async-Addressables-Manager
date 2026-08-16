using UnityEngine;

namespace JasonSkillman.AsyncAddressablesManager
{
	public struct LoadedContextKey<TAsset> where TAsset : Object
	{
		private object key;
		private TAsset asset;
		private bool isValid;

		public object Key => key;
		public TAsset Asset => asset;
		public bool IsValid => isValid;

		public LoadedContextKey(object key, TAsset asset)
		{
			this.key = key;
			this.asset = asset;
			isValid = true;
		}
		
		public LoadedContextKey(object key, Object asset)
		{
			this.key = key;
			this.asset = (TAsset)asset;
			isValid = true;
		}
		
		public LoadedContextKey(string key, TAsset asset)
		{
			this.key = key;
			this.asset = asset;
			isValid = true;
		}
		
		public LoadedContextKey(string key, Object asset)
		{
			this.key = key;
			this.asset = (TAsset)asset;
			isValid = true;
		}

		public void Clear()
		{
			isValid = false;
			asset = null;
			key = null;
		}
	}
}
