interface IPoolInit 
{
	/// This method runs after the object is retrieved from the pool and should be used to re-initialize it
	void InitFromPool();

	/// This method prepares object for deactivation before it is released back into the pool
	void DeactivateBeforeRelease();
}
