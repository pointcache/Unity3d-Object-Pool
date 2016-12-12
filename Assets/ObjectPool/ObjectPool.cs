// /** 
// * ObjectPool.cs
// * Will Hart and Dylan Bailey
// * 20161203
// */

namespace Zen.ObjectPool
{
	#region Dependencies

	using System;
	using System.Collections.Generic;
	using UnityEngine;
	using Zenobit.Common.ObjectPool;
	using Object = UnityEngine.Object;

	#endregion

	public static class ObjectPoolExtensions
	{
		/// <summary>
		/// Creates or retrieves an instance of the prefab from the Object Pool.
		/// </summary>
		public static GameObject InstantiateFromPool(this GameObject prefab, Vector3 position, Quaternion rotation)
		{
			return ObjectPool.Instantiate(prefab, position, rotation);
		}

		/// <summary>
		/// Creates or retrieves an instance of the prefab from the Object Pool.
		/// </summary>
		public static GameObject InstantiateFromPool(this GameObject prefab)
		{
			return ObjectPool.Instantiate(prefab, Vector3.zero, Quaternion.identity);
		}

		/// <summary>
		/// Creates or retrieves an instance of the prefab from the Object Pool and returns it as the given type.
		/// </summary>
		public static T InstantiateFromPool<T>(this GameObject prefab)
			where T : class
		{
			var tComp = ObjectPool.Instantiate(prefab).GetComponent<T>();
			if (tComp == null) Debug.LogError("Object of type " + typeof(T).Name + " is not contained in prefab");
			return tComp;
		}

		/// <summary>
		/// Creates or retrieves an instance of the prefab from the Object Pool and returns it as the given type.
		/// </summary>
		public static T InstantiateFromPool<T>(this GameObject prefab, Vector3 position, Quaternion rotation)
			where T : class
		{
			var tComp = ObjectPool.Instantiate(prefab, position, rotation).GetComponent<T>();
			if (tComp == null) Debug.LogError("Object of type " + typeof(T).Name + " is not contained in prefab");
			return tComp;
		}

		/// <summary>
		/// Creates or retrieves an instance of the prefab from the Object Pool.
		/// </summary>
		public static GameObject InstantiateFromPool(
			this GameObject prefab,
			Vector3 position,
			Quaternion rotation,
			GameObject activeParent,
			GameObject inactiveParent)
		{
			return ObjectPool.Instantiate(prefab, position, rotation, activeParent, inactiveParent);
		}

		/// <summary>
		/// Creates or retrieves an instance of the prefab from the Object Pool and returns it as the given type.
		/// </summary>
		public static T InstantiateFromPool<T>(
			this GameObject prefab,
			Vector3 position,
			Quaternion rotation,
			GameObject activeParent,
			GameObject inactiveParent)
			where T : class
		{
			return ObjectPool.Instantiate(prefab, position, rotation, activeParent, inactiveParent) as T;
		}

		/// <summary>
		/// Releases the prefab instance back to the pool. Use in place of Destroy(gameObject)
		/// </summary>
		public static void Release(this GameObject objthis)
		{
			ObjectPool.Release(objthis);
		}

		//public static void Release<T>(this T objThis) where T: UnityEngine.Component
		//{
		//	ObjectPool.Release(objThis.gameObject);
		//}

		/// <summary>
		/// Releases the prefab instance back to the pool after a timed delay. Use in place of Destroy(gameObject, delayTime)
		/// </summary>
		public static void ReleaseDelayed(this GameObject objthis, float delayTime)
		{
			ObjectPool.DelayedRelease(objthis, delayTime);
		}
	}

	public class ObjectPool : MonoBehaviour
	{
		private static ObjectPool _instance;

		private readonly Dictionary<GameObject, Pool> _pool = new Dictionary<GameObject, Pool>();
		[Tooltip("Parent gameobject that runtime pooled instances of a prefab are set to when active. Overridden in custom pools")]
		public GameObject ActiveParentDefault;
		[Tooltip("Parent gameobject that runtime pooled instances of a prefab are set to when released. Overridden in custom pools")]
		public GameObject InactiveParentDefault;

