using UnityEngine;
using System;
using System.Collections;
using System.Collections.Generic;

//The only methods for accessing pools from outside this script
public static class ObjectPoolExtensions
{
	public static GameObject InstantiateFromPool(this GameObject prefab, Vector3 position, Quaternion rotation)
	{
		return ObjectPool.Instantiate(prefab, position, rotation);
	}

	public static T InstantiateFromPool<T>(this GameObject prefab, Vector3 position, Quaternion rotation)
		where T : class
	{
		return ObjectPool.Instantiate(prefab, position, rotation) as T;
	}

	public static void Release(this GameObject objthis)
	{
		ObjectPool.Release(objthis);
	}

	public static void ReleaseDelayed(this GameObject objthis, float delayTime)
	{
		ObjectPool.DelayedRelease(objthis, delayTime);
	}
}

public class ObjectPool : MonoBehaviour
{
	private static ObjectPool _instance;
	public static ObjectPool instance
	{
		//Singleton management
		get
		{
			if (_instance == null)
			{
				_instance = FindObjectOfType<ObjectPool>();
				if (_instance == null)
				{
					GameObject go = new GameObject("ObjectPool");
					_instance = go.AddComponent<ObjectPool>();
					
				}
			}
			return _instance;
		}
	}

	//Custom pools you can fill via Inspector on a per-prefab basis, adds more features to the pool
	public List<Pool> customPools = new List<Pool>();

	public List<Pool> runtimePools = new List<Pool>();//Read only, just displays pools created on runtime without prior setup."

	//Ties pools to related object
	Dictionary<GameObject, Pool> pool = new Dictionary<GameObject, Pool>();

	// Create from pool
	public static GameObject Instantiate(GameObject prefab, Vector3 position, Quaternion rotation)
	{
		return instance._Instantiate(prefab, position, rotation);
	}

	GameObject _Instantiate(GameObject prefab, Vector3 position, Quaternion rotation)
	{
		//if our pools already exist for this specific prefab, use it
		if (pool.ContainsKey(prefab))
		{
			Pool current = pool[prefab];
			return current.Request(position, rotation);
		}
		else //Create a new pool for this prefab, then use it
		{
			Pool newpool = new Pool();
			runtimePools.Add(newpool);
			pool.Add(prefab, newpool);
			newpool.MaxObjectsWarning = 100;
			newpool.prefab = prefab;
			return newpool.Request(position, rotation);
		}

	}

	void Awake()
	{
		for (int i = 0; i < customPools.Count; i++)
		{
			if (customPools[i].prefab == null)
			{
				Debug.LogError("Exists custom object pool without object, clean it up.");
				continue;
			}

			if (!pool.ContainsKey(customPools[i].prefab))
			{
				pool.Add(customPools[i].prefab, customPools[i]);
			}
			else
			{
				Debug.LogError("Trying to add object that is already registered by object pooler.");
			}
		}
	}

	void Start()
	{
		//Preload each pool with specified number of deactivated instances of prefab
		for (int i = 0; i < customPools.Count; i++)
		{
			customPools[i].PreloadInstances();
		}
	}

	// Release an active object back into the pool for reuse
	public static void Release(GameObject obj)
	{
		instance._release(obj);
	}

	void _release(GameObject obj)
	{
		ObjectPoolID id = obj.GetComponent<ObjectPoolID>();
		if (id.Free)
		{
			return;
		}

		//Culling
		// Trigger culling if the feature is ON and the size  of the 
		//   overall pool is over the Cull Above threashold.
		//   This is triggered here because Despawn has to occur before
		//   it is worth culling anyway, and it is run fairly often.
		if (!id.Pool.cullingActive &&   // Cheap & Singleton. Only trigger once!
			id.Pool.cullDespawned &&    // Is the feature even on? Cheap too.
			id.Pool.CountTotal > id.Pool.cullAboveCount)   // Criteria met?
		{
			id.Pool.cullingActive = true;
			StartCoroutine(id.Pool.CullDespawned());

		}

		id.Pool.Release(id);
	}

	// Schedule pool release for the future
	public static void DelayedRelease(GameObject obj, float delayTime)
	{
		instance._delayedRelease(obj, delayTime);
	}

	IEnumerator _delayedRelease(GameObject obj, float delayTime)
	{
		yield return new WaitForSeconds(delayTime);
		_release(obj);
	}

	[Serializable]
	public class Pool //: MonoBehaviour
	{
		public override string ToString()
		{
			if (prefab == null)
				return "Empty Pool";
			return prefab.ToString();
		}

		public GameObject prefab;

		//The parent object to attach spawned and activated instances to, null for no parent
		public GameObject activeParentGO;
		//Parent object for objects that are currently sitting in the pool awaiting use
		public GameObject inactiveParentGO;

		//Helpful name for keeping track of pool assignments
		public string Keyword;
		//Warn via debug log if the pool spawns more instances of a prefab than this number
		public int MaxObjectsWarning = 10;
		//How many prefabs to spawn on level load
		public uint preloadCount = 0;
		//Cull the despawned pool after no use for a period of time or not
		public bool cullDespawned = false;
		//If cullDespawned = true, this is the number of deactive spawns to cull the pool down to
		public uint cullAboveCount = 100;
		//How long to wait before culling inactive objects
		public float cullWaitDelay = 60;
		//How many inactive objects to cull per frame max
		public int cullMaxPerPass = 1000;

