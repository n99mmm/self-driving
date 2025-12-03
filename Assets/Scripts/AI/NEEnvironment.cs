// SerialID: [77a855b2-f53d-4b80-9c94-c40562952b74]
using UnityEngine;
using UnityEngine.UI;
using System.Collections.Generic;
using System;
using System.Linq;
using System.IO;

#if UNITY_EDITOR
using UnityEditor.SceneManagement;
#endif


[System.Serializable]
public class CheckpointData
{
    public int generation;
    public int totalPopulation;
    // NNBrain.cs で定義した BrainData を使用する前提
    public List<BrainData> brainDataList; 
}


public class NEEnvironment : Environment
{
    [Header("Settings"), SerializeField] private int totalPopulation = 100;
    private int TotalPopulation { get { return totalPopulation; } }

    [SerializeField] private int tournamentSelection = 85;
    private int TournamentSelection { get { return tournamentSelection; } }

    [SerializeField] private int eliteSelection = 4;
    private int EliteSelection { get { return eliteSelection; } }

    [SerializeField] public bool[] selectedInputs = new bool[46];
    [SerializeField] public List<double> sensorAngleConfig = new List<double>();

    private int InputSize { get; set; }

    private List<int> SelectedInputsList { get; set; }

    [SerializeField] private int hiddenSize = 8;
    private int HiddenSize { get { return hiddenSize; } }

    [SerializeField] private int hiddenLayers = 1;
    private int HiddenLayers { get { return hiddenLayers; } }

    [SerializeField] private int outputSize = 4;
    private int OutputSize { get { return outputSize; } }

    [SerializeField] private int nAgents = 4;
    private int NAgents { get { return nAgents; } }


    [Header("Agent Prefab"), SerializeField] private GameObject gObject = null;
    private GameObject GObject => gObject;

    [SerializeField] private bool isChallenge4 = false;
    private bool IsChallenge4 { get { return isChallenge4; } }

    [Header("Checkpoint Management")] 
    [SerializeField] private string baseSaveName = "CarEvolution"; // ① 基本ファイル名
    private string BaseSaveName => baseSaveName;
    
    [SerializeField] private string loadCheckpointPath = ""; // ② ロードするファイルのパス
    private string LoadCheckpointPath => loadCheckpointPath;
    
    [SerializeField] private bool loadOnStart = false; // ③ ロードを有効にするフラグ

    [Header("UI References"), SerializeField] private Text populationText = null;
    
    [Header("File Management Subfolders")]
    [SerializeField] private string checkpointFolderName = "Checkpoints"; // JSON用フォルダ名
    [SerializeField] private string statsFolderName = "Stats";

    [Header("Stage Configuration")]
    [SerializeField] private string stageName = "Stage1"; // 例: Stage1, Stage2 など
    private string StageName => stageName;

    [Header("Learning Curve Visualization")]
    // private List<double> bestRewardsHistory = new List<double>();
    // private List<double> avgRewardsHistory = new List<double>();

    // // グラフを描画するためのLineRendererと親オブジェクト（Unity画面上で設定）
    // public LineRenderer bestRewardLine;
    // public LineRenderer avgRewardLine;
    // public RectTransform graphContainer; // グラフの描画領域の親となるRectTransform


        // ★ AnimationCurve を使う新しい宣言 ★
    [SerializeField] private AnimationCurve bestRewardCurve = new AnimationCurve();
    [SerializeField] private AnimationCurve avgRewardCurve = new AnimationCurve();


    // グラフ設定用のパラメータ
    public float maxRewardY = 20.0f; // グラフのY軸の最大値（報酬の予想最大値）
    public int maxHistoryPoints = 100; // 画面に表示する世代の最大数
    private Text PopulationText { get { return populationText; } }

    private float GenBestRecord { get; set; }

    private float SumReward { get; set; }
    private float AvgReward { get; set; }

    private List<NNBrain> Brains { get; set; } = new List<NNBrain>();
    private List<GameObject> GObjects { get; } = new List<GameObject>();
    private List<Agent> Agents { get; } = new List<Agent>();
    private int Generation { get; set; }

