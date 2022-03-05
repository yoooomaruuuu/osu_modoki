using UnityEngine;
using TaskSystem;

//オブジェクトプール
public class ObjectPool<T> where T : CachedBehavoir
{
    private struct ObjectParam
    {
        public GameObject prefab;   // 元のprefab
        public Transform root;      // 生成した親ノード
        public T[] pool;            // 空きオブジェクトバッファ
        public int freeIndex;       // 空きオブジェクトインデックス
        public int genMax;          // 生成限界数
        public int genCount;        // 生成した数
    }

    private int category = 0;                       // オブジェクトのカテゴリ
    private int typeCount = 0;                      // オブジェクト取得数
    private ObjectParam[] objParams = null;         // オブジェクト情報
    private T[][] objList = null;                   // 全オブジェクトリスト
    private TaskSystem<T> activeObjTask = null;     // 稼働オブジェクトタスク
    private int orderCount = 0;                     // 実行オブジェクト数

    private float advanceTime = 0f; // procHandler 経過時間
    // デリゲートキャッシュ
    private OrderHandler<T> procHandler = null;
    private OrderHandler<T> clearHandler = null;

    // 稼働数
    public int actCount { get { return this.activeObjTask.count; } }

    // コンストラクタ
    public ObjectPool()
    {
        // デリゲートキャッシュ
        // 複雑な処理でないので今回はラムダ式で手抜きしている
        this.procHandler = new OrderHandler<T>((obj, no) =>
        {
            float elapsedTime = this.advanceTime;
            // 新規追加されたものは経過時間 0sec.
            if (no >= this.orderCount) elapsedTime = 0f;
            if (!obj.Run(no, elapsedTime))
            {
                this.Sleep(obj);
                return false;
            }
            return true;
        });
        this.clearHandler = new OrderHandler<T>((obj, no) =>
        {
            this.Sleep(obj);
            return false;
        });
    }

    // 初期化
    // category : オブジェクトのカテゴリ指定
    // prefabs : 複製するPrefabリスト
    // caps : 複製限回数リスト
    public void Initialize(int category, GameObject[] prefabs, int[] caps)
    {
        // 初期化エラーチェック
        Debug.Assert(prefabs != null && prefabs.Length > 0);
        Debug.Assert(caps != null || caps.Length == prefabs.Length);

        this.category = category;
        this.typeCount = prefabs.Length;
        this.objList = new T[this.typeCount][];
        this.objParams = new ObjectParam[this.typeCount];

        // Prefab 読み込み
        int capacity = 0;
        for (int type=0; type < this.typeCount; ++type)
        {
            int genMax = caps[type];
            if (genMax == 0) continue;

            this.objList[type] = new T[genMax];
            this.objParams[type].pool = new T[genMax];
            this.objParams[type].freeIndex = -1;

            GameObject prefab = prefabs[type];
            // 開発用にまだPrefabが用意されていない場合を考慮してnullを許容する
            if (prefab == null) continue;

            this.objParams[type].genMax = genMax;
            this.objParams[type].genCount = 0;
            this.objParams[type].prefab = prefab;

            // 親ノード作成
            GameObject typeGo = new GameObject(prefab.name);
            typeGo.isStatic = true;
            Transform typeRoot = typeGo.transform;
            this.objParams[type].root = typeRoot;
            // MENU: シーン切り替えで自動で削除させない
            Object.DontDestroyOnLoad(typeGo);

            capacity += genMax;
        }

        this.activeObjTask = new TaskSystem<T>(capacity);
    }

    // 終了
    public void Final()
    {
        for (int type = 0; type < this.typeCount; ++type)
        {
            if (this.objList[type] == null) continue;

            int count = this.objList[type].Length;
            for (int index = 0; index < count; ++index)
            {
                if (this.objList[type][index] == null) break;

                this.objList[type][index].Release();
            }
            Object.Destroy(this.objParams[type].root);
        }
        this.category = 0;
        this.typeCount = 0;
        this.objList = null;
        this.objParams = null;
        this.activeObjTask = null;
    }

