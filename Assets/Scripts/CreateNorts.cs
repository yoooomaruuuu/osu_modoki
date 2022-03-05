using UnityEngine;

public class CreateNorts : MonoBehaviour
{
    [SerializeField, Tooltip("複製するPrefab")]
    private GameObject[] prefabs = new GameObject[1];
    [SerializeField, Tooltip("生成数")]
    private int[] caps = new int[1];

    private ObjectPool<Nort> pool = new ObjectPool<Nort>();
    private float calcTimeSpan = 0f;

    // Start is called before the first frame update
    void Start()
    {
        // プール内バッファ生成
        this.pool.Initialize(0, this.prefabs, this.caps);
        // オブジェクトの生成
        this.pool.Generate();
    }

    public void OnDestroy()
    {
        this.pool.Final();
    }
    // Update is called once per frame
    void Update()
    {
        this.pool.FrameTop();
        float elapsedTime = Time.deltaTime;
        this.calcTimeSpan += elapsedTime;

        float span = 1f;
        if(this.calcTimeSpan > span)
        {
            float createX = Random.value * Screen.width;
            float createY = Random.value * Screen.height;
            Nort nort;
            Vector3 point = Camera.main.ScreenToWorldPoint(new Vector3(createX, createY, 1f));
            if(this.pool.AwakeObject(0, point, out nort))
            {
            }
            this.calcTimeSpan -= span;
        }
        this.pool.Proc(elapsedTime);
    }
}