    private float BestRecord { get; set; }

    private List<AgentPair> AgentsSet { get; } = new List<AgentPair>();
    private Queue<NNBrain> CurrentBrains { get; set; }

    private List<Obstacle> Obstacles { get; } = new List<Obstacle>();
    // NEEnvironment.cs クラス内変数に追加
    private string sessionTimestamp;

/// <summary>
    /// 現在の個体群の状態（重みと世代番号）をJSONファイルとして保存します。
    /// </summary>
    private void SavePopulation()
    {
        // 世代交代中のGenPopulation()から呼ばれた場合、世代番号は既にインクリメントされています。
        // 手動保存の場合は、現在の評価中の世代番号を保存します。
        
        var checkpoint = new CheckpointData
        {
            generation = this.Generation,
            totalPopulation = this.TotalPopulation,
            brainDataList = new List<BrainData>()
        };

        foreach (var brain in Brains)
        {
            // BrainsリストにはNNBrainオブジェクトが入っているため、GetBrainData()を呼び出し
            checkpoint.brainDataList.Add(brain.GetBrainData());
        }

        // ファイル名を生成: BaseSaveName_Gen{世代番号}_{日時}.json
        string timestamp = DateTime.Now.ToString("yyyyMMdd_HHmmss");
        string filename = $"{BaseSaveName}_Gen{Generation}_{timestamp}.json";
        
        string directoryPath = Application.dataPath + $"/LearningData/NE/{StageName}/{checkpointFolderName}/";
        if (!System.IO.Directory.Exists(directoryPath))
        {
            System.IO.Directory.CreateDirectory(directoryPath);
        }
        
        string fullPath = directoryPath + filename;
        
        try
        {
            string json = JsonUtility.ToJson(checkpoint, true); // trueで整形して保存
            System.IO.File.WriteAllText(fullPath, json);
            Debug.Log($"✅ Checkpoint saved successfully: {filename}");
        }
        catch (Exception e)
        {
            Debug.LogError($"❌ Error saving checkpoint: {e.Message}");
        }
    }

    private void OnApplicationQuit(){
        // アプリケーション（またはエディターでの実行）が終了する直前に保存
        Debug.Log("Saving final checkpoint before quitting...");
        SavePopulation(); 
    }

    //     /// <summary>
    // /// 学習履歴を更新し、Unity画面上のLineRendererを使ってグラフを描画します。
    // /// </summary>
    // private void UpdateGraph()
    // {
    //         // デバッグログ追加
    //     if (bestRewardLine == null) {
    //         Debug.LogError("FATAL ERROR: bestRewardLine is NULL!");
    //         return;
    //     }
    //     if (avgRewardLine == null) {
    //         Debug.LogError("FATAL ERROR: avgRewardLine is NULL!");
    //         return;
    //     }
    //     Debug.Log("UpdateGraph: LineRenderers are OK. Attempting to add data.");

    //     // 履歴に最新のデータを追加
    //     bestRewardsHistory.Add(GenBestRecord);
    //     avgRewardsHistory.Add(AvgReward);

    //     // 古すぎるデータを削除して、表示世代数を制限
    //     if (bestRewardsHistory.Count > maxHistoryPoints)
    //     {
    //         bestRewardsHistory.RemoveAt(0);
    //         avgRewardsHistory.RemoveAt(0);
    //     }

    //     int count = bestRewardsHistory.Count;
    //     float graphWidth = graphContainer.rect.width;
    //     float graphHeight = graphContainer.rect.height;

    //     // LineRendererの準備
    //     bestRewardLine.positionCount = count;
    //     avgRewardLine.positionCount = count;

    //     for (int i = 0; i < count; i++)
    //     {
    //         // X座標: 履歴のインデックスを現在の表示世代数で正規化し、グラフの幅を乗算
    //         float xPosition = (float)i / (maxHistoryPoints - 1) * graphWidth;

