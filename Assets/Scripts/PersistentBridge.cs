using UnityEngine;
using Coherence.Toolkit;

[DisallowMultipleComponent]
public class PersistentBridge : MonoBehaviour
{
    void Awake()
    {
        DontDestroyOnLoad(gameObject);
    }
}