		public List<Pool> CustomPools = new List<Pool>();

		public List<Pool> RuntimePools = new List<Pool>();
		//Read only, just displays pools created on runtime without prior setup."

		public static ObjectPool Instance
		{
			get
			{
				if (_instance != null) return _instance;

				_instance = FindObjectOfType<ObjectPool>();
				if (_instance != null) return _instance;

				var go = new GameObject("ObjectPool");
				_instance = go.AddComponent<ObjectPool>();
				return _instance;
			}
		}

		public static GameObject Instantiate(
			GameObject prefab,
			Vector3 position,
			Quaternion rotation,
			GameObject activeParent = null,
			GameObject inactiveParent = null)

		{
			return Instance._Instantiate(prefab, position, rotation, activeParent, inactiveParent);
		}

		private GameObject _Instantiate(
			GameObject prefab,
			Vector3 position,
			Quaternion rotation,
			GameObject activeParent = null,
			GameObject inactiveParent = null)
		{
			if (_pool.ContainsKey(prefab))
			{
				var current = _pool[prefab];

				//GameObject keywordActiveGameObject = new GameObject(current.Keyword);
				//keywordActiveGameObject.transform.SetParent(activeParent.transform);
				//current.activeParentGO = keywordActiveGameObject;
				//
				//GameObject keywordInactiveGameObject = new GameObject(current.Keyword);
				//keywordInactiveGameObject.transform.SetParent(inactiveParent.transform);
				//current.inactiveParentGO = keywordInactiveGameObject;

				//if (current.activeParentGO == null)


				return current.Request(position, rotation);
			}
			var newpool = new Pool();


			//if (activeParent) newpool.activeParentGO = activeParent;
			//else newpool.activeParentGO = ActiveParentDefault;
			//if (inactiveParent) newpool.inactiveParentGO = inactiveParent;
			//else newpool.inactiveParentGO = InactiveParentDefault;

			var keywordActiveGameObject = new GameObject(prefab.name);
			keywordActiveGameObject.transform.SetParent(
				activeParent != null ? activeParent.transform : ActiveParentDefault.transform);
			newpool.ActiveParentGo = keywordActiveGameObject;

			var keywordInactiveGameObject = new GameObject(prefab.name);
			keywordInactiveGameObject.transform.SetParent(
				inactiveParent != null ? inactiveParent.transform : InactiveParentDefault.transform);
			newpool.InactiveParentGo = keywordInactiveGameObject;

			RuntimePools.Add(newpool);
			_pool.Add(prefab, newpool);
			newpool.MaxObjectsWarning = 1000;
			newpool.Prefab = prefab;
			return newpool.Request(position, rotation);
		}

		private void Awake()
		{
			if (ActiveParentDefault == null) ActiveParentDefault = GameObject.Find("Active");
			if (InactiveParentDefault == null) InactiveParentDefault = GameObject.Find("Inactive");

			foreach (var p in CustomPools)
			{
				if (p.Prefab == null)
				{
					Debug.LogError("Custom object pool exists without prefab assigned to it.");
					continue;
				}
				//Set keyword if blank
				if (p.Keyword.Length == 0) p.Keyword = p.Prefab.name;
				//Set custom pool's parents to the default if they don't exist
				if (p.ActiveParentGo == null)
				{
					p.ActiveParentGo = ActiveParentDefault;
					var keywordActiveGameObject = new GameObject(p.Keyword);
					keywordActiveGameObject.transform.SetParent(p.ActiveParentGo.transform);
					p.ActiveParentGo = keywordActiveGameObject;
				}
				if (p.InactiveParentGo == null)
				{
					p.InactiveParentGo = InactiveParentDefault;
					var keywordInactiveGameObject = new GameObject(p.Keyword);
					keywordInactiveGameObject.transform.SetParent(p.InactiveParentGo.transform);
					p.InactiveParentGo = keywordInactiveGameObject;
				}

				if (!_pool.ContainsKey(p.Prefab))
				{
					_pool.Add(p.Prefab, p);
				}
				else
				{
					Debug.LogError("Trying to add object that is already registered by object pooler.");
				}
			}
		}