		//Internal use only, determines whether culling is actively occurring now or not
		public bool cullingActive = false;

		//Internal use only, manages counts of spawned items in the pool active/inactive
		public int CountFree;
		public int CountInUse;
		public int CountTotal => CountFree + CountInUse;
		List<ObjectPoolID> free = new List<ObjectPoolID>();
		List<ObjectPoolID> inUse = new List<ObjectPoolID>();

		GameObject temp;

		public void PreloadInstances()
		{
			//Fires on level load, prespawns preloadCount number of instances into pool
			if (preloadCount <= 0)
			{
				return;
			}

			ObjectPoolID obj;

			for (int i = 0; i < preloadCount; i++)
			{
				//Create new prefab instantiation and add to free pool
				temp = (GameObject)GameObject.Instantiate(prefab, Vector3.zero, Quaternion.identity);
				obj = temp.AddComponent<ObjectPoolID>();
				free.Add(obj);
				CountFree= free.Count;

				//Configure parent object references
				obj.Pool = this;
				if (activeParentGO)
					obj.MyParentTransform = activeParentGO.transform;
				else
					obj.MyParentTransform = prefab.transform.parent;

				obj.transform.SetParent(obj.MyParentTransform);
				if (CountTotal > MaxObjectsWarning)
				{
					Debug.LogError("ObjectPool: More than max objects spawned. --- " + prefab.name + " Max obj set to: " + MaxObjectsWarning + " and the pool already has: " + CountTotal);
				}

				if (!inactiveParentGO)
					obj.transform.SetParent(instance.transform);
				else
					obj.transform.SetParent(inactiveParentGO.transform);
				obj.SetFree(true);
				obj.gameObject.SetActive(false);

			}
		}

		//Called internally when a script wants an object from the pool, works as above
		public GameObject Request(Vector3 position, Quaternion rotation)
		{
			ObjectPoolID obj;
			if (CountFree <= 0)
			{
				temp = (GameObject)GameObject.Instantiate(prefab, position, rotation);
				obj = temp.AddComponent<ObjectPoolID>();
				inUse.Add(obj);
				CountInUse = inUse.Count;

				obj.Pool = this;
				if (activeParentGO)
					obj.MyParentTransform = activeParentGO.transform;
				else
					obj.MyParentTransform = prefab.transform.parent;

				obj.transform.SetParent(obj.MyParentTransform);
				if (CountTotal > MaxObjectsWarning)
				{
					Debug.LogError("ObjectPool: More than max objects spawned. --- " + prefab.name + " Max obj set to: " + MaxObjectsWarning + " and the pool already has: " + CountTotal);
				}
				obj.SetFree(false);
			}
			else
			{
				obj = free[0];

				free.RemoveAt(0);
				inUse.Add(obj);
				obj.transform.SetParent(obj.MyParentTransform);

				obj.gameObject.transform.position = position;
				obj.gameObject.transform.rotation = rotation;
				obj.gameObject.SetActive(true);

				CountFree = free.Count;
				CountInUse = inUse.Count;
				temp = obj.gameObject;
				obj.SetFree(false);
			}

			//This calls the spawned object's Interface initialization method if it has one
			var init = temp.GetComponent<IPoolInit>();
			if (init != null) init.InitFromPool();
			
			return temp;
		}

		//Opposite of request, frees object into pool after it's done being used
		public void Release(ObjectPoolID obj)
		{
			//This calls the spawned object's Interface deinit method if it has one
			var init = temp.GetComponent<IPoolInit>();
			if (init != null) init.DeactivateBeforeRelease();

			inUse.Remove(obj);
			CountInUse = inUse.Count;
			free.Add(obj);

			CountFree = free.Count;

			if (!inactiveParentGO)
				obj.transform.SetParent(instance.transform);
			else
				obj.transform.SetParent(inactiveParentGO.transform);

			obj.gameObject.SetActive(false);
			obj.SetFree(true);
			
		}

		/// <summary>
		/// Waits for 'cullDelay' in seconds and culls the 'despawned' list if 
		/// above 'cullingAbove' amount. 
		/// 
		/// Triggered by DespawnInstance()
		/// </summary>
		public IEnumerator CullDespawned()
		{
			// First time always pause, then check to see if the condition is
			//   still true before attempting to cull.
			yield return new WaitForSeconds(this.cullWaitDelay);

			while (this.CountTotal > this.cullAboveCount)
			{
				// Attempt to delete an amount == this.cullMaxPerPass
				for (int i = 0; i < this.cullMaxPerPass; i++)
				{
					// Break if this.cullMaxPerPass would go past this.cullAbove
					if (this.CountTotal <= this.cullAboveCount)
						break;  // The while loop will stop as well independently

					// Destroy the last item in the list
					if (this.CountFree > 0)
					{
						ObjectPoolID inst = this.free[0];
						free.RemoveAt(0);
						Destroy(inst.gameObject);
						this.CountFree = free.Count;

					}
					
				}

				// Check again later
				yield return new WaitForSeconds(this.cullWaitDelay);
			}
			
			// Reset the singleton so the feature can be used again if needed.
			this.cullingActive = false;
			yield return null;
		}
	}
}
