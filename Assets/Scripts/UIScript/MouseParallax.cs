using UnityEngine;
using UnityEngine.Rendering;

public class MouseParallax : MonoBehaviour
{
   
    [SerializeField] private GameObject[] ParalaxObjects;
    [SerializeField] private float MouseSpeedX = 1f, MouseSpeedY = 2f;
    private Vector3[] OriginalPositions;

    void Start()
    {
        Cursor.lockState = CursorLockMode.Confined;
        OriginalPositions = new Vector3[ParalaxObjects.Length];
        for(int i = 0; i < ParalaxObjects.Length; i++)
        {
            OriginalPositions[i] = ParalaxObjects[i].transform.position;
        }
    }

    private void FixedUpdate()
    {
        float x, y;
        x = (Input.mousePosition.x - (Screen.width / 2)) * MouseSpeedX / Screen.width;
        y = (Input.mousePosition.y - (Screen.width / 2)) * MouseSpeedY / Screen.width;

        for (int i = 1; i < ParalaxObjects.Length; i++)
        {
            ParalaxObjects[i - 1].transform.position = OriginalPositions[i - 1] + (new Vector3(x, y, 0f) * i * ((i - 1) - (ParalaxObjects.Length/2)));
        }
    }
}
