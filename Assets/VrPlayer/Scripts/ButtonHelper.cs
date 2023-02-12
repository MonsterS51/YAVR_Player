using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class ButtonHelper : MonoBehaviour
{

    private Button ThisButton { get { return gameObject.GetComponent<Button>(); } }


    // Start is called before the first frame update
    void Start()
    {

    }

    // Update is called once per frame
    void Update()
    {
		//- подгоняем размер коллайдера под размер кнопки
		var size = GetComponent<RectTransform>().rect.size;
		var bc = gameObject.GetComponent<BoxCollider>();
		if (bc != null) bc.size = size;
	}

    public void OnBeamEnter()
    {
		if (!gameObject.activeInHierarchy) return;
		if (ThisButton == null) return;
		if (!ThisButton.isActiveAndEnabled) return;
		Debug.Log($"{nameof(ButtonHelper)} : {gameObject.name} : {nameof(OnBeamEnter)}");
		ThisButton?.Select();
	}
	public void OnBeamExit()
	{
		if (ThisButton == null) return;
		Debug.Log($"{nameof(ButtonHelper)} : {gameObject.name} : {nameof(OnBeamExit)}");
		EventSystem.current.SetSelectedGameObject(null);
	}

	public void OnBeamClick()
	{
		if (!gameObject.activeInHierarchy) return;
		if (ThisButton == null) return;
		if (!ThisButton.isActiveAndEnabled) return;
		Debug.Log($"{nameof(ButtonHelper)} : {gameObject.name} : {nameof(OnBeamClick)}");
		ThisButton.onClick?.Invoke();
	}

}