		private void Start()
		{
			foreach (var p in CustomPools)
			{
				p.PreloadInstances();
			}
		}

		public static void Release(GameObject obj)
		{
			Instance._release(obj);
		}

		void _release(GameObject obj)
		{
			ObjectPoolId id = obj.GetComponent<ObjectPoolId>();
			if (id.Free)
			{
				return;
			}

			//Culling
			// Trigger culling if the feature is ON and the size  of the 
			//   overall pool is over the Cull Above threashold.
			//   This is triggered here because Despawn has to occur before
			//   it is worth culling anyway, and it is run fairly often.
			if (!id.Pool.CullingActive && // Cheap & Singleton. Only trigger once!
			    id.Pool.cullDespawned && // Is the feature even on? Cheap too.
			    (id.Pool.CountTotal > id.Pool.CullAboveCount)) // Criteria met?
			{
				id.Pool.CullingActive = true;
				//StartCoroutine(id.Pool.CullDespawned());
				Timing.RunCoroutine(id.Pool.CullDespawned(), Segment.SlowUpdate);
			}

			id.Pool.Release(id);
		}

		public static void DelayedRelease(GameObject obj, float delayTime)
		{
			Instance._delayedRelease(obj, delayTime);
		}

		private void _delayedRelease(GameObject obj, float delayTime)
		{
			Timing.CallDelayed(
				obj,
				delayTime,
				x => { Timing.RunCoroutine(_delayedReleaseCoroutine(x)); });
		}

		IEnumerator<float> _delayedReleaseCoroutine(GameObject obj)
		{
			_release(obj);
			yield return 0f;
		}

		[Serializable]
		public class Pool //: MonoBehaviour
		{
			[Tooltip("The prefab to pool")]
			public GameObject Prefab;

			[Tooltip("The name given to pooled objects")]
			public string Keyword;

			[Tooltip("How many of the prefab should be pre-instantiated on game start")]
			public uint PreloadCount = 0;

			[Tooltip("Parent gameobject that pooled instances of this prefab are set to when active")]
			public GameObject ActiveParentGo;
			[Tooltip("Parent gameobject that pooled instances of this prefab are set to when released to the pool")]
			public GameObject InactiveParentGo;

			[Tooltip("Pooled prefab count limit before you receive console log warnings")]
			public int MaxObjectsWarning = 10000;

			[Tooltip("Enables the pool to cull prefabs above a set limit")]
			public bool cullDespawned = false;

			[Tooltip("The count above which the pool culls IFF Cull Despawned is enabled")]
			public uint CullAboveCount = 100;

			[Tooltip("The maximum objects the pool will cull per activation")]
			public int CullMaxPerPass = 1000;

			[Tooltip("How long (in seconds) since the last pool use before culling engages")]
			public float CullWaitDelay = 60;
			[ReadOnly] public bool CullingActive;

			[ReadOnly] public int CountFree;
			[ReadOnly] public int CountInUse;

			public int CountTotal
			{
				get { return CountFree + CountInUse; }
			}
			private readonly List<ObjectPoolId> _free = new List<ObjectPoolId>();
			private readonly List<ObjectPoolId> _inUse = new List<ObjectPoolId>();
			private GameObject _temp;


			public override string ToString()
			{
				return Prefab == null ? "Empty Pool" : Prefab.ToString();
			}

