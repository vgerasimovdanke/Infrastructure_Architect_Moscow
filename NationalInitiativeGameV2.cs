using UnityEngine;
using UnityEngine.SceneManagement;
using System.Collections.Generic;
using System.Collections;

public class NationalInitiativeGame : MonoBehaviour
{
    [System.Serializable]
    public class GameTask {
        public string name;
        public float cost;
        public float reward;
        public int workersReward;
        public float baseDuration;
        public Color taskColor;
        public float deadline;

        [HideInInspector] public float remainingTime;
        [HideInInspector] public int workersAssigned = 0;
        [HideInInspector] public bool isCompleted;
        [HideInInspector] public bool isFailed;
        [HideInInspector] public GameObject visualModel;
        [HideInInspector] public GameObject constructionEffect;
        [HideInInspector] public float towerHeight;
    }

    public float budget = 330000f;
    public int totalWorkers = 2;
    public int maxAllowedFailures = 2;

    private int availableWorkers;
    private int failedTasks = 0;
    private float elapsedTime = 0f;
    private bool gameOver = false;
    private string gameOverReason = "";
    private bool gameStarted = false;

    public List<GameTask> tasks = new List<GameTask>();
    private GameObject playerManager;
    private Material safeMat;
    private Texture2D darkOverlayTex;
    private List<(Vector2 pos, string text, float time)> completionPopups = new List<(Vector2, string, float)>();
    private const float foundationHeight = 1.8f;

    void Start() {
        totalWorkers = 2;
        availableWorkers = totalWorkers;

        Camera cam = Camera.main;
        if (cam == null) {
            GameObject camObj = new GameObject("Main Camera");
            cam = camObj.AddComponent<Camera>();
            camObj.AddComponent<AudioListener>();
            camObj.tag = "MainCamera";
        }
        cam.farClipPlane = 1000f;
        cam.transform.position = new Vector3(0, 38f, -45f); 
        cam.transform.rotation = Quaternion.Euler(40f, 0, 0);
        cam.backgroundColor = new Color(0.01f, 0.01f, 0.02f);

        GameObject l = new GameObject("Sun");
        l.AddComponent<Light>().type = LightType.Directional;
        l.transform.rotation = Quaternion.Euler(50, -30, 0);
        l.GetComponent<Light>().intensity = 0.4f;

        GameObject temp = GameObject.CreatePrimitive(PrimitiveType.Cube);
        safeMat = new Material(temp.GetComponent<Renderer>().sharedMaterial);
        Destroy(temp);

        // Основная подложка города (Тротуар)
        GameObject ground = GameObject.CreatePrimitive(PrimitiveType.Plane);
        ground.transform.localScale = new Vector3(100, 1, 100);
        SetColor(ground, new Color(0.06f, 0.06f, 0.08f));

        BuildStrictCity();
        CreateRain();
        InitTasks();
        
        playerManager = GameObject.CreatePrimitive(PrimitiveType.Capsule);
        playerManager.transform.position = new Vector3(0, 1.2f, -18f);
        SetColor(playerManager, Color.white, true);
        CreateLabel(playerManager, "РУКОВОДИТЕЛЬ ПТО", Color.white, 2.8f, 90, 0.2f);

        PlaceParkGreenery();

        InvokeRepeating("SpawnTraffic", 1f, 3.5f);
    }

    bool IsOnRoad(float x, float z) {
        float vertHalf = 6f;
        float horizHalf = 5f;
        if (Mathf.Abs(x + 25f) <= vertHalf || Mathf.Abs(x - 25f) <= vertHalf) return true;
        if (Mathf.Abs(z + 40f) <= horizHalf || Mathf.Abs(z) <= horizHalf || Mathf.Abs(z - 40f) <= horizHalf || Mathf.Abs(z - 80f) <= horizHalf) return true;
        return false;
    }

    void PlaceParkGreenery() {
        Vector3[] spots = new Vector3[] {
            new Vector3(-8f, 0f, -26f), new Vector3(8f, 0f, -26f),
            new Vector3(-20f, 0f, -12f), new Vector3(20f, 0f, -12f),
            new Vector3(-6f, 0f, -8f), new Vector3(6f, 0f, -8f)
        };
        float treeScale = 1.45f;
        foreach (Vector3 spot in spots) {
            Vector3 p = spot + new Vector3(Random.Range(-2f, 2f), 0f, Random.Range(-2f, 2f));
            if (IsOnRoad(p.x, p.z)) continue;
            if (Random.value < 0.6f) {
                CreateTree(p, treeScale);
            } else {
                CreateBush(p, treeScale);
            }
        }
    }

