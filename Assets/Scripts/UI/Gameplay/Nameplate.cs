using TMPro;
using UnityEngine;

public class Nameplate : MonoBehaviour
{
    [SerializeField] private TMP_Text label;
    [SerializeField] private float faceLerp = 20f;

    public void SetText(string value)
    {
        if (label != null) label.text = value;
    }

    void LateUpdate()
    {
        var cam = Camera.main;
        if (!cam) return;

        var toCam = transform.position - cam.transform.position;
        var target = Quaternion.LookRotation(toCam);
        transform.rotation = Quaternion.Slerp(transform.rotation, target, Time.deltaTime * faceLerp);
    }
}
