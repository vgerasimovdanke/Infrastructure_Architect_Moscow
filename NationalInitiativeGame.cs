using UnityEngine;
using System.Collections.Generic;
using System.Collections;

public class NationalInitiativeGame : MonoBehaviour
{
    [System.Serializable]
    public class GameTask {
        public string name;
        public float cost; public float reward; public float baseDuration;
        public Color taskColor;
        [HideInInspector] public float remainingTime; 
        [HideInInspector] public int workersAssigned = 0;
        [HideInInspector] public bool isCompleted;
        [HideInInspector] public GameObject visualModel;
    }

    public float budget = 1500000f;
    public int totalWorkers = 15;
    private int availableWorkers;
    public List<GameTask> tasks = new List<GameTask>();
    private GameObject playerManager;
    private Material safeMat;

    void Start() {
        availableWorkers = totalWorkers;
        
        Camera cam = Camera.main;
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
        CreateLabel(playerManager, "РУКОВОДИТЕЛЬ ПТО", Color.white, 2.8f, 75);

        InvokeRepeating("SpawnTraffic", 1f, 3.5f);
    }

    void BuildStrictCity() {
        // Вертикальные дороги
        CreateRoad(new Vector3(-25f, 0.05f, 50f), new Vector3(10f, 0.15f, 300f));
        CreateRoad(new Vector3(25f, 0.05f, 50f), new Vector3(10f, 0.15f, 300f));
        
        // Горизонтальные дороги (шаг 40)
        for(int z = -40; z <= 120; z += 40) {
            CreateRoad(new Vector3(0, 0.05f, z), new Vector3(300f, 0.15f, 8f));
            
            // Фонари: расставлены по краям
            CreateLampPost(new Vector3(-32f, 0, z));
            CreateLampPost(new Vector3(32f, 0, z));
            CreateLampPost(new Vector3(-18f, 0, z + 15f));
            CreateLampPost(new Vector3(18f, 0, z + 15f));
        }

        // Декор в центре
        for(int i = -1; i <= 1; i++) {
            GameObject stripe = GameObject.CreatePrimitive(PrimitiveType.Cube);
            stripe.transform.position = new Vector3(i * 15f, 0.1f, -10f);
            stripe.transform.localScale = new Vector3(0.5f, 0.1f, 30f);
            SetColor(stripe, new Color(0.2f, 0.2f, 0.3f));
        }

        // Фоновые здания
        for (int x = -90; x <= 90; x += 18) {
            for (int z = -30; z <= 150; z += 18) {
                if (Mathf.Abs(x) < 35 && z < 60) continue; 
                if (Mathf.Abs(x - 25) < 8 || Mathf.Abs(x + 25) < 8) continue; 
                if (z % 40 == 0 || (z-2) % 40 == 0) continue;

                float h = Random.Range(15f, 40f);
                GameObject b = GameObject.CreatePrimitive(PrimitiveType.Cube);
                b.transform.position = new Vector3(x, h/2, z);
                b.transform.localScale = new Vector3(12f, h, 12f);
                SetColor(b, new Color(0.1f, 0.1f, 0.12f));
                AddWindows(b, h);
            }
        }
    }

    void CreateRoad(Vector3 pos, Vector3 scale) {
        GameObject r = GameObject.CreatePrimitive(PrimitiveType.Cube);
        r.transform.position = pos; r.transform.localScale = scale;
        SetColor(r, new Color(0.02f, 0.02f, 0.03f));
    }

    void AddWindows(GameObject building, float height) {
        for (float y = 3f; y < height - 2f; y += 4f) {
            if (Random.value > 0.3f) {
                GameObject w = GameObject.CreatePrimitive(PrimitiveType.Quad);
                w.transform.SetParent(building.transform);
                w.transform.localPosition = new Vector3(0, (y/height)-0.5f, -0.51f);
                w.transform.localScale = new Vector3(0.6f, 1.5f/height, 1f);
                SetColor(w, new Color(0.9f, 0.8f, 0.5f), true);
                Destroy(w.GetComponent<BoxCollider>());
            }
        }
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
        tasks.Add(new GameTask { name = "Объект ул. Ягодная", cost = 25000, reward = 200000, baseDuration = 8f, taskColor = Color.green });
        tasks.Add(new GameTask { name = "Цифровой Двойник", cost = 30000, reward = 120000, baseDuration = 6f, taskColor = Color.cyan });
        tasks.Add(new GameTask { name = "VR-Симуляция", cost = 180000, reward = 750000, baseDuration = 12f, taskColor = new Color(1f, 0.1f, 0.6f) });

        for (int i = 0; i < tasks.Count; i++) {
            GameObject tower = GameObject.CreatePrimitive(PrimitiveType.Cube);
            tower.transform.localScale = new Vector3(8f, 18f, 8f);
            
            // КЛЮЧЕВОЕ ИСПРАВЛЕНИЕ: z = 20f (ровно между дорогами 0 и 40)
            tower.transform.position = new Vector3(i * 22f - 22f, 9f, 20f);
            
            SetColor(tower, new Color(0.15f, 0.15f, 0.2f));
            for(float h = -0.4f; h < 0.5f; h += 0.25f) {
                GameObject band = GameObject.CreatePrimitive(PrimitiveType.Cube);
                band.transform.SetParent(tower.transform);
                band.transform.localPosition = new Vector3(0, h, 0);
                band.transform.localScale = new Vector3(1.05f, 0.05f, 1.05f);
                SetColor(band, Color.black);
            }
            tasks[i].visualModel = tower;
            tasks[i].remainingTime = tasks[i].baseDuration;
            CreateLabel(tower, tasks[i].name, Color.yellow, 11f, 55);
        }
    }

