using System;
using UnityEngine;

public abstract class Singleton<T> : MonoBehaviour where T : MonoBehaviour
{
    private static readonly Lazy<T> LazyInstance = new Lazy<T>(CreateSingleton);

    public static T Instance => LazyInstance.Value;

    private static T CreateSingleton()
    {
        var existing = FindObjectOfType<T>();
        if (existing)
        {
            Debug.Log($"{typeof(T).Name} already exists, returning existing instance." );
            DontDestroyOnLoad(existing.gameObject);
            return existing;
        }
        Debug.Log($"Created new instance of {typeof(T).Name}." );
        var ownerObject = new GameObject($"{typeof(T).Name} (singleton)");
        var instance = ownerObject.AddComponent<T>();
        DontDestroyOnLoad(ownerObject);
        return instance;
    }
}