			public void PreloadInstances()
			{
				if (Keyword.Length == 0) Keyword = Prefab.gameObject.name;
				if (PreloadCount <= 0)
				{
					return;
				}

				for (var i = 0; i < PreloadCount; i++)
				{
					_temp = Object.Instantiate(Prefab, Vector3.zero, Quaternion.identity);
					_temp.name += CountTotal;
					var obj = _temp.AddComponent<ObjectPoolId>();
					_free.Add(obj);
					CountFree = _free.Count;

					obj.Pool = this;
					obj.MyParentTransform = ActiveParentGo ? ActiveParentGo.transform : Prefab.transform.parent;

					obj.transform.SetParent(obj.MyParentTransform);
					if (CountTotal > MaxObjectsWarning)
					{
						//Debug.LogError("ObjectPool: More than max objects spawned. --- " + prefab.name + " Max obj set to: " + MaxObjectsWarning + " and the pool already has: " + CountTotal);
					}

					obj.transform.SetParent(!InactiveParentGo ? Instance.transform : InactiveParentGo.transform);
					obj.SetFree(true);
					obj.gameObject.SetActive(false);
				}
			}

			public GameObject Request(Vector3 position, Quaternion rotation)
			{
				ObjectPoolId obj;
				if (CountFree <= 0)
				{
					_temp = Object.Instantiate(Prefab, position, rotation);
					_temp.name += CountTotal;
					obj = _temp.AddComponent<ObjectPoolId>();
					_inUse.Add(obj);
					CountInUse = _inUse.Count;

					obj.Pool = this;
					obj.MyParentTransform = ActiveParentGo ? ActiveParentGo.transform : Prefab.transform.parent;

					obj.transform.SetParent(obj.MyParentTransform);
					//if (CountTotal > MaxObjectsWarning)
					//{
					//    //Debug.LogError("ObjectPool: More than max objects spawned. --- " + prefab.name + " Max obj set to: " + MaxObjectsWarning + " and the pool already has: " + CountTotal);
					//}
					obj.SetFree(false);
				}
				else
				{
					obj = _free[_free.Count - 1];

					_free.RemoveAt(_free.Count - 1);

					_inUse.Add(obj);
					obj.transform.SetParent(obj.MyParentTransform);

					obj.gameObject.transform.position = position;
					obj.gameObject.transform.rotation = rotation;
					//obj.gameObject.SetActive(true);

					CountFree = _free.Count;
					CountInUse = _inUse.Count;
					_temp = obj.gameObject;
					_temp.SetActive(true);
					obj.SetFree(false);
				}

				var init = _temp.GetComponent<IPoolInit>();
				if (init != null)
				{
					init.InitFromPool();
				}

				return _temp;
			}

			public void Release(ObjectPoolId obj)
			{
				var release = obj.GetComponent<IPoolRelease>();
				if (release != null)
				{
					release.DeactivateBeforeRelease();
				}

				_inUse.Remove(obj);
				CountInUse = _inUse.Count;
				_free.Add(obj);

				CountFree = _free.Count;

				obj.transform.SetParent(!InactiveParentGo ? Instance.transform : InactiveParentGo.transform);

				obj.SetFree(true);
				obj.gameObject.SetActive(false);
			}

			/// <summary>
			///     Waits for 'cullDelay' in seconds and culls the 'despawned' list if
			///     above 'cullingAbove' amount.
			///     Triggered by DespawnInstance()
			/// </summary>
			public IEnumerator<float> CullDespawned()
			{
				// First time always pause, then check to see if the condition is
				//   still true before attempting to cull.
				yield return Timing.WaitForSeconds(CullWaitDelay);

				while (CountTotal > CullAboveCount)
				{
					// Attempt to delete an amount == this.cullMaxPerPass
					for (var i = 0; i < CullMaxPerPass; i++)
					{
						// Break if this.cullMaxPerPass would go past this.cullAbove
						if (CountTotal <= CullAboveCount)
							break; // The while loop will stop as well independently

						// Destroy the last item in the list
						if (CountFree <= 0) continue;

						var inst = _free[0];
						_free.RemoveAt(0);
						Destroy(inst.gameObject);
						CountFree = _free.Count;
					}

					// Check again later
					yield return Timing.WaitForSeconds(CullWaitDelay);
				}

				// Reset the singleton so the feature can be used again if needed.
				CullingActive = false;
				yield return 0.0f;
			}
		}
	}
}