    // 全オブジェクト生成
    // 生成数が増えると時間がかかりがちなためInitializeと分ける
    public void Generate()
    {
        for (int type = 0; type < this.typeCount; ++type)
        {
            int genLimit = this.objParams[type].genMax -
                           this.objParams[type].genCount;
            for (int index = 0; index < genLimit; ++index)
            {
                if (this.objList[type][index] != null) continue;
                T obj = this.GenerateObject(type);
                int freeIndex = ++this.objParams[type].freeIndex;
                this.objParams[type].pool[freeIndex] = obj;
            }
        }

    }

    // オブジェクトの生成
    // type : オブジェクトの種類
    private T GenerateObject(int type)
    {
        int index = this.objParams[type].genCount;
        GameObject prefab = this.objParams[type].prefab;
        Transform root = this.objParams[type].root;

        GameObject go = Object.Instantiate(prefab, root) as GameObject;

#if UNITY_EDITOR
        go.name = string.Format(this.objParams[type].prefab.name + "{0:D2}", this.objParams[type].genCount);
#endif
        T obj = go.GetComponent<T>();

        // ユニークIDの割り振り
        obj.Create(UNIQUEID.Create(
            UNIQUEID.CATEGORYBIT(this.category) |
            UNIQUEID.TYPEBIT(type) |
            UNIQUEID.INDEXBIT(index)));

        this.objList[type][index] = obj;
        ++this.objParams[type].genCount;

        return obj;
    }

    // フレームの頭で呼ばれる処理
    public void FrameTop()
    {
        // 更新オブジェクト数の更新
        this.orderCount = this.activeObjTask.count;
    }

    // 定期更新
    // elapsedTime : 経過時間
    public void Proc(float elapsedTime)
    {
        this.advanceTime = elapsedTime;
        if (this.activeObjTask.count > 0)
        {
            this.activeObjTask.Order(this.procHandler);
            this.orderCount = this.activeObjTask.count;
        }
    }

    // 種類別有効数所得
    // type : 種類
    public int GetActiveCount(int type)
    {
        return this.objParams[type].genCount - (this.objParams[type].freeIndex + 1);
    }

    // 全消去
    public void Clear()
    {
        this.activeObjTask.Order(this.clearHandler);
    }

    // オブジェクト呼び出し
    // type : 種類
    // localPosition : 生成座標
    // obj : 生成したオブジェクト
    // return : 呼び出しに成功
    public bool AwakeObject(int type, Vector3 localPositon, out T obj)
    {
        if (this.PickOutObject(type, out obj))
        {
            int no = this.activeObjTask.count - 1;
            obj.WakeUp(no, localPositon);
            return true;
        }
        return false;
    }

    // オブジェクト取得
    // unique : ユニークID
    // obj : 対象オブジェクト
    // return : IDが一致したか(異なる場合はすでに一度回収されている)
    public bool GetObject(UNIQUEID unique, out T obj)
    {
        // 関係のないユニークID
        if (this.category != unique.category)
        {
            obj = null;
            return false;
        }

        obj = this.objList[unique.type][unique.index];
        if (!obj.isAlive) return false;

        // フラッシュIDが更新されていれば別人
        return (obj.uniqueId == unique);
    }

    // オブジェクト取り出し
    // type : 種類
    // obj : 取り出したオブジェクト
    private bool PickOutObject(int type, out T obj)
    {
        obj = null;
        // 空きオブジェクトを取り出す
        if (this.objParams[type].freeIndex >= 0)
        {
            obj = this.objParams[type].pool[this.objParams[type].freeIndex];
            --this.objParams[type].freeIndex;
        } else {
            return false;
        }

        this.activeObjTask.Attach(obj);
        obj.uniqueId.Update();

        return true;
    }

    // 稼働終了処理
    // obj : オブジェクト
    private void Sleep(T obj)
    {
        int type = obj.uniqueId.type;
        ++this.objParams[type].freeIndex;
        this.objParams[type].pool[this.objParams[type].freeIndex] = obj;
        obj.Sleep();
    }

}