    void CreateTree(Vector3 basePos, float scale) {
        GameObject trunk = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
        trunk.transform.position = basePos + Vector3.up * 1.5f;
        trunk.transform.localScale = new Vector3(0.55f * scale, 1.5f, 0.55f * scale);
        SetColor(trunk, new Color(0.35f, 0.22f, 0.15f));
        float crownY = basePos.y + 4.2f;
        Color green = new Color(0.12f, 0.45f, 0.18f);
        float baseR = 1.1f * scale;
        for (int i = 0; i < 5; i++) {
            GameObject part = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            float ox = (Random.value - 0.5f) * 1.4f * scale;
            float oz = (Random.value - 0.5f) * 1.4f * scale;
            float oy = (Random.value - 0.5f) * 0.8f * scale;
            part.transform.position = new Vector3(basePos.x + ox, crownY + oy, basePos.z + oz);
            float s = baseR * (0.7f + Random.value * 0.6f);
            part.transform.localScale = new Vector3(s, s * 1.15f, s);
            SetColor(part, green);
        }
    }

    void CreateBush(Vector3 basePos, float scale) {
        float by = basePos.y + 1.2f;
        Color green = new Color(0.15f, 0.5f, 0.2f);
        for (int i = 0; i < 3; i++) {
            GameObject part = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            part.transform.position = new Vector3(basePos.x + (Random.value - 0.5f) * 1.2f, by + (Random.value - 0.5f) * 0.4f, basePos.z + (Random.value - 0.5f) * 1.2f);
            float s = (0.9f + Random.value * 0.7f) * scale;
            part.transform.localScale = new Vector3(s, s * 0.85f, s);
            SetColor(part, green);
        }
    }

    void BuildStrictCity() {
        // Вертикальные дороги
        CreateRoad(new Vector3(-25f, 0.05f, 50f), new Vector3(10f, 0.15f, 300f), true);
        CreateRoad(new Vector3(25f, 0.05f, 50f), new Vector3(10f, 0.15f, 300f), true);

        // Горизонтальные дороги (шаг 40)
        for (int z = -40; z <= 120; z += 40) {
            CreateRoad(new Vector3(0, 0.05f, z), new Vector3(300f, 0.15f, 8f), false);
        }

        // Фонари на углах блоков, за пределами проезжей части (отступ от края дороги)
        float[] roadZ = new float[] { -40f, 0f, 40f, 80f };
        float[] roadX = new float[] { -25f, 25f };
        float lampOffset = 7f;
        foreach (float rx in roadX) {
            foreach (float rz in roadZ) {
                float sx = rx < 0 ? -lampOffset : lampOffset;
                float sz = lampOffset;
                CreateLampPost(new Vector3(rx + sx, 0, rz - sz));
                CreateLampPost(new Vector3(rx + sx, 0, rz + sz));
            }
        }
        CreateLampPost(new Vector3(-25f - lampOffset, 0f, -20f));
        CreateLampPost(new Vector3( 25f + lampOffset, 0f, -20f));

        // Фоновые здания
        for (int x = -90; x <= 90; x += 18) {
            for (int z = -30; z <= 150; z += 18) {
                if (Mathf.Abs(x) < 35 && z < 60) continue;

                // Отступ домов от дорог: не вплотную (учёт ширины дороги и здания ~12)
                bool nearVerticalRoad = Mathf.Abs(x + 25f) < 11f || Mathf.Abs(x - 25f) < 11f;
                bool nearHorizontalRoad =
                    Mathf.Abs(z + 40f) < 10f ||
                    Mathf.Abs(z - 0f)  < 10f ||
                    Mathf.Abs(z - 40f) < 10f ||
                    Mathf.Abs(z - 80f) < 10f;
                if (nearVerticalRoad || nearHorizontalRoad) continue;

                float h = Random.Range(15f, 40f);
                GameObject b = GameObject.CreatePrimitive(PrimitiveType.Cube);
                b.transform.position = new Vector3(x, h/2, z);
                b.transform.localScale = new Vector3(12f, h, 12f);
                Color buildingTint = new Color(0.1f, 0.1f, 0.12f);
                int tintType = Random.Range(0, 4);
                if (tintType == 1) buildingTint = Color.Lerp(buildingTint, new Color(0.08f, 0.12f, 0.18f), 0.4f);
                else if (tintType == 2) buildingTint = Color.Lerp(buildingTint, new Color(0.14f, 0.11f, 0.1f), 0.35f);
                else if (tintType == 3) buildingTint = Color.Lerp(buildingTint, new Color(0.1f, 0.14f, 0.14f), 0.3f);
                SetColor(b, buildingTint);
                AddWindows(b, h);
            }
        }

        PlaceGreenery();
    }

    void PlaceGreenery() {
        float treeScale = 1.5f;
        int count = 45;
        for (int i = 0; i < count; i++) {
            float px = Random.Range(-85f, 85f);
            float pz = Random.Range(-25f, 148f);
            if (Mathf.Abs(px) < 40 && pz < 65) continue;
            if (IsOnRoad(px, pz)) continue;
            Vector3 spot = new Vector3(px, 0f, pz);
            if (Random.value < 0.55f)
                CreateTree(spot, treeScale);
            else
                CreateBush(spot, treeScale);
        }
    }