    void Update() {
        foreach (var t in tasks) {
            if (t.workersAssigned > 0 && !t.isCompleted) {
                t.remainingTime -= Time.deltaTime * t.workersAssigned;
                if (t.remainingTime <= 0) {
                    t.isCompleted = true; budget += t.reward;
                    SetColor(t.visualModel, t.taskColor, true);
                    foreach(Transform child in t.visualModel.transform) SetColor(child.gameObject, t.taskColor, true);
                }
            }
        }
    }

    void SpawnTraffic() {
        bool left = Random.value > 0.5f;
        GameObject car = GameObject.CreatePrimitive(PrimitiveType.Cube);
        car.transform.position = new Vector3(left ? -25f : 25f, 0.8f, left ? -80f : 150f);
        car.transform.localScale = new Vector3(2.5f, 1.2f, 5f);
        SetColor(car, new Color(0.2f, 0.2f, 0.5f), true);
        if (!left) car.transform.rotation = Quaternion.Euler(0, 180, 0);
        StartCoroutine(Drive(car));
    }

    IEnumerator Drive(GameObject c) {
        while (c != null) {
            c.transform.Translate(Vector3.forward * 35f * Time.deltaTime);
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
        Vector3 work = task.visualModel.transform.position + new Vector3(Random.Range(-2f, 2f), -8.5f, -6f);
        float s = 0;
        while (s < 1f) { s += Time.deltaTime * 1.5f; w.transform.position = Vector3.Lerp(home, work, s) + Vector3.up * Mathf.Abs(Mathf.Sin(s * 20f)) * 0.8f; yield return null; }
        while (!task.isCompleted) { w.transform.position = work + Vector3.up * (1f + Mathf.PingPong(Time.time * 4, 0.5f)); yield return null; }
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

    void OnGUI() {
        GUI.backgroundColor = new Color(0, 0, 0, 0.95f);
        Rect p = new Rect(25, Screen.height - 240, Screen.width - 50, 220);
        GUI.Box(p, "");
        GUIStyle st = new GUIStyle(GUI.skin.label) { fontSize = 32, fontStyle = FontStyle.Bold };
        GUI.Label(new Rect(p.x + 40, p.y + 25, 1200, 50), $"БАЛАНС: {budget:F0} Р. | БРИГАДЫ: {availableWorkers} / {totalWorkers}", st);
        float btnW = (p.width - 120) / 3;
        for (int i = 0; i < tasks.Count; i++) {
            var t = tasks[i];
            Rect r = new Rect(p.x + 40 + (i * (btnW + 20)), p.y + 90, btnW, 110);
            if (t.isCompleted) {
                GUI.backgroundColor = t.taskColor;
                GUI.Button(r, $"ОБЪЕКТ СДАН\n+{t.reward} р.", new GUIStyle(GUI.skin.button){fontSize=22, fontStyle=FontStyle.Bold});
            } else {
                GUI.backgroundColor = t.workersAssigned > 0 ? Color.yellow : Color.gray;
                string txt = t.workersAssigned > 0 ? $"В РАБОТЕ: {Mathf.Ceil(t.remainingTime/t.workersAssigned)}с" : $"{t.name}\n{t.cost}р";
                if (GUI.Button(r, txt) && availableWorkers > 0) StartCoroutine(SendWorker(t));
            }
        }
    }

    void CreateLabel(GameObject p, string t, Color c, float y, int s) {
        GameObject o = new GameObject("L"); o.transform.position = p.transform.position + Vector3.up * y;
        o.transform.SetParent(p.transform); var m = o.AddComponent<TextMesh>();
        m.text = t; m.fontSize = s; m.characterSize = 0.12f; m.color = c; m.anchor = TextAnchor.MiddleCenter;
    }
}