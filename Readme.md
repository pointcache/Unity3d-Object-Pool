
![License MIT](https://img.shields.io/badge/license-MIT-green.svg)
# Unity3d-Object-Pool

This is a simple ObjectPool (gameobject) for unity.

To incorporate into an existing project, create a GameObject in your hierarchy and place the ObjectPool component onto it.  You can then set up custom pools for your prefabs in the same manner as the Demo scene does with the DemoPrefab.

To use, in your code, replace calls to Instantiate(prefab) with prefab.InstantiateFromPool() and calls to Destroy(gameObject).  If you've set up a custom pool via inspector for that particular prefab, the pool will use your custom settings.  Otherwise, it sets up a runtime pool for the prefab the first time prefab.InstantiateFromPool() is called.

To use it just call extension method 
``` csharp
var go =  (yourgameobject or prefab reference).InstantiateFromPool(Vector3 position, Quaternion rotation);
```

The pool will be automatically created and manager object would be dropped in the scene.
If you need more manual setup create empty object in scene and add ObjectPool to it and study some options.

If you create a pool in scene before calling instantiation you have some control.

After you finished using the object, call 

``` csharp
(yourgameobject).Release();
```

and it will be placed back in pool.

Since pool has no way to remember initial state of your object, you have to reset it back to initial state in
OnEnable() 

TODO: make a separate callback for resetting object to initial state.

Example usage of this: bullets/enemies etc.