    //         // Y座標: 報酬をmaxRewardYで正規化し、グラフの高さを乗算
    //         float bestY = Mathf.Clamp((float)bestRewardsHistory[i] / maxRewardY, 0f, 1f) * graphHeight;
    //         float avgY = Mathf.Clamp((float)avgRewardsHistory[i] / maxRewardY, 0f, 1f) * graphHeight;

    //         // グラフの位置をRectTransform（Canvasの子）のローカル座標で設定
    //         bestRewardLine.SetPosition(i, new Vector3(xPosition, bestY, 0));
    //         avgRewardLine.SetPosition(i, new Vector3(xPosition, avgY, 0));
    //     }
    // }

        /// <summary>
    /// 学習履歴を更新し、AnimationCurve に Keyframe を追加します。
    /// </summary>

    
    // WaypointsController への参照（Inspectorで設定するか、FindObjectOfTypeで取得）
    // private WaypointsController waypointsController; 
    
    private void UpdateCurve()
    {
        // 既存の GenBestRecord と AvgReward の値を使用
        float time = (float)Generation; // 世代番号を時間軸に使用

        // Keyframe を作成し、AnimationCurve に追加
        Keyframe bestKey = new Keyframe(time, GenBestRecord);
        Keyframe avgKey = new Keyframe(time, AvgReward);

        // 追加する際、既存の Keyframe が上書きされないように注意
        // ここでは単純に末尾に追加します
        bestRewardCurve.AddKey(bestKey);
        avgRewardCurve.AddKey(avgKey);

        // [補足] カーブが滑らかに見えるよう、接線（Tangent）を設定する場合もありますが、
        // まずはシンプルな AddKey で動作を確認してください。
        
        Debug.Log($"✅ Curves updated for Gen {Generation}. Best: {GenBestRecord}, Avg: {AvgReward}");
    }

    // private (Vector3 position, Quaternion rotation) GetRandomSpawnPosition()
    // {
    //     if (waypointsController == null || waypointsController.AllWaypoints.Count == 0)
    //     {
    //         // ウェイポイントが見つからない場合は、デフォルトの初期位置 (0, 0, 0) を使用
    //         return (Vector3.zero, Quaternion.identity); 
    //     }
        
    //     // ウェイポイントのリストからランダムなインデックスを選択 (スタート地点を避けるため、1から始める)
    //     // リストの要素数までを含めるため、UnityEngine.Random.Range(min, max) の max は count にする (min <= value < max)

    //     int startIndex = 1;
    //     int maxIndex = waypointsController.AllWaypoints.Count; // 36
    //     // UnityEngine.Random.Range(1, 36) は 1 から 35 を返す
    //     int randomIndex = UnityEngine.Random.Range(startIndex, maxIndex);

    //     Waypoint waypoint = waypointsController.AllWaypoints[randomIndex];
        
    //     Vector3 position = waypoint.transform.position;
        
    //     // WaypointのNextDirectionを進行方向としてQuaternionを生成
    //     // ※ Waypoint.cs に NextDirection プロパティが定義されている前提
    //     Quaternion rotation = Quaternion.LookRotation(waypoint.NextDirection); 
        
    //     // Z軸の回転を無視するためにY軸のみを回転させる (2D/平坦なレースコースの場合)
    //     // 必要に応じてコメントアウトを外す:
    //     rotation = Quaternion.Euler(0, rotation.eulerAngles.y, 0); 

    //     return (position, rotation);
    // }