    readonly float[] intersectionZ = new float[] { -40f, 0f, 40f, 80f, 120f };
    readonly float[] intersectionX = new float[] { -25f, 25f };
    const float intersectionGapZ = 5.5f;
    const float intersectionGapX = 6.5f;

    bool InIntersectionZoneZ(float worldZ) {
        for (int i = 0; i < intersectionZ.Length; i++)
            if (Mathf.Abs(worldZ - intersectionZ[i]) < intersectionGapZ) return true;
        return false;
    }
    bool InIntersectionZoneX(float worldX) {
        for (int i = 0; i < intersectionX.Length; i++)
            if (Mathf.Abs(worldX - intersectionX[i]) < intersectionGapX) return true;
        return false;
    }

    void CreateRoad(Vector3 pos, Vector3 scale, bool isVertical) {
        GameObject r = GameObject.CreatePrimitive(PrimitiveType.Cube);
        r.transform.position = pos;
        r.transform.localScale = scale;
        SetColor(r, new Color(0.02f, 0.02f, 0.03f));

        float y = pos.y + 0.01f;
        float segLen = 4f;
        float gap = 2.5f;
        Color edgeColor = new Color(0.38f, 0.38f, 0.4f);
        if (isVertical) {
            float hw = scale.x * 0.5f;
            CreateDashedLineVertical(pos.x, y, pos.z, scale.z, 0.25f, 0.2f, segLen, gap, new Color(0.95f, 0.9f, 0.3f));
            CreateVerticalEdgeLines(pos.x - hw + 0.15f, y, pos.z, scale.z, 0.2f, 0.16f, edgeColor);
            CreateVerticalEdgeLines(pos.x + hw - 0.15f, y, pos.z, scale.z, 0.2f, 0.16f, edgeColor);
        } else {
            float hw = scale.z * 0.5f;
            CreateDashedLineHorizontal(pos.x, y, pos.z, scale.x, 0.25f, 0.2f, segLen, gap, new Color(0.95f, 0.9f, 0.3f));
            CreateHorizontalEdgeLines(pos.x, y, pos.z - hw + 0.15f, scale.x, 0.16f, 0.2f, edgeColor);
            CreateHorizontalEdgeLines(pos.x, y, pos.z + hw - 0.15f, scale.x, 0.16f, 0.2f, edgeColor);
        }
    }

    void CreateVerticalEdgeLines(float x, float y, float zCenter, float totalLen, float widthX, float widthZ, Color c) {
        float half = totalLen * 0.5f;
        float zStart = zCenter - half;
        float zEnd = zCenter + half;
        float segStart = -1000f;
        float step = 0.4f;
        for (float z = zStart; z <= zEnd + step; z += step) {
            bool inZone = z <= zEnd && InIntersectionZoneZ(z);
            if (inZone) {
                if (segStart > -500f) {
                    float segLen = z - segStart;
                    if (segLen > 0.1f)
                        CreateRoadLine(new Vector3(x, y, segStart + segLen * 0.5f), new Vector3(widthX, widthZ, segLen), c);
                    segStart = -1000f;
                }
            } else {
                if (segStart < -500f) segStart = z;
            }
        }
        if (segStart > -500f && segStart < zEnd - 0.1f)
            CreateRoadLine(new Vector3(x, y, segStart + (zEnd - segStart) * 0.5f), new Vector3(widthX, widthZ, zEnd - segStart), c);
    }

    void CreateHorizontalEdgeLines(float xCenter, float y, float z, float totalLen, float widthX, float widthZ, Color c) {
        float half = totalLen * 0.5f;
        float xStart = xCenter - half;
        float xEnd = xCenter + half;
        float segStart = -1000f;
        float step = 0.4f;
        for (float x = xStart; x <= xEnd + step; x += step) {
            bool inZone = x <= xEnd && InIntersectionZoneX(x);
            if (inZone) {
                if (segStart > -500f) {
                    float segLen = x - segStart;
                    if (segLen > 0.1f)
                        CreateRoadLine(new Vector3(segStart + segLen * 0.5f, y, z), new Vector3(segLen, widthX, widthZ), c);
                    segStart = -1000f;
                }
            } else {
                if (segStart < -500f) segStart = x;
            }
        }
        if (segStart > -500f && segStart < xEnd - 0.1f)
            CreateRoadLine(new Vector3(segStart + (xEnd - segStart) * 0.5f, y, z), new Vector3(xEnd - segStart, widthX, widthZ), c);
    }

    void CreateDashedLineVertical(float x, float y, float zCenter, float totalLen, float widthX, float widthZ, float segLen, float gap, Color c) {
        float half = totalLen * 0.5f;
        float step = segLen + gap;
        for (float z = -half + segLen * 0.5f; z < half; z += step) {
            float worldZ = zCenter + z;
            if (InIntersectionZoneZ(worldZ)) continue;
            CreateRoadLine(new Vector3(x, y, worldZ), new Vector3(widthX, 0.16f, segLen), c);
        }
    }

