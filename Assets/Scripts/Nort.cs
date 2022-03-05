using UnityEngine;
using UnityEngine.EventSystems;

public abstract class Nort : CachedBehavoir
{
    private SpriteRenderer spRender = null;
    public bool hited { get; set; }
    public EventTrigger.Entry entry = null;
    // オブジェクト生成時処理
    protected override void OnCreate()
    {
        EventTrigger trigger = GetComponent<EventTrigger>();
        SetEntry();
        trigger.triggers.Add(entry);
        this.spRender = this.GetComponent<SpriteRenderer>();
        hited = false;
    }

    // オブジェクト解放処理
    protected override void OnRelease()
    {
    }

    // オブジェクト呼び出し処理
    protected override void OnAwake(int no)
    {
        // 稼働No.をプライオリティにして後発の弾が手前
        this.spRender.sortingOrder = no;
        Debug.Log("create!: " + this.uniqueId.ToString());
    }

    // オブジェクト実行処理
    protected override bool OnRun(int no, float elapsedTime)
    {
        if (hited) return false;
        return true;
    }

    // オブジェクト回収時処理
    protected override void OnSleep()
    {
        hited = false;
    }
    public abstract void OnActionCallback(PointerEventData data);

    public abstract void SetEntry();
}
