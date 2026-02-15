using UnityEngine;

public class TestLauncher : MonoBehaviour
{
    [Tooltip("LiveUI")]
    [SerializeField] private KeyCode liveKey = KeyCode.Alpha4;
    
    
    private void Update()
    {
        if (Input.GetKeyDown(liveKey))
            StartLive();
    }
    
    public void StartLive()
    {
        UIManager.Instance.SwitchRootPatched<LiveUIRoot>();
    }
}
