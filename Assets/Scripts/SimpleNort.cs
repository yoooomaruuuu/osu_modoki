using UnityEngine;
using UnityEngine.EventSystems;
public class SimpleNort : Nort
{

    public override void OnActionCallback(PointerEventData data)
    {
        this.trans_.localScale = Vector3.one * 2;
        this.hited = true;
        Debug.Log("click!");
    }

    public override void SetEntry()
    {
        entry = new EventTrigger.Entry();
        entry.eventID = EventTriggerType.PointerClick;
        entry.callback.AddListener((data) => { OnActionCallback((PointerEventData)data);  });
    }

}
