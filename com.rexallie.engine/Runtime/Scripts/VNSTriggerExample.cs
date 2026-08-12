using UnityEngine;

public class VNSTriggerExample : MonoBehaviour
{
    void Start()
    {
        if (DialogueManager.Instance != null)
        {
            DialogueManager.Instance.OnVNSTrigger += HandleVNSTrigger;
        }
    }

    void OnDestroy()
    {
        if (DialogueManager.Instance != null)
        {
            DialogueManager.Instance.OnVNSTrigger -= HandleVNSTrigger;
        }
    }

    private void HandleVNSTrigger(string eventName, string[] parameters)
    {
        Debug.Log($"[VNSTriggerExample] Received event '{eventName}' with {parameters.Length} parameters.");

        if (eventName.ToLower() == "spawn")
        {
            if (parameters.Length > 0)
            {
                string objType = parameters[0].ToLower();
                if (objType == "cube")
                {
                    GameObject cube = GameObject.CreatePrimitive(PrimitiveType.Cube);
                    cube.transform.position = new Vector3(0, 2, 0);
                    cube.name = "VNS_SpawnedCube";
                    Debug.Log("[VNSTriggerExample] Spawned a Cube in the scene!");
                }
                else if (objType == "sphere")
                {
                    GameObject sphere = GameObject.CreatePrimitive(PrimitiveType.Sphere);
                    sphere.transform.position = new Vector3(0, 2, 0);
                    sphere.name = "VNS_SpawnedSphere";
                    Debug.Log("[VNSTriggerExample] Spawned a Sphere in the scene!");
                }
            }
        }
    }
}
