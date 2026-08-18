using UnityEngine;
using static UnityEngine.Rendering.DebugUI;

public class LargeMap : MonoBehaviour
{

    [SerializeField] GameObject MapCanvas;

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        if (Input.GetKey(KeyCode.M))
        {
            MapCanvas.SetActive(true);
        }
        else
        {
            MapCanvas.SetActive(false);
        }
    }
    }