    void Start() {
        // Calculate and set input size.
        int sensorCount = 0;
        foreach (bool value in selectedInputs)
        {
            if (value) sensorCount++;
        }
        InputSize = sensorCount;

        // Calculate and set sensors list.
        List<int> selectedInputsList = new List<int>();
        for (int i = 0; i < selectedInputs.Length; i++)
        {
            if (selectedInputs[i]) selectedInputsList.Add(i);
        }
        SelectedInputsList = selectedInputsList;

        sessionTimestamp = DateTime.Now.ToString("yyyyMMdd_HHmmss");  

        int startGen = 0;
        
        // LoadOnStart フラグが立っているかチェック
        if (loadOnStart)
        {
            // LoadPopulation() メソッドを実行し、復元された世代番号を取得
            startGen = LoadPopulation(); 
        }

        // ロードしない (startGen == 0)、またはロードに失敗した場合、新規に初期化
        if (startGen == 0)
        {
            // 【既存の "Initialize brain." に相当する処理】
            for(int i = 0; i < TotalPopulation; i++) {
                Brains.Add(new NNBrain(InputSize, HiddenSize, HiddenLayers, OutputSize));
            }
            Generation = 0; // 新規開始時は世代を0にリセット
        }
        else
        {
            // ロード成功時：Brains リストは既に LoadPopulation 内で復元されているため、初期化をスキップ。
            // Generation 変数も LoadPopulation 内で復元済み。
        }   

        for(int i = 0; i < NAgents; i++) {
            var obj = Instantiate(GObject);
            obj.SetActive(true);
            GObjects.Add(obj);
            Agents.Add(obj.GetComponent<Agent>());
        }
        
        foreach(Agent agent in Agents)
        {
            agent.SetAgentConfig(sensorAngleConfig);
        }

        BestRecord = -9999;
        SetStartAgents();
        if (IsChallenge4) {
            Obstacles.AddRange(FindObjectsOfType<Obstacle>());
        }

        // waypointsController = FindObjectOfType<WaypointsController>();
        // if (waypointsController == null) {
        //     Debug.LogError("❌ WaypointsController not found in the scene."); // ログがもし出たら、オブジェクト配置ミス
        // } else {
        //     Debug.Log("✅ WaypointsController found.");
        // }
    }

    void SetStartAgents() {
        CurrentBrains = new Queue<NNBrain>(Brains);
        AgentsSet.Clear();
        var size = Math.Min(NAgents, TotalPopulation);
        for(var i = 0; i < size; i++) {
            AgentsSet.Add(new AgentPair {
                agent = Agents[i],
                brain = CurrentBrains.Dequeue()
            });
        }
    }

    void FixedUpdate() {
        foreach(var pair in AgentsSet.Where(p => !p.agent.IsDone)) {
            AgentUpdate(pair.agent, pair.brain);
        }
        AgentsSet.RemoveAll(p => {
            if(p.agent.IsDone) {
                p.agent.Stop();
                p.agent.gameObject.SetActive(false);
                float r = p.agent.Reward;
                BestRecord = Mathf.Max(r, BestRecord);
                GenBestRecord = Mathf.Max(r, GenBestRecord);
                p.brain.Reward = r;
                SumReward += r;
            }
            return p.agent.IsDone;
        });

        if(CurrentBrains.Count == 0 && AgentsSet.Count == 0) {
            SetNextGeneration();
        }
        else {
            SetNextAgents();
        }
    }

    private void AgentUpdate(Agent a, NNBrain b) {
        var observation = a.GetAllObservations();
        var rearranged = RearrangeObservation(observation, SelectedInputsList);
        var action = b.GetAction(rearranged); // ネットワークの出力 (double[])
        
        // ★アクション値をクランプする処理を挿入★
        // action[0] = Steering, action[1] = Acceleration/Gas, action[2] = Brake (またはそれに相当するもの)
        
        double[] clampedAction = new double[action.Length];
        for (int i = 0; i < action.Length; i++)
        {
            // 全てのアクションを -1.0 から 1.0 の範囲に制限
            // 加速は 0.0 から 1.0、ブレーキは 0.0 から 1.0 など、アクションの物理的な意味に合わせて範囲を調整する必要があるかもしれません。
            clampedAction[i] = Mathf.Clamp((float)action[i], -1f, 1f); 
        }
        
        a.AgentAction(clampedAction, false); // クランプしたアクションを渡す
    }