    void CreateDashedLineHorizontal(float xCenter, float y, float z, float totalLen, float widthX, float widthZ, float segLen, float gap, Color c) {
        float half = totalLen * 0.5f;
        float step = segLen + gap;
        for (float x = -half + segLen * 0.5f; x < half; x += step) {
            float worldX = xCenter + x;
            if (InIntersectionZoneX(worldX)) continue;
            CreateRoadLine(new Vector3(worldX, y, z), new Vector3(segLen, 0.16f, widthZ), c);
        }
    }

    void CreateRoadLine(Vector3 pos, Vector3 scale, Color c) {
        GameObject line = GameObject.CreatePrimitive(PrimitiveType.Cube);
        line.transform.position = pos;
        line.transform.localScale = scale;
        SetColor(line, c);
    }

    void AddWindows(GameObject building, float height) {
        float density = 0.45f + Random.Range(0f, 0.25f);
        float warm = Random.Range(0f, 0.4f);
        Color windowColor = Color.Lerp(new Color(0.9f, 0.8f, 0.5f), new Color(1f, 0.75f, 0.45f), warm);
        int added = 0;
        for (float y = 3f; y < height - 2f; y += 3.5f) {
            if (Random.value < density) {
                AddOneWindow(building, height, y, windowColor);
                added++;
            }
        }
        while (added < 2) {
            float y = Random.Range(4f, height - 3f);
            AddOneWindow(building, height, y, windowColor);
            added++;
        }
    }

    void AddOneWindow(GameObject building, float height, float y, Color windowColor) {
        GameObject w = GameObject.CreatePrimitive(PrimitiveType.Quad);
        w.transform.SetParent(building.transform);
        w.transform.localPosition = new Vector3(0, (y / height) - 0.5f, -0.51f);
        w.transform.localScale = new Vector3(0.6f, 1.5f / height, 1f);
        SetColor(w, windowColor, true);
        Destroy(w.GetComponent<BoxCollider>());
    }

    void CreateLampPost(Vector3 pos) {
        GameObject baseObj = new GameObject("Lamp");
        baseObj.transform.position = pos;
        GameObject post = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
        post.transform.SetParent(baseObj.transform);
        post.transform.localPosition = Vector3.up * 4.5f;
        post.transform.localScale = new Vector3(0.2f, 4.5f, 0.2f);
        SetColor(post, Color.black);
        GameObject head = GameObject.CreatePrimitive(PrimitiveType.Sphere);
        head.transform.SetParent(baseObj.transform);
        head.transform.localPosition = Vector3.up * 9.5f;
        head.transform.localScale = Vector3.one * 1.5f;
        SetColor(head, new Color(1f, 0.9f, 0.6f), true);

        GameObject lightObj = new GameObject("LampLight");
        lightObj.transform.SetParent(baseObj.transform);
        lightObj.transform.localPosition = Vector3.up * 9f;
        Light light = lightObj.AddComponent<Light>();
        light.type = LightType.Point;
        light.color = new Color(1f, 0.95f, 0.8f);
        light.intensity = 0.8f;
        light.range = 12f;
    }

    void CreateRain() {
        GameObject rp = new GameObject("Rain");
        for (int i = 0; i < 150; i++) {
            GameObject r = GameObject.CreatePrimitive(PrimitiveType.Cube);
            r.transform.SetParent(rp.transform);
            r.transform.position = new Vector3(Random.Range(-100, 100), Random.Range(20, 50), Random.Range(-50, 150));
            r.transform.localScale = new Vector3(0.05f, 1.5f, 0.05f);
            SetColor(r, new Color(0.5f, 0.6f, 1f, 0.2f));
            Destroy(r.GetComponent<BoxCollider>());
            StartCoroutine(AnimateRain(r));
        }
    }

    IEnumerator AnimateRain(GameObject r) {
        while(true) {
            r.transform.Translate(Vector3.down * 40f * Time.deltaTime + Vector3.right * 3f * Time.deltaTime);
            if (r.transform.position.y < 0) r.transform.position = new Vector3(Random.Range(-100, 100), 50f, Random.Range(-50, 150));
            yield return null;
        }
    }

