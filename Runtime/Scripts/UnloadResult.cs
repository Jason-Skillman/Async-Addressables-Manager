namespace JasonSkillman.AsyncAddressablesManager
{
	public enum UnloadResult : byte
	{
		/// <summary>
		/// The asset is invalid.
		/// </summary>
		Invalid,
		
		/// <summary>
		/// The asset is already unloaded.
		/// </summary>
		AlreadyUnloaded,
		
		/// <summary>
		/// The asset was returned and the asset still has a reference count keeping it loaded.
		/// </summary>
		Returned,
		
		/// <summary>
		/// The asset was returned and released from memory.
		/// </summary>
		ReturnedAndReleased
	}
}
