using UnityEngine;

public class PersistUI : MonoBehaviour
{
    private static PersistUI instance;

    void Awake()
    {
        if (instance == null)
        {
            instance = this;
            DontDestroyOnLoad(this.gameObject);  // Prevents the UI from being destroyed on scene reload
        }
        else
        {
            Destroy(gameObject);  // Ensures only one UI instance exists
        }
    }
}