    void InitTasks() {
        tasks.Clear();

        tasks.Add(new GameTask { name = "Ул. Ягодная — объект А", cost = 36000, reward = 95000, workersReward = 2, baseDuration = 11f, deadline = 28f, taskColor = Color.green });
        tasks.Add(new GameTask { name = "Ул. Ягодная — объект Б", cost = 42000, reward = 108000, workersReward = 2, baseDuration = 13f, deadline = 40f, taskColor = Color.cyan });
        tasks.Add(new GameTask { name = "Цифровой двойник", cost = 56000, reward = 172000, workersReward = 3, baseDuration = 17f, deadline = 56f, taskColor = new Color(1f, 0.7f, 0.2f) });
        tasks.Add(new GameTask { name = "VR-симуляция", cost = 92000, reward = 265000, workersReward = 3, baseDuration = 23f, deadline = 74f, taskColor = new Color(1f, 0.1f, 0.6f) });
        tasks.Add(new GameTask { name = "ПТО: модернизация ПО", cost = 68000, reward = 200000, workersReward = 3, baseDuration = 19f, deadline = 64f, taskColor = new Color(0.4f, 0.8f, 1f) });

        // Ручные позиции по X/Z: два объекта ближе к руководителю (между дорогами -40 и 0),
        // три — дальше (между 0 и 40); высоты — треугольником (Цифровой двойник и ПТО выше, VR чуть ниже)
        Vector3[] positions = new Vector3[] {
            new Vector3(-15f, 0f, -15f), // Ул. Ягодная — объект А (передний левый)
            new Vector3( 15f, 0f, -15f), // Ул. Ягодная — объект Б (передний правый)
            new Vector3(-13f, 0f,  10f), // Цифровой двойник (задний левый, ближе к камере)
            new Vector3(  0f, 0f,  15f), // VR-симуляция (центр, чуть глубже)
            new Vector3( 13f, 0f,  10f)  // ПТО: модернизация ПО (задний правый)
        };

        float[] heights = new float[] { 18f, 18f, 20f, 17f, 20f };

        for (int i = 0; i < tasks.Count; i++) {
            float h = heights[i];
            Vector3 pos = positions[i];

            GameObject tower = new GameObject("Tower");
            tower.transform.position = new Vector3(pos.x, 0f, pos.z);

            // Фундамент — отдельная форма (низкий цилиндр)
            GameObject foundation = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            foundation.name = "Foundation";
            foundation.transform.SetParent(tower.transform);
            foundation.transform.localPosition = new Vector3(0, foundationHeight * 0.5f, 0);
            foundation.transform.localScale = new Vector3(5f, foundationHeight * 0.5f, 5f);
            SetColor(foundation, new Color(0.22f, 0.2f, 0.18f));

            // Основное здание — один объём с поясами, как раньше (рост по высоте)
            GameObject building = GameObject.CreatePrimitive(PrimitiveType.Cube);
            building.name = "Building";
            building.transform.SetParent(tower.transform);
            building.transform.localScale = new Vector3(8f, 0.01f, 8f);
            building.transform.localPosition = new Vector3(0, foundationHeight + 0.005f, 0);
            SetColor(building, new Color(0.15f, 0.15f, 0.2f));
            for (float band = -0.4f; band < 0.5f; band += 0.25f) {
                GameObject bandObj = GameObject.CreatePrimitive(PrimitiveType.Cube);
                bandObj.transform.SetParent(building.transform);
                bandObj.transform.localPosition = new Vector3(0, band, 0);
                bandObj.transform.localScale = new Vector3(1.05f, 0.05f, 1.05f);
                SetColor(bandObj, Color.black);
            }

            tasks[i].visualModel = tower;
            tasks[i].remainingTime = tasks[i].baseDuration;
            tasks[i].towerHeight = h;
            tasks[i].constructionEffect = null;

            CreateFoundationLabel(tower, tasks[i].name);
        }
    }

    void CreateFoundationLabel(GameObject tower, string text) {
        GameObject o = new GameObject("FoundationLabel");
        o.transform.SetParent(tower.transform);
        o.transform.localPosition = new Vector3(0, foundationHeight + 2f, 5.8f);
        o.transform.localRotation = Quaternion.identity;
        o.transform.localScale = Vector3.one;
        TextMesh m = o.AddComponent<TextMesh>();
        m.text = text;
        m.fontSize = 85;
        m.characterSize = 0.22f;
        m.color = new Color(0.95f, 0.95f, 0.9f);
        m.anchor = TextAnchor.MiddleCenter;
        m.fontStyle = FontStyle.Bold;
        m.alignment = TextAlignment.Center;
    }

    void Update() {
        if (!gameStarted || gameOver) return;

        elapsedTime += Time.deltaTime;

        foreach (var t in tasks) {
            if (t.isCompleted || t.isFailed) continue;

            if (elapsedTime > t.deadline) {
                t.isFailed = true;
                failedTasks++;
                UpdateTowerGrowth(t, 1f, false);
                SetTowerCompleted(t.visualModel, Color.red);
                continue;
            }

            if (t.workersAssigned > 0) {
                t.remainingTime -= Time.deltaTime * t.workersAssigned;
                if (t.remainingTime <= 0f) {
                    t.isCompleted = true;
                    budget += t.reward;
                    availableWorkers += t.workersReward;
                    totalWorkers += t.workersReward;
                    UpdateTowerGrowth(t, 1f, false);
                    SetTowerCompleted(t.visualModel, t.taskColor);
                    Vector3 worldPos = t.visualModel.transform.position + Vector3.up * (foundationHeight + t.towerHeight + 3f);
                    Vector2 screen = Camera.main.WorldToScreenPoint(worldPos);
                    screen.y = Screen.height - screen.y;
                    completionPopups.Add((screen, $"+{t.reward:F0} р.  +{t.workersReward} бригад", Time.time));
                }
            }
            if (!t.isCompleted && !t.isFailed) {
                float progress = t.workersAssigned > 0 ? (1f - t.remainingTime / t.baseDuration) : 0f;
                UpdateTowerGrowth(t, progress, t.workersAssigned > 0);
            }
        }

        if (budget < 0f) {
            EndGame("Бюджет ушёл в минус. Проект провален.");
        } else if (failedTasks >= maxAllowedFailures) {
            EndGame("Слишком много просроченных объектов.");
        } else if (AllTasksResolved()) {
            EndGame("Все объекты обработаны. Практика завершена.");
        }
    }

