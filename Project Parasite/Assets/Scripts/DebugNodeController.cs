using UnityEngine;

public class DebugNodeController : MonoBehaviour
{
    public GameObject parent;
    private DebugControls debug;

    void Start()
    {
        debug = parent.GetComponent<IHasPlayer>().player.GetComponent<PlayerController>().cam.GetComponent<DebugControls>();
    }

    void Update()
    {
        gameObject.GetComponent<SpriteRenderer>().enabled = debug.viewDebug;
    }
}
