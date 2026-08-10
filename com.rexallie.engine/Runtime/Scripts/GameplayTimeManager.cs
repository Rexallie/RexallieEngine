using UnityEngine;

public class GameplayTimeManager : MonoBehaviour
{
    public static GameplayTimeManager Instance { get; private set; }

    private float totalPlaytime = 0f;

    void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
        }
        else
        {
            Destroy(gameObject);
        }
    }

    void Update()
    {
        // Add the time elapsed since the last frame to our total.
        totalPlaytime += Time.deltaTime;
    }

    public float GetPlaytime()
    {
        return totalPlaytime;
    }

    public void SetPlaytime(float time)
    {
        totalPlaytime = time;
    }
}