    bool AllTasksResolved() {
        foreach (var t in tasks) {
            if (!t.isCompleted && !t.isFailed) return false;
        }
        return true;
    }

    void EndGame(string reason) {
        gameOver = true;
        gameOverReason = reason;
    }

    void SpawnTraffic() {
        bool left = Random.value > 0.5f;
        float roadX = left ? -25f : 25f;
        float laneOffset = -2.8f;
        GameObject car = GameObject.CreatePrimitive(PrimitiveType.Cube);
        car.transform.position = new Vector3(roadX + laneOffset, 0.8f, left ? -80f : 150f);
        car.transform.localScale = new Vector3(2.5f, 1.2f, 5f);
        SetColor(car, new Color(0.2f, 0.2f, 0.5f), true);
        if (!left) car.transform.rotation = Quaternion.Euler(0, 180, 0);
        StartCoroutine(Drive(car, roadX + laneOffset));
    }

    IEnumerator Drive(GameObject c, float laneX) {
        while (c != null) {
            Vector3 pos = c.transform.position;
            c.transform.position = new Vector3(laneX, pos.y, pos.z + (c.transform.rotation.eulerAngles.y > 90f ? -35f : 35f) * Time.deltaTime);
            if (Mathf.Abs(c.transform.position.z) > 180) break;
            yield return null;
        }
        if (c != null) Destroy(c);
    }

    IEnumerator SendWorker(GameTask task) {
        if (task.workersAssigned == 0) budget -= task.cost;
        task.workersAssigned++; availableWorkers--;
        GameObject w = GameObject.CreatePrimitive(PrimitiveType.Capsule);
        w.transform.localScale = Vector3.one * 0.8f;
        SetColor(w, Color.yellow, true);
        Vector3 home = playerManager.transform.position;
        Vector3 work = task.visualModel.transform.position + new Vector3(Random.Range(-2f, 2f), foundationHeight + 0.5f, -6f);
        float s = 0;
        while (s < 1f) { s += Time.deltaTime * 1.5f; w.transform.position = Vector3.Lerp(home, work, s) + Vector3.up * Mathf.Abs(Mathf.Sin(s * 20f)) * 0.8f; yield return null; }
        while (!task.isCompleted && !task.isFailed) { w.transform.position = work + Vector3.up * (1f + Mathf.PingPong(Time.time * 4, 0.5f)); yield return null; }
        s = 0;
        while (s < 1f) { s += Time.deltaTime * 1.5f; w.transform.position = Vector3.Lerp(work, home, s) + Vector3.up * Mathf.Abs(Mathf.Sin(s * 20f)) * 0.8f; yield return null; }
        Destroy(w); availableWorkers++;
    }

    void SetColor(GameObject o, Color c, bool em = false) {
        Renderer r = o.GetComponent<Renderer>();
        r.material = new Material(safeMat);
        r.material.color = c;
        if (em) { r.material.EnableKeyword("_EMISSION"); r.material.SetColor("_EmissionColor", c * 3.5f); }
    }

    void UpdateTowerGrowth(GameTask t, float progress, bool showSkeleton) {
        Transform tower = t.visualModel.transform;
        Transform building = tower.Find("Building");
        if (building == null) return;

        float h = t.towerHeight;
        float buildH = Mathf.Max(0.01f, progress * h);
        building.localScale = new Vector3(8f, buildH, 8f);
        building.localPosition = new Vector3(0, foundationHeight + buildH * 0.5f, 0);

        Transform label = tower.Find("FoundationLabel");
        if (label != null)
            label.localPosition = new Vector3(0, foundationHeight + buildH + 2f, 5.8f);
    }

