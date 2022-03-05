using UnityEngine;
using UnityEngine.EventSystems;

class ScrollNort : Nort
{

    public override void OnActionCallback(PointerEventData data)
    {
        this.trans_.localScale = Vector3.one * 2;
        this.hited = true;
        Debug.Log("drag!");
    }

    public override void SetEntry()
    {
        entry = new EventTrigger.Entry();
        entry.eventID = EventTriggerType.Drag;
        entry.callback.AddListener((data) => { OnActionCallback((PointerEventData)data);  });
    }

}