    private int LoadPopulation()
    {
        if (string.IsNullOrEmpty(LoadCheckpointPath))
        {
            Debug.LogError("❌ LoadCheckpointPath is empty. Cannot load.");
            return 0;
        }

    // 1. 基本パス: /Assets/LearningData/NE/ を定義
        string basePath = Application.dataPath + $"/LearningData/NE/";

        // 2. パスを決定:
        // LoadCheckpointPath に '/' が含まれていれば、それは Stage名 からの相対パス全体とみなす。
        // 例: LoadCheckpointPath = "Stage1/Checkpoints/Stage1_GenXXX_日時.json"
        string fullPath;
        if (LoadCheckpointPath.Contains("/")) 
        {
            // 相対パス全体を使用
            fullPath = basePath + LoadCheckpointPath;
        }
        else
        {
            // 従来のパス（StageNameに依存）を使用
            fullPath = basePath + StageName + $"/{checkpointFolderName}/" + LoadCheckpointPath;
        }
        
        // ★★★★ 上記のコードブロック全体が、元のパス構築部分の置き換えです ★★★★
        
        if (!System.IO.File.Exists(fullPath))
        {
            Debug.LogError($"❌ Checkpoint file not found: {fullPath}");
            // 従来のパス構築方法が失敗した場合、StageNameを無視したパスで再チェック（念のため）
            // LoadCheckpointPathにファイル名だけが入っている可能性を考慮し、Checkpointsフォルダ以下を探す
            string fallbackPath = Application.dataPath + $"/LearningData/NE/Stage1/{checkpointFolderName}/" + LoadCheckpointPath;

            if (System.IO.File.Exists(fallbackPath)) {
                fullPath = fallbackPath;
                Debug.LogWarning($"⚠️ Found checkpoint file in Stage1 folder: {fullPath}");
            } else {
                Debug.LogError($"❌ Fallback Checkpoint file not found: {fallbackPath}");
                return 0;
            }
        }

        try
        {
            string json = System.IO.File.ReadAllText(fullPath);
            CheckpointData checkpoint = JsonUtility.FromJson<CheckpointData>(json);

            if (checkpoint.brainDataList.Count != TotalPopulation)
            {
                // 注意: ロード時の個体数と設定のTotalPopulationが異なる場合は警告
                Debug.LogWarning($"⚠️ Population size mismatch. Loaded: {checkpoint.brainDataList.Count}, Current Setting: {TotalPopulation}");
            }
            
            // 状態の復元
            Generation = checkpoint.generation;
            Brains.Clear();
            
            // 全てのNNBrainを再構築し、重みをロード
            foreach (var brainData in checkpoint.brainDataList)
            {
                // NNBrainのコンストラクタはパラメータ（InputSizeなど）で呼び出す
                var newBrain = new NNBrain(InputSize, HiddenSize, HiddenLayers, OutputSize);
                newBrain.SetBrainData(brainData); // NNBrainに実装されている前提
                newBrain.Reward = -9999; // 評価待ちにリセット
                Brains.Add(newBrain);
            }

            Debug.Log($"✅ Loaded checkpoint. Resuming from Generation: {Generation}");
            return Generation;

        }
        catch (Exception e)
        {
            Debug.LogError($"❌ Error loading checkpoint from {LoadCheckpointPath}: {e.Message}");
            return 0;
        }
    }

    private void SetNextAgents() {
        int size = Math.Min(NAgents - AgentsSet.Count, CurrentBrains.Count);
        for(var i = 0; i < size; i++) {
            var nextAgent = Agents.First(a => a.IsDone);
            var nextBrain = CurrentBrains.Dequeue();

            // ★ここから追加・修正★
            // 1. 初期位置と回転をランダムに取得
            // (Vector3 randomPos, Quaternion randomRot) = GetRandomSpawnPosition();
            
            nextAgent.Reset();
            
            // 2. エージェントのTransformを設定
            // nextAgent.transform.position = randomPos;
            // nextAgent.transform.rotation = randomRot;
            // ★ここまで追加・修正★

            AgentsSet.Add(new AgentPair {
                agent = nextAgent,
                brain = nextBrain
            });
        }
        UpdateText();
    }