    void SetTowerCompleted(GameObject tower, Color accentColor) {
        foreach (Transform child in tower.transform) {
            TextMesh label = child.GetComponent<TextMesh>();
            if (label != null) {
                label.color = accentColor;
                continue;
            }
            if (child.name == "Foundation" || child.name == "FoundationLabel") continue;

            if (child.name == "Building") {
                Renderer r = child.gameObject.GetComponent<Renderer>();
                if (r != null) {
                    r.material = new Material(safeMat);
                    r.material.color = Color.Lerp(accentColor, new Color(0.12f, 0.12f, 0.15f), 0.9f);
                }
                foreach (Transform sub in child) {
                    Renderer sr = sub.GetComponent<Renderer>();
                    if (sr != null) {
                        sr.material = new Material(safeMat);
                        sr.material.color = accentColor;
                        sr.material.EnableKeyword("_EMISSION");
                        sr.material.SetColor("_EmissionColor", accentColor * 0.5f);
                    }
                }
                continue;
            }
            Renderer r2 = child.gameObject.GetComponent<Renderer>();
            if (r2 != null) {
                r2.material = new Material(safeMat);
                r2.material.color = accentColor;
                r2.material.EnableKeyword("_EMISSION");
                r2.material.SetColor("_EmissionColor", accentColor * 0.4f);
            }
        }
    }

    void OnGUI() {
        if (!gameStarted) {
            DrawStartScreen();
            return;
        }

        GUI.backgroundColor = new Color(0, 0, 0, 0.95f);
        Rect p = new Rect(25, Screen.height - 260, Screen.width - 50, 240);
        GUI.Box(p, "");

        GUIStyle header = new GUIStyle(GUI.skin.label) { fontSize = 28, fontStyle = FontStyle.Bold };
        GUIStyle sub = new GUIStyle(GUI.skin.label) { fontSize = 20 };

        float headerW = p.width - 80;
        GUI.Label(new Rect(p.x + 40, p.y + 20, headerW - 320, 40),
            $"БАЛАНС: {budget:F0} Р. | БРИГАДЫ: {availableWorkers} / {totalWorkers}", header);
        if (!gameOver && availableWorkers == 0) {
            GUIStyle noWorkers = new GUIStyle(GUI.skin.label) { fontSize = 20, fontStyle = FontStyle.Bold };
            noWorkers.normal.textColor = new Color(1f, 0.85f, 0.4f);
            GUI.Label(new Rect(p.x + headerW - 300, p.y + 20, 300, 40), "Нет свободных бригад", noWorkers);
        }

        GUI.Label(new Rect(p.x + 40, p.y + 60, 800, 30),
            $"ВРЕМЯ: {elapsedTime:F1} c | ПРОСРОЧЕНО: {failedTasks} / {maxAllowedFailures}", sub);

        float btnW = (p.width - 120) / Mathf.Max(1, tasks.Count);
        float btnH = 120f;

        GUIStyle btnTask = new GUIStyle(GUI.skin.button) {
            fontSize = 18, fontStyle = FontStyle.Bold, wordWrap = true, alignment = TextAnchor.MiddleCenter
        };

        for (int i = 0; i < tasks.Count; i++) {
            var t = tasks[i];
            Rect r = new Rect(p.x + 40 + i * (btnW + 10), p.y + 100, btnW, btnH);

            if (t.isCompleted) {
                GUI.backgroundColor = t.taskColor;
                string completedTxt = t.workersReward > 0
                    ? $"ОБЪЕКТ СДАН\n+{t.reward} р.  +{t.workersReward} бригад"
                    : $"ОБЪЕКТ СДАН\n+{t.reward} р.";
                GUI.Button(r, completedTxt,
                    new GUIStyle(GUI.skin.button) { fontSize = 18, fontStyle = FontStyle.Bold });
            } else if (t.isFailed) {
                GUI.backgroundColor = Color.red;
                GUI.Button(r, $"ПРОСРОЧЕНО\nдедлайн: {t.deadline:F0} c",
                    new GUIStyle(GUI.skin.button) { fontSize = 18, fontStyle = FontStyle.Bold });
            } else {
                GUI.backgroundColor = t.workersAssigned > 0 ? Color.yellow : Color.gray;
                string txt;
                if (t.workersAssigned > 0) {
                    float est = Mathf.Max(0.1f, t.remainingTime / t.workersAssigned);
                    txt = $"В РАБОТЕ\n≈ {est:F1} c\nдедлайн: {t.deadline:F0} c";
                } else {
                    txt = $"{t.name}\n{t.cost:F0} р\nдедлайн: {t.deadline:F0} c";
                }
                if (!gameOver && availableWorkers > 0 && GUI.Button(r, txt, btnTask))
                    StartCoroutine(SendWorker(t));
            }
        }

        completionPopups.RemoveAll(pp => Time.time - pp.time > 3f);
        foreach (var pp in completionPopups) {
            float age = Time.time - pp.time;
            float alpha = age < 0.5f ? 1f : (age > 2.5f ? 1f - (age - 2.5f) / 0.5f : 1f);
            GUIStyle popStyle = new GUIStyle(GUI.skin.label) { fontSize = 20, fontStyle = FontStyle.Bold, alignment = TextAnchor.MiddleCenter };
            popStyle.normal.textColor = new Color(0.2f, 1f, 0.4f, alpha);
            Rect popR = new Rect(pp.pos.x - 120, pp.pos.y - 15 - age * 8f, 240, 30);
            GUI.Label(popR, pp.text, popStyle);
        }

        if (gameOver) {
            DrawGameOverScreen();
        }
    }

