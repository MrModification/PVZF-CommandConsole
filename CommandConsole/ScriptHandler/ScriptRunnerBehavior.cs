using MelonLoader;
using System.Collections;
using UnityEngine;

public class ScriptRunnerBehaviour : MonoBehaviour
{
    public static ScriptRunnerBehaviour Instance;

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
        }
        else if (Instance != this)
        {
            Destroy(gameObject);
        }
    }

    public void RunWait(float seconds)
    {
        MelonCoroutines.Start(WaitRoutine(seconds));
    }

    private static IEnumerator WaitRoutine(float seconds)
    {
        yield return new WaitForSeconds(seconds);
    }
}