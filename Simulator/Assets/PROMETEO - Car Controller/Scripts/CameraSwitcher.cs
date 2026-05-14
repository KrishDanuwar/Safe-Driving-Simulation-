using UnityEngine;

public class CameraSwitcher : MonoBehaviour
{
    [Header("TPP Settings")]
    public Vector3 tppPosition = new Vector3(0, 3.5f, -9f);
    public Vector3 tppRotation = new Vector3(15, 0, 0);

    [Header("FPP Settings")]
    public Vector3 fppPosition = new Vector3(0.0f, 1.2f, 1.2f);
    public Vector3 fppRotation = new Vector3(0, 0, 0);

    private bool isFPP = false;

    void Start()
    {
        ApplyTPP();
    }

    void Update()
    {
        // Strictly only the X key, no Shift required.
        if (Input.GetKeyDown(KeyCode.X))
        {
            ToggleCamera();
        }
    }

    void ToggleCamera()
    {
        isFPP = !isFPP;
        Debug.Log("Camera Switched! FPP mode: " + isFPP);
        
        if (isFPP)
        {
            transform.localPosition = fppPosition;
            transform.localRotation = Quaternion.Euler(fppRotation);
        }
        else
        {
            transform.localPosition = tppPosition;
            transform.localRotation = Quaternion.Euler(tppRotation);
        }
    }

    void ApplyTPP()
    {
        transform.localPosition = tppPosition;
        transform.localRotation = Quaternion.Euler(tppRotation);
    }
}