    void DrawStartScreen() {
        float w = Screen.width;
        float h = Screen.height;

        if (darkOverlayTex == null) {
            darkOverlayTex = new Texture2D(1, 1);
            darkOverlayTex.SetPixel(0, 0, new Color(0f, 0f, 0f, 0.88f));
            darkOverlayTex.Apply();
        }
        GUI.DrawTexture(new Rect(0, 0, w, h), darkOverlayTex);

        int titleFont = Mathf.RoundToInt(82 * 1.25f);
        int subFont = Mathf.RoundToInt(46 * 1.25f);
        int instrFont = Mathf.RoundToInt(40 * 1.25f);
        GUIStyle titleStyle = new GUIStyle(GUI.skin.label) {
            fontSize = titleFont, fontStyle = FontStyle.Bold, alignment = TextAnchor.MiddleCenter
        };
        titleStyle.normal.textColor = Color.white;
        GUI.Label(new Rect(0, h * 0.03f, w, 130), "Infrastructure Architect", titleStyle);
        GUIStyle subStyle = new GUIStyle(GUI.skin.label) { fontSize = subFont, alignment = TextAnchor.MiddleCenter };
        subStyle.normal.textColor = Color.white;
        GUI.Label(new Rect(0, h * 0.03f + 120, w, 72), "Управление объектами ПТО", subStyle);

        string instruction =
            "Вы — руководитель ПТО. Распределяйте ограниченное число бригад по объектам.\n\n" +
            "• У каждого объекта есть стоимость запуска, награда за сдачу и дедлайн в секундах. За сданный объект вы получаете деньги и дополнительно бригады — их выгодно сдавать раньше.\n" +
            "• Если не успеть сдать объект до дедлайна — он считается просроченным. Допускается не более 2 просрочек.\n" +
            "• Бюджет не должен уйти в минус. Цель — сдать все объекты в срок или завершить без двух просрочек.";
        GUIStyle instrStyle = new GUIStyle(GUI.skin.label) {
            fontSize = instrFont, alignment = TextAnchor.UpperLeft, wordWrap = true
        };
        instrStyle.normal.textColor = new Color(0.92f, 0.92f, 0.92f);
        float instrW = w * 0.62f;
        Rect instrRect = new Rect((w - instrW) * 0.5f, h * 0.18f, instrW, h * 0.50f);
        GUI.Label(instrRect, instruction, instrStyle);

        GUIStyle btnStyle = new GUIStyle(GUI.skin.button) { fontSize = 48, fontStyle = FontStyle.Bold };
        float btnW2 = w * 0.7f;
        float btnH2 = 102f;
        Rect btnRect = new Rect((w - btnW2) * 0.5f, h * 0.72f, btnW2, btnH2);
        GUI.backgroundColor = new Color(0.2f, 0.75f, 0.25f);
        if (GUI.Button(btnRect, "ИГРАТЬ", btnStyle)) {
            gameStarted = true;
        }
    }

    void DrawGameOverScreen() {
        float w = Screen.width;
        float h = Screen.height;
        if (darkOverlayTex == null) {
            darkOverlayTex = new Texture2D(1, 1);
            darkOverlayTex.SetPixel(0, 0, new Color(0f, 0f, 0f, 0.88f));
            darkOverlayTex.Apply();
        }
        GUI.DrawTexture(new Rect(0, 0, w, h), darkOverlayTex);

        GUIStyle over = new GUIStyle(GUI.skin.label) {
            fontSize = 42, fontStyle = FontStyle.Bold, alignment = TextAnchor.MiddleCenter
        };
        over.normal.textColor = Color.white;
        GUI.Label(new Rect(0, h * 0.30f, w, 140), gameOverReason, over);

        GUIStyle btnStyle = new GUIStyle(GUI.skin.button) { fontSize = 34, fontStyle = FontStyle.Bold };
        Rect btnRect = new Rect(w * 0.22f, h * 0.52f, w * 0.56f, 70);
        if (GUI.Button(btnRect, "Перезапустить миссию", btnStyle)) {
            SceneManager.LoadScene(SceneManager.GetActiveScene().name);
        }
    }

    void CreateLabel(GameObject p, string t, float y) {
        CreateLabel(p, t, Color.white, y, 70, 0.18f);
    }

    void CreateLabel(GameObject p, string t, Color c, float y, int fontSize, float charSize) {
        GameObject o = new GameObject("Label");
        o.transform.position = p.transform.position + Vector3.up * y;
        o.transform.SetParent(p.transform);
        TextMesh m = o.AddComponent<TextMesh>();
        m.text = t;
        m.fontSize = fontSize;
        m.characterSize = charSize;
        m.color = c;
        m.anchor = TextAnchor.MiddleCenter;
        m.fontStyle = FontStyle.Bold;
    }
}