    /// <summary>
    /// 学習統計データ（世代、最高報酬、平均報酬）をファイルに記録します。
    /// </summary>
    private void LogLearningStats()
    {
        // ファイルパスの設定
        // BaseSaveName を使って "CarEvolution_Stats.csv" のようなファイル名を作成
        string filename = $"{BaseSaveName}_Stats_{sessionTimestamp}.csv";
        string directoryPath = Application.dataPath + $"/LearningData/NE/{StageName}/{statsFolderName}/";
        string fullPath = directoryPath + filename;

        // フォルダが存在しない場合は作成
        if (!Directory.Exists(directoryPath))
        {
            Directory.CreateDirectory(directoryPath);
        }

        // ファイルが存在するか確認（存在しない場合はヘッダー行を書き込む）
        bool fileExists = File.Exists(fullPath);
        
        // 追記モードでファイルを開く
        using (StreamWriter sw = new StreamWriter(fullPath, true)) 
        {
            // ファイルが新規作成される場合（fileExistsがfalse）は、ヘッダーを書き込む
            if (!fileExists)
            {
                sw.WriteLine("Generation,GenerationBestReward,AverageReward");
            }
            
            // データをカンマ区切りで書き込む (ToString("F4")で小数点を整形)
            sw.WriteLine($"{Generation},{GenBestRecord.ToString("F4")},{AvgReward.ToString("F4")}");
            
            // Console にログ出力（確認用）
            Debug.Log($"📊 Stats logged for Gen {Generation}: Best={GenBestRecord}, Avg={AvgReward}");
        }
    }

    private void SetNextGeneration() {
        AvgReward = SumReward / TotalPopulation;

        // ★★★ ここで統計データをログに記録する ★★★
        LogLearningStats();
        // UpdateGraph();
        UpdateCurve();
        
        GenPopulation(); 
        SumReward = 0;
        GenBestRecord = -9999;
        Agents.ForEach(a => a.Reset());
        SetStartAgents();
        UpdateText();
    }

    private static int CompareBrains(Brain a, Brain b) {
        if(a.Reward > b.Reward) return -1;
        if(b.Reward > a.Reward) return 1;
        return 0;
    }

    private void GenPopulation() {
        var children = new List<NNBrain>();
        var bestBrains = Brains.ToList();

        // Elite selection
        bestBrains.Sort(CompareBrains);
        if(EliteSelection > 0) {
            children.AddRange(bestBrains.Take(EliteSelection));
        }

#if UNITY_EDITOR
        var path = string.Format("Assets/LearningData/NE/{0}.json", EditorSceneManager.GetActiveScene().name);
        bestBrains[0].Save(path);
#endif

        while(children.Count < TotalPopulation) {
            var tournamentMembers = Brains.AsEnumerable().OrderBy(x => Guid.NewGuid()).Take(tournamentSelection).ToList();
            tournamentMembers.Sort(CompareBrains);
            children.Add(tournamentMembers[0].Mutate(Generation));
            children.Add(tournamentMembers[1].Mutate(Generation));
        }
        Brains = children;
        Generation++;
    }

    protected List<double> RearrangeObservation(List<double> observation, List<int> indexesToUse)
    {
        if(observation == null || indexesToUse == null) return null;

        List<double> rearranged = new List<double>();
        foreach(int index in indexesToUse)
        {
            if(index >= observation.Count)
            {
                rearranged.Add(0);
                continue;
            }
            rearranged.Add(observation[index]);
        }

        return rearranged;
    }

    private void UpdateText() {
        PopulationText.text = "Population: " + (TotalPopulation - CurrentBrains.Count) + "/" + TotalPopulation
            + "\nGeneration: " + (Generation + 1)
            + "\nBest Record: " + BestRecord
            + "\nBest this gen: " + GenBestRecord
            + "\nAverage: " + AvgReward;
    }

    private struct AgentPair
    {
        public NNBrain brain;
        public Agent agent;
    }
}
