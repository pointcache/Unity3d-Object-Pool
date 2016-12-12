The Demo branch has a basic Unity scene set up showing how to use custom pools.

To incorporate into an existing project, create a GameObject in your hierarchy and place the ObjectPool component onto it.  You can then set up custom pools for your prefabs in the same manner as the Demo scene does with the DemoPrefab.

To use, in your code, replace calls to Instantiate(prefab) with prefab.InstantiateFromPool() and calls to Destroy(gameObject).  If you've set up a custom pool via inspector for that particular prefab, the pool will use your custom settings.  Otherwise, it sets up a runtime pool for the prefab the first time prefab.InstantiateFromPool() is called.