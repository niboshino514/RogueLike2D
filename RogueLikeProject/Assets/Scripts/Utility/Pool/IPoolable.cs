using UnityEngine;

namespace Utility.Pool
{
    public interface IPoolable
    {
        abstract void OnCreate(GameObject prefab);
        abstract void OnSpawn();
        abstract void OnDespawn();
    }
}