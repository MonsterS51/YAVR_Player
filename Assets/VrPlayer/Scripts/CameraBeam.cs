using System.Collections;
using UnityEngine;

public class CameraBeam : MonoBehaviour
{
    private const float _maxDist = 11;
    private GameObject _targetObject = null;

    public void Update()
    {
        RaycastHit hit;
        if (Physics.Raycast(transform.position, transform.forward, out hit, _maxDist))
        {
            if (_targetObject != hit.transform.gameObject)
            {
                _targetObject?.SendMessage("OnBeamExit");
                _targetObject = hit.transform.gameObject;
                _targetObject.SendMessage("OnBeamEnter");
            }
        }
        else
        {
            _targetObject?.SendMessage("OnBeamExit");
            _targetObject = null;
        }

        if (Google.XR.Cardboard.Api.IsTriggerPressed)
        {
            _targetObject?.SendMessage("OnBeamClick");
        }
    }
}
