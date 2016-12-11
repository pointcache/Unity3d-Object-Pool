// /** 
//  * ObjectPoolId.cs
//  * Dylan Bailey
//  * 20161209
// */

namespace Zen.ObjectPool
{
    #region Dependencies

    using UnityEngine;

    #endregion
    

    public class ObjectPoolId : MonoBehaviour
    {
        public Transform MyParentTransform;
        public ObjectPool.Pool Pool { get; set; }

	    public bool Free
	    {
		    get { return IsFree; }
			set { IsFree = value; }
	    }
        private bool IsFree { get; set; }

	    public int ThisId
	    {
		    get { return GetInstanceID(); }
	    } 

        public int PrefabId
        {
            get
            {
                return MyParentTransform == null ? 0 : MyParentTransform.gameObject.GetInstanceID();
            }
        }

        public void SetFree(bool state)
        {
            IsFree = state;
        }

        public bool GetFree()
        {
            return IsFree;
        }
    }
}