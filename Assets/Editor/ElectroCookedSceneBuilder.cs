using System.Collections.Generic;
using System.IO;
using ElectroCooked.Circuit;
using ElectroCooked.Core;
using ElectroCooked.Items;
using ElectroCooked.Managers;
using ElectroCooked.Minigame;
using ElectroCooked.Player;
using ElectroCooked.UI;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.Rendering;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem.UI;
#endif

namespace ElectroCooked.EditorTools
{
    /// <summary>
    /// Gera do zero uma sala de aula jogável (cena + prefabs + materiais + UI),
    /// já com tudo referenciado. Use o menu "ElectroCooked > Construir cena jogável".
    ///
    /// Rodar de novo simplesmente recria a cena por cima — então dá pra iterar no
    /// layout mexendo neste arquivo e reconstruindo, sem arrastar nada na mão.
    /// Quando você começar a ajustar a cena manualmente, faça uma cópia dela antes
    /// (Ctrl+D no arquivo .unity), porque o rebuild sobrescreve.
    /// </summary>
    public static class ElectroCookedSceneBuilder
    {
        internal const string ScenesFolder = "Assets/Scenes";
        internal const string PrefabsFolder = "Assets/Prefabs";
        internal const string MaterialsFolder = "Assets/Materials";
        internal const string ScenePath = ScenesFolder + "/Aula_Circuitos.unity";

        // ---------- paleta ----------
        // Regra: o ALUNO, cada COMPONENTE e cada ESTADO da bancada precisam de
        // matizes bem separados. Num jogo estilo Overcooked o jogador identifica
        // tudo pela cor, de longe e com pressa — duas coisas parecidas viram erro.
        //
        //   aluno ....... roxo      (não se confunde com nada da sala)
        //   bateria ..... verde
        //   resistor .... bege/areia
        //   fio ......... ciano
        //   extintor .... vermelho  (vermelho fica reservado só pra ele)
        //   bancada ..... cinza -> azul (montando) -> amarelo (calculando) -> marrom (queimada)
        internal static readonly Color FloorColor = new Color(0.72f, 0.70f, 0.66f);
        internal static readonly Color WallColor = new Color(0.42f, 0.46f, 0.52f);
        internal static readonly Color BenchBodyColor = new Color(0.28f, 0.30f, 0.34f);
        internal static readonly Color BenchTopColor = new Color(0.35f, 0.38f, 0.45f);

        internal static readonly Color PlayerColor = new Color(0.62f, 0.26f, 0.92f);      // roxo
        internal static readonly Color PlayerAccent = new Color(0.97f, 0.89f, 0.76f);     // cabeça, tom creme
        internal static readonly Color PlayerMarker = new Color(1.00f, 1.00f, 1.00f);     // bico da frente

        internal static readonly Color BatteryColor = new Color(0.12f, 0.80f, 0.38f);     // verde
        internal static readonly Color ResistorColor = new Color(0.88f, 0.72f, 0.38f);    // bege/areia
        internal static readonly Color WireColor = new Color(0.13f, 0.78f, 0.86f);        // ciano
        internal static readonly Color ExtinguisherColor = new Color(0.90f, 0.13f, 0.10f);// vermelho
        internal static readonly Color BandColor = new Color(0.10f, 0.10f, 0.12f);

        [MenuItem("ElectroCooked/Construir só a aula de circuitos", false, 10)]
        public static void BuildPlayableSceneMenu() => BuildPlayableScene(true);

        public static void BuildPlayableScene(bool showDialog = true)
        {
            if (EditorApplication.isPlaying)
            {
                EditorUtility.DisplayDialog("ElectroCooked", "Saia do modo Play antes de construir a cena.", "Ok");
                return;
            }

            if (showDialog && !EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo())
                return;

            EnsureFolders();

            // --- assets reutilizáveis ---
            Material floorMat = Mat("M_Piso", FloorColor, 0f, 0.15f);
            Material wallMat = Mat("M_Parede", WallColor, 0f, 0.1f);
            Material benchBodyMat = Mat("M_Bancada_Corpo", BenchBodyColor, 0.1f, 0.3f);
            Material benchTopMat = Mat("M_Bancada_Tampo", BenchTopColor, 0.15f, 0.45f);
            ApplyPalette();

            Material playerMat = Mat("M_Aluno", PlayerColor, 0f, 0.35f);
            Material accentMat = Mat("M_Aluno_Detalhe", PlayerAccent, 0f, 0.4f);
            Material markerMat = Mat("M_Aluno_Marcador", PlayerMarker, 0f, 0.55f);
            Material particleMat = ParticleMat("M_Particula");

            GameObject batteryPrefab = MakePartPrefab("P_Bateria", CircuitComponentType.Battery, BatteryColor);
            GameObject resistorPrefab = MakePartPrefab("P_Resistor", CircuitComponentType.Resistor, ResistorColor);
            GameObject wirePrefab = MakePartPrefab("P_Fio", CircuitComponentType.Wire, WireColor);
            GameObject extinguisherPrefab = MakeExtinguisherPrefab("P_Extintor", particleMat);

            // --- cena nova ---
            Scene scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);

            RenderSettings.ambientMode = AmbientMode.Trilight;
            RenderSettings.ambientSkyColor = new Color(0.55f, 0.58f, 0.65f);
            RenderSettings.ambientEquatorColor = new Color(0.42f, 0.42f, 0.45f);
            RenderSettings.ambientGroundColor = new Color(0.25f, 0.24f, 0.22f);
            RenderSettings.fog = false;

            BuildLighting();
            Camera cam = BuildCamera();
            BuildRoom(floorMat, wallMat);

            GameObject player = BuildPlayer(playerMat, accentMat, markerMat, cam.transform);

            var stations = new List<CircuitStation>
            {
                BuildStation("Bancada_1", new Vector3(-4.2f, 0f, 1.6f), benchBodyMat, benchTopMat, particleMat),
                BuildStation("Bancada_2", new Vector3( 0.0f, 0f, 1.6f), benchBodyMat, benchTopMat, particleMat),
                BuildStation("Bancada_3", new Vector3( 4.2f, 0f, 1.6f), benchBodyMat, benchTopMat, particleMat),
            };

            BuildCrate("Caixa_Baterias", new Vector3(-5.4f, 0f, -3.4f), BatteryColor, Carryable.Kind.CircuitPart,
                CircuitComponentType.Battery, batteryPrefab, "BATERIAS");
            BuildCrate("Caixa_Resistores", new Vector3(-1.8f, 0f, -3.4f), ResistorColor, Carryable.Kind.CircuitPart,
                CircuitComponentType.Resistor, resistorPrefab, "RESISTORES");
            BuildCrate("Caixa_Fios", new Vector3(1.8f, 0f, -3.4f), WireColor, Carryable.Kind.CircuitPart,
                CircuitComponentType.Wire, wirePrefab, "FIOS");
            BuildCrate("Caixa_Extintores", new Vector3(5.4f, 0f, -3.4f), ExtinguisherColor, Carryable.Kind.FireExtinguisher,
                CircuitComponentType.Battery, extinguisherPrefab, "EXTINTORES");

            BuildManagers(stations);
            BuildUI(player, stations);

            Undo.ClearAll();
            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene, ScenePath);
            RegisterSceneInBuildSettings();

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();

            Selection.activeGameObject = player;
            Debug.Log($"[ElectroCooked] Cena construída em {ScenePath}. " +
                      $"{stations.Count} bancadas, 4 caixas de suprimento. Aperte Play. " +
                      $"Controles: WASD mover, E pegar/encaixar, ESPAÇO usar extintor.");
            if (showDialog)
                EditorUtility.DisplayDialog("ElectroCooked",
                    "Sala de aula criada!\n\n" +
                    "Aperte PLAY:\n" +
                    "  WASD   - andar\n" +
                    "  E      - pegar peça / encaixar na bancada\n" +
                    "  ESPAÇO - usar o extintor\n" +
                    "  C      - abrir/fechar a calculadora\n" +
                    "  F      - mostrar/esconder o formulário\n\n" +
                    "Monte o que o ticket da bancada pede, acerte a conta e entregue.\n" +
                    "Errou? O circuito explode: pegue um extintor na caixa vermelha.",
                    "Bora!");
        }

        [MenuItem("ElectroCooked/Corrigir materiais rosa", false, 1)]
        public static void FixPinkMaterials()
        {
            EnsureFolders();

            bool urp = UsingUrp();
            int repointed = RepointAllMaterials();
            ApplyPalette();

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();

            string pipeline = urp ? "URP (Universal Render Pipeline)" : "Built-in (nenhum Render Pipeline Asset atribuído)";
            string msg = $"Pipeline ativo: {pipeline}.\n" +
                         $"{repointed} material(is) apontados para o shader certo.";

            Debug.Log("[ElectroCooked] " + msg.Replace("\n", " "));
            EditorUtility.DisplayDialog("ElectroCooked", msg +
                (urp ? "" : "\n\nO projeto tem o pacote do URP instalado mas está sem o URP Asset, " +
                            "então quem está rodando é o pipeline Built-in. Os materiais agora usam " +
                            "shaders do Built-in e a sala volta a aparecer colorida.\n\n" +
                            "Se um dia você atribuir um URP Asset em Project Settings > Graphics, " +
                            "é só rodar este comando de novo que ele reaponta tudo para o URP."),
                "Ok");
        }

        /// <summary>
        /// Passa por todos os materiais do jogo e troca o shader pelo do pipeline
        /// ativo, preservando a cor. É isso que tira o rosa choque.
        /// </summary>
        internal static int RepointAllMaterials()
        {
            if (!AssetDatabase.IsValidFolder(MaterialsFolder)) return 0;

            Shader lit = LitShader();
            Shader particle = ParticleShader();
            int changed = 0;

            foreach (string guid in AssetDatabase.FindAssets("t:Material", new[] { MaterialsFolder }))
            {
                string path = AssetDatabase.GUIDToAssetPath(guid);
                Material m = AssetDatabase.LoadAssetAtPath<Material>(path);
                if (m == null) continue;

                Shader target = path.Contains("Particula") ? particle : lit;
                if (target == null || m.shader == target) continue;

                Color c = ReadMainColor(m);
                m.shader = target;
                WriteMainColor(m, c);
                EditorUtility.SetDirty(m);
                changed++;
            }
            return changed;
        }

        internal static Color ReadMainColor(Material m)
        {
            if (m.HasProperty("_BaseColor")) return m.GetColor("_BaseColor");
            if (m.HasProperty("_Color")) return m.GetColor("_Color");
            return Color.white;
        }

        internal static void WriteMainColor(Material m, Color c)
        {
            if (m.HasProperty("_BaseColor")) m.SetColor("_BaseColor", c);
            if (m.HasProperty("_Color")) m.SetColor("_Color", c);
        }

        [MenuItem("ElectroCooked/Atualizar cores (sem refazer a cena)", false, 2)]
        public static void RefreshPalette()
        {
            EnsureFolders();
            ApplyPalette();
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            Debug.Log("[ElectroCooked] Paleta aplicada nos materiais. " +
                      "Aluno=roxo, bateria=verde, resistor=bege, fio=ciano, extintor=vermelho.");
        }

        /// <summary>
        /// Reescreve a cor de todos os materiais do jogo a partir das constantes lá
        /// de cima. Como os materiais são assets reaproveitados, dá pra iterar na
        /// paleta sem reconstruir a cena inteira e perder ajustes de layout.
        /// </summary>
        internal static void ApplyPalette()
        {
            Mat("M_Piso", FloorColor, 0f, 0.15f);
            Mat("M_Parede", WallColor, 0f, 0.1f);
            Mat("M_Bancada_Corpo", BenchBodyColor, 0.1f, 0.3f);
            Mat("M_Bancada_Tampo", BenchTopColor, 0.15f, 0.45f);

            Mat("M_Aluno", PlayerColor, 0f, 0.35f);
            Mat("M_Aluno_Detalhe", PlayerAccent, 0f, 0.4f);
            Mat("M_Aluno_Marcador", PlayerMarker, 0f, 0.55f);

            Mat("M_P_Bateria", BatteryColor, 0.1f, 0.4f);
            Mat("M_P_Resistor", ResistorColor, 0.1f, 0.4f);
            Mat("M_P_Fio", WireColor, 0.1f, 0.4f);
            Mat("M_Extintor", ExtinguisherColor, 0.3f, 0.55f);

            // detalhes metálicos e cenário
            Mat("M_Resistor_Faixa", BandColor, 0f, 0.3f);
            Mat("M_Bateria_Polo", new Color(0.85f, 0.85f, 0.88f), 0.8f, 0.7f);
            Mat("M_Resistor_Perna", new Color(0.75f, 0.75f, 0.78f), 0.85f, 0.65f);
            Mat("M_Extintor_Metal", new Color(0.25f, 0.25f, 0.28f), 0.85f, 0.6f);
            Mat("M_Lousa", new Color(0.10f, 0.24f, 0.18f), 0f, 0.2f);

            ParticleMat("M_Particula");

            ApplyCratePalette("BATERIAS", BatteryColor);
            ApplyCratePalette("RESISTORES", ResistorColor);
            ApplyCratePalette("FIOS", WireColor);
            ApplyCratePalette("EXTINTORES", ExtinguisherColor);
        }

        internal static void ApplyCratePalette(string label, Color color)
        {
            Mat("M_Caixa_" + label, Darken(color), 0f, 0.3f);
            Mat("M_Caixa_Borda_" + label, color, 0f, 0.5f);
        }

        internal static Color Darken(Color c, float factor = 0.7f)
            => new Color(c.r * factor, c.g * factor, c.b * factor, 1f);

        [MenuItem("ElectroCooked/Abrir cena da aula", false, 20)]
        public static void OpenScene()
        {
            if (File.Exists(ScenePath))
            {
                if (EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo())
                    EditorSceneManager.OpenScene(ScenePath);
            }
            else
            {
                EditorUtility.DisplayDialog("ElectroCooked",
                    "A cena ainda não existe. Use \"ElectroCooked > Construir cena jogável\".", "Ok");
            }
        }

        // =====================================================================
        // Mundo
        // =====================================================================

        internal static void BuildLighting()
        {
            var sunGo = new GameObject("Sol");
            sunGo.transform.SetPositionAndRotation(new Vector3(0f, 8f, 0f), Quaternion.Euler(48f, -35f, 0f));
            Light sun = sunGo.AddComponent<Light>();
            sun.type = LightType.Directional;
            sun.color = new Color(1f, 0.97f, 0.89f);
            sun.intensity = 1.35f;
            sun.shadows = LightShadows.Soft;
        }

        internal static Camera BuildCamera()
        {
            var camGo = new GameObject("Main Camera");
            camGo.tag = "MainCamera";
            camGo.transform.SetPositionAndRotation(new Vector3(0f, 12.5f, -8.5f), Quaternion.Euler(54f, 0f, 0f));

            Camera cam = camGo.AddComponent<Camera>();
            cam.fieldOfView = 48f;
            cam.nearClipPlane = 0.3f;
            cam.farClipPlane = 120f;
            cam.clearFlags = CameraClearFlags.SolidColor;
            cam.backgroundColor = new Color(0.09f, 0.10f, 0.13f);
            camGo.AddComponent<AudioListener>();
            return cam;
        }

        internal static void BuildRoom(Material floorMat, Material wallMat)
        {
            var room = new GameObject("Sala_de_Aula");

            GameObject floor = Prim(PrimitiveType.Plane, "Piso", room.transform, Vector3.zero,
                new Vector3(1.6f, 1f, 1.2f), floorMat);
            floor.isStatic = true;

            // paredes: sala de 16 x 12
            MakeWall(room.transform, "Parede_Fundo", new Vector3(0f, 1.5f, 6f), new Vector3(16f, 3f, 0.4f), wallMat);
            MakeWall(room.transform, "Parede_Frente", new Vector3(0f, 1.5f, -6f), new Vector3(16f, 3f, 0.4f), wallMat);
            MakeWall(room.transform, "Parede_Esq", new Vector3(-8f, 1.5f, 0f), new Vector3(0.4f, 3f, 12f), wallMat);
            MakeWall(room.transform, "Parede_Dir", new Vector3(8f, 1.5f, 0f), new Vector3(0.4f, 3f, 12f), wallMat);

            // lousa no fundo, só pra dar cara de sala de aula
            Material boardMat = Mat("M_Lousa", new Color(0.10f, 0.24f, 0.18f), 0f, 0.2f);
            GameObject board = Prim(PrimitiveType.Cube, "Lousa", room.transform, new Vector3(0f, 1.9f, 5.75f),
                new Vector3(7f, 2f, 0.12f), boardMat);
            Object.DestroyImmediate(board.GetComponent<Collider>());
            board.isStatic = true;
        }

        internal static void MakeWall(Transform parent, string name, Vector3 pos, Vector3 scale, Material mat)
        {
            GameObject wall = Prim(PrimitiveType.Cube, name, parent, pos, scale, mat);
            wall.isStatic = true;
        }

        internal static GameObject BuildPlayer(Material bodyMat, Material accentMat, Material markerMat, Transform cam)
        {
            var player = new GameObject("Aluno");
            player.transform.position = new Vector3(0f, 0.1f, -2.5f);

            var cc = player.AddComponent<CharacterController>();
            cc.height = 1.7f;
            cc.radius = 0.35f;
            cc.center = new Vector3(0f, 0.85f, 0f);
            cc.slopeLimit = 50f;
            cc.stepOffset = 0.35f;

            GameObject body = Prim(PrimitiveType.Capsule, "Corpo", player.transform,
                new Vector3(0f, 0.85f, 0f), new Vector3(0.7f, 0.6f, 0.7f), bodyMat);
            Object.DestroyImmediate(body.GetComponent<Collider>());

            GameObject head = Prim(PrimitiveType.Sphere, "Cabeca", player.transform,
                new Vector3(0f, 1.62f, 0f), new Vector3(0.5f, 0.5f, 0.5f), accentMat);
            Object.DestroyImmediate(head.GetComponent<Collider>());

            // bico indicando pra onde ele está virado (importante num jogo de mira/extintor)
            GameObject nose = Prim(PrimitiveType.Cube, "Frente", player.transform,
                new Vector3(0f, 1.6f, 0.28f), new Vector3(0.16f, 0.16f, 0.22f), markerMat);
            Object.DestroyImmediate(nose.GetComponent<Collider>());

            var carryPoint = new GameObject("PontoDaMao");
            carryPoint.transform.SetParent(player.transform, false);
            carryPoint.transform.localPosition = new Vector3(0f, 1.15f, 0.55f);

            var controller = player.AddComponent<PlayerController>();
            controller.moveSpeed = 5.5f;
            controller.cameraTransform = cam;

            var carrier = player.AddComponent<PlayerCarrier>();
            carrier.carryPoint = carryPoint.transform;
            carrier.interactRange = 1.9f;

            return player;
        }

        // =====================================================================
        // Bancadas
        // =====================================================================

        internal static CircuitStation BuildStation(string name, Vector3 pos, Material bodyMat, Material topMat, Material particleMat)
        {
            var root = new GameObject(name);
            root.transform.position = pos;

            GameObject body = Prim(PrimitiveType.Cube, "Corpo", root.transform,
                new Vector3(0f, 0.45f, 0f), new Vector3(2.4f, 0.9f, 1.2f), bodyMat);
            body.isStatic = true;

            GameObject top = Prim(PrimitiveType.Cube, "Tampo", root.transform,
                new Vector3(0f, 0.98f, 0f), new Vector3(2.5f, 0.16f, 1.3f), topMat);

            // encaixes
            Transform batterySlot = MakeSlot(root.transform, "Slot_Bateria", new Vector3(-0.85f, 1.15f, 0.05f));
            Transform wireSlot = MakeSlot(root.transform, "Slot_Fio", new Vector3(0.0f, 1.15f, -0.38f));
            Transform[] resistorSlots =
            {
                MakeSlot(root.transform, "Slot_Resistor_1", new Vector3(-0.2f, 1.15f, 0.20f)),
                MakeSlot(root.transform, "Slot_Resistor_2", new Vector3(0.35f, 1.15f, 0.20f)),
                MakeSlot(root.transform, "Slot_Resistor_3", new Vector3(0.90f, 1.15f, 0.20f)),
            };

            // fogo
            var fireGo = new GameObject("Fogo");
            fireGo.transform.SetParent(root.transform, false);
            fireGo.transform.localPosition = new Vector3(0f, 1.15f, 0f);

            // o cone de partículas emite ao longo do +Z local, então giramos o
            // objeto das chamas para que elas subam.
            var flamesGo = new GameObject("Chamas");
            flamesGo.transform.SetParent(fireGo.transform, false);
            flamesGo.transform.localRotation = Quaternion.Euler(-90f, 0f, 0f);

            ParticleSystem fireFx = MakeParticles(flamesGo, particleMat,
                color: new Color(1f, 0.55f, 0.12f, 0.85f), lifetime: 0.9f, speed: 2.2f, size: 0.55f,
                rate: 55f, coneAngle: 18f, radius: 0.45f, gravity: -0.35f);

            var fireLightGo = new GameObject("LuzDoFogo");
            fireLightGo.transform.SetParent(fireGo.transform, false);
            fireLightGo.transform.localPosition = new Vector3(0f, 0.6f, 0f);
            Light fireLight = fireLightGo.AddComponent<Light>();
            fireLight.type = LightType.Point;
            fireLight.color = new Color(1f, 0.55f, 0.2f);
            fireLight.intensity = 4.5f;
            fireLight.range = 7f;
            fireLight.enabled = false;

            var hazard = fireGo.AddComponent<FireHazard>();
            hazard.fireParticles = fireFx;
            hazard.fireLight = fireLight;
            hazard.maxFireAmount = 100f;

            // explosão
            var boomGo = new GameObject("Explosao");
            boomGo.transform.SetParent(root.transform, false);
            boomGo.transform.localPosition = new Vector3(0f, 1.15f, 0f);
            ParticleSystem boomFx = MakeParticles(boomGo, particleMat,
                color: new Color(1f, 0.85f, 0.35f, 0.95f), lifetime: 0.55f, speed: 7.5f, size: 0.45f,
                rate: 0f, coneAngle: 60f, radius: 0.15f, gravity: 0.6f, burstCount: 45,
                shapeType: ParticleSystemShapeType.Sphere);

            var station = root.AddComponent<CircuitStation>();
            station.batterySlot = batterySlot;
            station.wireSlot = wireSlot;
            station.resistorSlots = resistorSlots;
            station.fireHazard = hazard;
            station.explosionParticles = boomFx;

            var visuals = root.AddComponent<StationVisuals>();
            visuals.surfaceRenderer = top.GetComponent<Renderer>();

            BuildTicketCanvas(root.transform, station);
            return station;
        }

        internal static Transform MakeSlot(Transform parent, string name, Vector3 localPos)
        {
            var go = new GameObject(name);
            go.transform.SetParent(parent, false);
            go.transform.localPosition = localPos;
            return go.transform;
        }

        internal static void BuildTicketCanvas(Transform stationRoot, CircuitStation station)
        {
            var canvasGo = new GameObject("Ticket", typeof(RectTransform));
            canvasGo.transform.SetParent(stationRoot, false);
            canvasGo.transform.localPosition = new Vector3(0f, 2.75f, 0f);
            canvasGo.transform.localScale = Vector3.one * 0.0078f;

            var canvas = canvasGo.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.WorldSpace;
            var canvasRt = canvasGo.GetComponent<RectTransform>();
            canvasRt.sizeDelta = new Vector2(460f, 400f);

            Image bg = MakeImage("Fundo", canvasRt, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f),
                Vector2.zero, new Vector2(460f, 400f), new Color(0.06f, 0.07f, 0.10f, 0.94f));
            Stretch(bg.rectTransform);

            // alinhado à esquerda: linhas do tipo "R1 = 20 Ohm" são muito mais
            // fáceis de varrer com o olho quando os "=" ficam empilhados
            Text txt = MakeText("Texto", bg.rectTransform, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f),
                Vector2.zero, new Vector2(420f, 360f), "", 34, TextAnchor.UpperLeft, Color.white,
                shadow: true, lineSpacing: 1.15f);
            Stretch(txt.rectTransform, 22f);

            var ticket = canvasGo.AddComponent<OrderTicketUI>();
            ticket.station = station;
            ticket.ticketText = txt;
            ticket.faceCamera = true;
        }

        // =====================================================================
        // Caixas de suprimento
        // =====================================================================

        internal static void BuildCrate(string name, Vector3 pos, Color color, Carryable.Kind kind,
            CircuitComponentType partType, GameObject prefab, string label)
        {
            var root = new GameObject(name);
            root.transform.position = pos;

            Material mat = Mat("M_Caixa_" + label, Darken(color), 0f, 0.3f);
            GameObject box = Prim(PrimitiveType.Cube, "Caixa", root.transform,
                new Vector3(0f, 0.5f, 0f), new Vector3(1.3f, 1f, 1.1f), mat);
            box.isStatic = true;

            Material rimMat = Mat("M_Caixa_Borda_" + label, color, 0f, 0.5f);
            GameObject rim = Prim(PrimitiveType.Cube, "Borda", root.transform,
                new Vector3(0f, 1.02f, 0f), new Vector3(1.4f, 0.1f, 1.2f), rimMat);
            Object.DestroyImmediate(rim.GetComponent<Collider>());

            var spawn = new GameObject("PontoDeSaida");
            spawn.transform.SetParent(root.transform, false);
            spawn.transform.localPosition = new Vector3(0f, 1.3f, -0.75f);

            var crate = root.AddComponent<SupplyCrate>();
            crate.kind = kind;
            crate.partType = partType;
            crate.itemPrefab = prefab != null ? prefab.GetComponent<Carryable>() : null;
            crate.spawnPoint = spawn.transform;

            // placa com o nome
            var canvasGo = new GameObject("Placa", typeof(RectTransform));
            canvasGo.transform.SetParent(root.transform, false);
            canvasGo.transform.localPosition = new Vector3(0f, 1.85f, 0f);
            canvasGo.transform.localScale = Vector3.one * 0.0075f;
            var canvas = canvasGo.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.WorldSpace;
            var rt = canvasGo.GetComponent<RectTransform>();
            rt.sizeDelta = new Vector2(440f, 96f);

            Image bg = MakeImage("Fundo", rt, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f),
                Vector2.zero, new Vector2(440f, 96f), new Color(0.06f, 0.07f, 0.10f, 0.92f));
            Stretch(bg.rectTransform);
            Text t = MakeText("Texto", bg.rectTransform, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f),
                Vector2.zero, new Vector2(420f, 86f), label, 52, TextAnchor.MiddleCenter, Color.white);
            Stretch(t.rectTransform, 6f);

            canvasGo.AddComponent<Billboard>();
        }

        // =====================================================================
        // Managers
        // =====================================================================

        internal static GameObject BuildManagers(List<CircuitStation> stations)
        {
            var go = new GameObject("_Managers");

            var score = go.AddComponent<ScoreManager>();
            score.pointsForFullGrade = 8f;
            score.penaltyPerMistake = 0.35f;

            var orders = go.AddComponent<OrderManager>();
            orders.stations = new List<CircuitStation>(stations);
            orders.delayBeforeNextOrder = 1.2f;

            go.AddComponent<MinigameManager>();

            var game = go.AddComponent<GameManager>();
            game.classDurationSeconds = 300f;
            game.secondsPerDifficultyStep = 90f;

            return go;
        }

        // =====================================================================
        // UI de tela
        // =====================================================================

        // =====================================================================
        // UI de tela
        //
        // Resolução de referência 1280x720 (e não 1920x1080): a janela Game de
        // quem está desenvolvendo costuma ser baixa, e com referência 1920 o
        // CanvasScaler encolhe tudo para menos da metade — foi por isso que o
        // texto ficou ilegível e os painéis saíram para fora da tela.
        //
        // Todos os painéis usam âncoras esticadas com margem em vez de tamanho
        // fixo, então eles se ajustam à janela em vez de vazar por fora dela.
        // =====================================================================

        internal const float UiMarginTop = 76f;
        internal const float UiMarginBottom = 60f;

        internal static void BuildUI(GameObject player, List<CircuitStation> stations)
        {
            var eventSystemGo = new GameObject("EventSystem");
            eventSystemGo.AddComponent<EventSystem>();
#if ENABLE_INPUT_SYSTEM
            var module = eventSystemGo.AddComponent<InputSystemUIInputModule>();
            module.AssignDefaultActions();
#else
            eventSystemGo.AddComponent<StandaloneInputModule>();
#endif

            var canvasGo = new GameObject("Canvas_HUD", typeof(RectTransform));
            var canvas = canvasGo.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            var scaler = canvasGo.AddComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1280f, 720f);
            scaler.screenMatchMode = CanvasScaler.ScreenMatchMode.MatchWidthOrHeight;
            scaler.matchWidthOrHeight = 0.5f;
            canvasGo.AddComponent<GraphicRaycaster>();
            var canvasRt = canvasGo.GetComponent<RectTransform>();

            // ---- barra superior ----
            Image topBar = MakeImage("BarraSuperior", canvasRt, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f),
                new Vector2(0f, -10f), new Vector2(660f, 62f), new Color(0.05f, 0.06f, 0.09f, 0.92f));

            Text timeText = MakeText("Tempo", topBar.rectTransform, new Vector2(0f, 0.5f), new Vector2(0f, 0.5f),
                new Vector2(22f, 0f), new Vector2(180f, 52f), "05:00", 40, TextAnchor.MiddleLeft, Color.white);
            Text gradeText = MakeText("Nota", topBar.rectTransform, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f),
                new Vector2(20f, 0f), new Vector2(240f, 52f), "NOTA 0.0", 32, TextAnchor.MiddleCenter,
                new Color(0.45f, 0.95f, 0.60f));
            Text mistakesText = MakeText("Explosoes", topBar.rectTransform, new Vector2(1f, 0.5f), new Vector2(1f, 0.5f),
                new Vector2(-66f, 0f), new Vector2(230f, 52f), "EXPLOSOES 0", 26, TextAnchor.MiddleRight,
                new Color(0.98f, 0.55f, 0.42f));
            Text comboText = MakeText("Combo", topBar.rectTransform, new Vector2(1f, 0.5f), new Vector2(1f, 0.5f),
                new Vector2(-22f, 0f), new Vector2(140f, 52f), "COMBO x1", 22, TextAnchor.MiddleRight,
                new Color(1f, 0.80f, 0.25f));
            comboText.gameObject.SetActive(false);

            Text hint = MakeText("DicaControles", canvasRt, new Vector2(0.5f, 0f), new Vector2(0.5f, 0f),
                new Vector2(0f, 16f), new Vector2(1160f, 34f), "", 20, TextAnchor.MiddleCenter,
                new Color(1f, 1f, 1f, 0.7f));

            // ---- painel de resumo (boletim) ----
            Image summary = MakeImage("PainelResumo", canvasRt, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f),
                Vector2.zero, new Vector2(660f, 430f), new Color(0.04f, 0.05f, 0.08f, 0.98f));
            Text summaryText = MakeText("Texto", summary.rectTransform, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f),
                Vector2.zero, new Vector2(600f, 380f), "", 28, TextAnchor.MiddleCenter, Color.white,
                shadow: true, lineSpacing: 1.25f);
            Stretch(summaryText.rectTransform, 28f);
            summary.gameObject.SetActive(false);

            var hud = canvasGo.AddComponent<HUDUI>();
            hud.timeText = timeText;
            hud.gradeText = gradeText;
            hud.mistakesText = mistakesText;
            hud.comboText = comboText;
            hud.controlsHintText = hint;
            hud.summaryPanel = summary.gameObject;
            hud.summaryText = summaryText;

            MinigameUI minigame = BuildMinigamePanel(canvasGo, canvasRt);
            BuildFormulaSheet(canvasGo, canvasRt);
            BuildCalculator(canvasGo, canvasRt, minigame);
            BuildNearbyStationPanel(canvasGo, canvasRt, player, stations);
        }

        // ---- painel grande com o pedido da bancada mais próxima (canto inferior esquerdo) ----
        internal static void BuildNearbyStationPanel(GameObject canvasGo, RectTransform canvasRt,
            GameObject player, List<CircuitStation> stations)
        {
            Image panel = MakeImage("PainelBancadaPerto", canvasRt, new Vector2(0f, 0f), new Vector2(0f, 0f),
                new Vector2(18f, 18f), new Vector2(340f, 214f), new Color(0.05f, 0.06f, 0.09f, 0.92f));

            Text header = MakeText("Cabecalho", panel.rectTransform, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f),
                new Vector2(0f, -10f), new Vector2(312f, 28f), "BANCADA", 20, TextAnchor.MiddleCenter,
                new Color(0.98f, 0.82f, 0.35f));

            Text body = MakeText("Corpo", panel.rectTransform, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f),
                new Vector2(0f, -42f), new Vector2(312f, 162f), "", 24, TextAnchor.UpperLeft, Color.white,
                shadow: true, lineSpacing: 1.18f);

            var nearby = canvasGo.AddComponent<NearbyStationUI>();
            nearby.player = player != null ? player.transform : null;
            nearby.stations = new List<CircuitStation>(stations);
            nearby.rootPanel = panel.gameObject;
            nearby.headerText = header;
            nearby.bodyText = body;
            nearby.showRange = 6.5f;

            panel.gameObject.SetActive(false);
        }

        // ---- formulário (tecla F), canto superior esquerdo ----
        internal static void BuildFormulaSheet(GameObject canvasGo, RectTransform canvasRt)
        {
            Image panel = MakeImage("PainelFormulario", canvasRt, new Vector2(0f, 1f), new Vector2(0f, 1f),
                new Vector2(18f, -18f), new Vector2(330f, 396f), new Color(0.05f, 0.06f, 0.09f, 0.96f));

            Text title = MakeText("Titulo", panel.rectTransform, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f),
                new Vector2(0f, -8f), new Vector2(304f, 30f), FormulaSheetUI.Title, 22,
                TextAnchor.MiddleCenter, new Color(0.98f, 0.82f, 0.35f));

            Text body = MakeText("Corpo", panel.rectTransform, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f),
                new Vector2(0f, -42f), new Vector2(304f, 344f), FormulaSheetUI.Body, 20,
                TextAnchor.UpperLeft, new Color(0.94f, 0.96f, 1f), shadow: true, lineSpacing: 1.12f);

            var sheet = canvasGo.AddComponent<FormulaSheetUI>();
            sheet.rootPanel = panel.gameObject;
            sheet.titleText = title;
            sheet.bodyText = body;
            sheet.startVisible = true;
        }

        // ---- calculadora (tecla C), lateral direita ----
        internal static void BuildCalculator(GameObject canvasGo, RectTransform canvasRt, MinigameUI minigame)
        {
            RectTransform p = NewRect("PainelCalculadora", canvasRt, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f),
                Vector2.zero, Vector2.zero);
            p.anchorMin = new Vector2(1f, 0f);
            p.anchorMax = new Vector2(1f, 1f);
            p.offsetMin = new Vector2(-318f, UiMarginBottom);
            p.offsetMax = new Vector2(-18f, -UiMarginTop);
            var panelImg = p.gameObject.AddComponent<Image>();
            panelImg.sprite = BackgroundSprite();
            panelImg.type = Image.Type.Sliced;
            panelImg.color = new Color(0.05f, 0.06f, 0.09f, 0.97f);

            MakeText("Titulo", p, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f),
                new Vector2(0f, -8f), new Vector2(276f, 28f), "CALCULADORA  [C]", 20,
                TextAnchor.MiddleCenter, new Color(0.98f, 0.82f, 0.35f));

            Text expression = MakeText("Conta", p, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f),
                new Vector2(0f, -40f), new Vector2(272f, 24f), "", 19,
                TextAnchor.MiddleRight, new Color(0.65f, 0.68f, 0.75f));

            Image visor = MakeImage("Visor", p, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f),
                new Vector2(0f, -66f), new Vector2(276f, 52f), new Color(0.02f, 0.06f, 0.03f, 1f));
            Text display = MakeText("Texto", visor.rectTransform, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f),
                Vector2.zero, new Vector2(256f, 44f), "0", 34, TextAnchor.MiddleRight,
                new Color(0.45f, 1f, 0.55f));
            Stretch(display.rectTransform, 10f);

            const float bw = 64f, bh = 46f, stepX = 70f, stepY = 52f;
            float[] colX = { -105f, -35f, 35f, 105f };
            float row0 = -132f;

            string[,] grid =
            {
                { "C", "<", "/", "x" },
                { "7", "8", "9", "-" },
                { "4", "5", "6", "+" },
                { "1", "2", "3", "=" },
            };

            var digits = new Button[10];
            Button dot = null, clear = null, back = null, add = null, sub = null, mul = null, div = null, eq = null;

            for (int r = 0; r < 4; r++)
            {
                for (int c = 0; c < 4; c++)
                {
                    string key = grid[r, c];
                    bool isDigit = key.Length == 1 && key[0] >= '0' && key[0] <= '9';

                    Color tint = key == "C" ? new Color(0.55f, 0.20f, 0.20f)
                               : key == "=" ? new Color(0.15f, 0.50f, 0.30f)
                               : isDigit ? new Color(0.20f, 0.23f, 0.30f)
                                         : new Color(0.28f, 0.30f, 0.40f);

                    Button b = MakeButton("Calc_" + key, p, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f),
                        new Vector2(colX[c], row0 - r * stepY), new Vector2(bw, bh), key, 28, tint);

                    if (isDigit) digits[key[0] - '0'] = b;
                    else if (key == "C") clear = b;
                    else if (key == "<") back = b;
                    else if (key == "/") div = b;
                    else if (key == "x") mul = b;
                    else if (key == "-") sub = b;
                    else if (key == "+") add = b;
                    else if (key == "=") eq = b;
                }
            }

            float rowZero = row0 - 4f * stepY;
            digits[0] = MakeButton("Calc_0", p, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f),
                new Vector2(-70f, rowZero), new Vector2(bw * 2f + 6f, bh), "0", 28, new Color(0.20f, 0.23f, 0.30f));
            dot = MakeButton("Calc_ponto", p, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f),
                new Vector2(70f, rowZero), new Vector2(bw * 2f + 6f, bh), ",", 28, new Color(0.20f, 0.23f, 0.30f));

            Button use = MakeButton("Calc_usar", p, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f),
                new Vector2(0f, rowZero - 54f), new Vector2(276f, 44f), "USAR NA RESPOSTA", 20,
                new Color(0.16f, 0.42f, 0.62f));

            Button close = MakeButton("Calc_fechar", p, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f),
                new Vector2(0f, rowZero - 104f), new Vector2(276f, 38f), "FECHAR", 18,
                new Color(0.24f, 0.25f, 0.30f));

            var calc = canvasGo.AddComponent<CalculatorUI>();
            calc.rootPanel = p.gameObject;
            calc.displayText = display;
            calc.expressionText = expression;
            calc.digitButtons = digits;
            calc.dotButton = dot;
            calc.clearButton = clear;
            calc.backspaceButton = back;
            calc.addButton = add;
            calc.subtractButton = sub;
            calc.multiplyButton = mul;
            calc.divideButton = div;
            calc.equalsButton = eq;
            calc.useResultButton = use;
            calc.closeButton = close;
            calc.minigameUI = minigame;
            minigame.calculator = calc;
            calc.startOpen = false;

            p.gameObject.SetActive(false);
        }

        // ---- painel do desafio de cálculo, centro da tela ----
        internal static MinigameUI BuildMinigamePanel(GameObject canvasGo, RectTransform canvasRt)
        {
            RectTransform root = NewRect("PainelMinigame", canvasRt, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f),
                Vector2.zero, Vector2.zero);
            root.anchorMin = new Vector2(0.5f, 0f);
            root.anchorMax = new Vector2(0.5f, 1f);
            root.offsetMin = new Vector2(-300f, UiMarginBottom);
            root.offsetMax = new Vector2(300f, -UiMarginTop);
            var rootImg = root.gameObject.AddComponent<Image>();
            rootImg.sprite = BackgroundSprite();
            rootImg.type = Image.Type.Sliced;
            rootImg.color = new Color(0.04f, 0.05f, 0.08f, 0.98f);

            Text header = MakeText("Cabecalho", root, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f),
                new Vector2(0f, -10f), new Vector2(560f, 42f), "CALCULE", 34, TextAnchor.MiddleCenter,
                new Color(0.98f, 0.82f, 0.35f));

            Image ticketBg = MakeImage("FundoTicket", root, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f),
                new Vector2(0f, -58f), new Vector2(560f, 140f), new Color(0.11f, 0.13f, 0.17f, 1f));
            Text ticket = MakeText("Texto", ticketBg.rectTransform, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f),
                Vector2.zero, new Vector2(530f, 126f), "", 26, TextAnchor.MiddleCenter, Color.white,
                shadow: true, lineSpacing: 1.22f);
            Stretch(ticket.rectTransform, 10f);

            Slider timer = MakeSlider("BarraTempo", root, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f),
                new Vector2(0f, -206f), new Vector2(560f, 18f), false, out Image timerFill);
            timer.interactable = false;
            timer.minValue = 0f; timer.maxValue = 1f; timer.value = 1f;

            Text feedback = MakeText("Feedback", root, new Vector2(0.5f, 0f), new Vector2(0.5f, 0f),
                new Vector2(0f, 10f), new Vector2(560f, 56f), "", 22, TextAnchor.MiddleCenter, Color.white,
                shadow: true, lineSpacing: 1.1f);

            // área da resposta: estica entre a barra de tempo e o feedback,
            // então ela encolhe junto com a janela em vez de vazar
            RectTransform area = NewRect("AreaResposta", root, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f),
                Vector2.zero, Vector2.zero);
            area.anchorMin = Vector2.zero;
            area.anchorMax = Vector2.one;
            area.offsetMin = new Vector2(20f, 74f);
            area.offsetMax = new Vector2(-20f, -232f);

            // ---------- estilo 1: teclado numérico ----------
            RectTransform numericRt = NewRect("Painel_Numerico", area, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f),
                Vector2.zero, Vector2.zero);
            numericRt.anchorMin = Vector2.zero;
            numericRt.anchorMax = Vector2.one;
            numericRt.offsetMin = Vector2.zero;
            numericRt.offsetMax = Vector2.zero;

            Image displayBg = MakeImage("Visor", numericRt, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f),
                new Vector2(0f, 0f), new Vector2(540f, 44f), new Color(0.02f, 0.06f, 0.03f, 1f));
            Text display = MakeText("Texto", displayBg.rectTransform, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f),
                Vector2.zero, new Vector2(520f, 38f), "_ _ _", 30, TextAnchor.MiddleCenter,
                new Color(0.45f, 1f, 0.55f));
            Stretch(display.rectTransform, 8f);

            var digitButtons = new Button[10];
            string[,] grid =
            {
                { "7", "8", "9" },
                { "4", "5", "6" },
                { "1", "2", "3" },
                { ",", "0", "C" },
            };

            const float kw = 100f, kh = 48f, kStepX = 110f, kStepY = 54f;
            float kStartX = -190f, kStartY = -52f;

            Button decimalBtn = null, clearBtn = null;
            for (int r = 0; r < 4; r++)
            {
                for (int c = 0; c < 3; c++)
                {
                    string label = grid[r, c];
                    var pos = new Vector2(kStartX + c * kStepX, kStartY - r * kStepY);
                    Button b = MakeButton("Tecla_" + label, numericRt, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f),
                        pos, new Vector2(kw, kh), label, 30,
                        label == "C" ? new Color(0.55f, 0.20f, 0.20f) : new Color(0.20f, 0.23f, 0.30f));

                    if (label == ",") decimalBtn = b;
                    else if (label == "C") clearBtn = b;
                    else digitButtons[int.Parse(label)] = b;
                }
            }

            Button confirmNumeric = MakeButton("Confirmar", numericRt, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f),
                new Vector2(210f, -52f), new Vector2(200f, 210f), "ENTREGAR", 26, new Color(0.15f, 0.50f, 0.30f));

            // ---------- estilo 2: múltipla escolha ----------
            RectTransform choiceRt = NewRect("Painel_MultiplaEscolha", area, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f),
                Vector2.zero, Vector2.zero);
            choiceRt.anchorMin = Vector2.zero;
            choiceRt.anchorMax = Vector2.one;
            choiceRt.offsetMin = Vector2.zero;
            choiceRt.offsetMax = Vector2.zero;

            var choiceButtons = new Button[4];
            for (int i = 0; i < 4; i++)
            {
                int col = i % 2, row = i / 2;
                var pos = new Vector2(-140f + col * 280f, -14f - row * 84f);
                choiceButtons[i] = MakeButton("Opcao_" + (i + 1), choiceRt, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f),
                    pos, new Vector2(266f, 70f), "--", 28, new Color(0.20f, 0.28f, 0.42f));
            }

            // ---------- estilo 3: régua ----------
            RectTransform sliderRt = NewRect("Painel_Regua", area, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f),
                Vector2.zero, Vector2.zero);
            sliderRt.anchorMin = Vector2.zero;
            sliderRt.anchorMax = Vector2.one;
            sliderRt.offsetMin = Vector2.zero;
            sliderRt.offsetMax = Vector2.zero;

            Text sliderValue = MakeText("Valor", sliderRt, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f),
                new Vector2(0f, -8f), new Vector2(520f, 52f), "0", 40, TextAnchor.MiddleCenter,
                new Color(0.45f, 1f, 0.55f));

            Slider answerSlider = MakeSlider("Regua", sliderRt, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f),
                new Vector2(0f, -74f), new Vector2(520f, 34f), true, out _);

            Button confirmSlider = MakeButton("Confirmar", sliderRt, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f),
                new Vector2(0f, -128f), new Vector2(240f, 56f), "ENTREGAR", 26, new Color(0.15f, 0.50f, 0.30f));

            var ui = canvasGo.AddComponent<MinigameUI>();
            ui.rootPanel = root.gameObject;
            ui.headerText = header;
            ui.ticketReminderText = ticket;
            ui.timerBar = timer;
            ui.timerBarFill = timerFill;
            ui.feedbackText = feedback;

            ui.numericPanel = numericRt.gameObject;
            ui.numericDisplayText = display;
            ui.digitButtons = digitButtons;
            ui.decimalButton = decimalBtn;
            ui.clearButton = clearBtn;
            ui.numericConfirmButton = confirmNumeric;

            ui.multipleChoicePanel = choiceRt.gameObject;
            ui.choiceButtons = choiceButtons;

            ui.sliderPanel = sliderRt.gameObject;
            ui.answerSlider = answerSlider;
            ui.answerSliderValueText = sliderValue;
            ui.sliderConfirmButton = confirmSlider;

            root.gameObject.SetActive(false);
            return ui;
        }

        internal static GameObject MakePartPrefab(string name, CircuitComponentType type, Color color)
        {
            var root = new GameObject(name);

            var rb = root.AddComponent<Rigidbody>();
            rb.mass = 0.8f;
            rb.interpolation = RigidbodyInterpolation.Interpolate;
            rb.collisionDetectionMode = CollisionDetectionMode.ContinuousDynamic;

            var col = root.AddComponent<BoxCollider>();
            col.center = new Vector3(0f, 0.12f, 0f);
            col.size = new Vector3(0.36f, 0.3f, 0.36f);

            Material mat = Mat("M_" + name, color, 0.1f, 0.4f);

            switch (type)
            {
                case CircuitComponentType.Battery:
                {
                    GameObject bodyGo = Prim(PrimitiveType.Cylinder, "Corpo", root.transform,
                        new Vector3(0f, 0.16f, 0f), new Vector3(0.24f, 0.16f, 0.24f), mat);
                    Object.DestroyImmediate(bodyGo.GetComponent<Collider>());

                    Material capMat = Mat("M_Bateria_Polo", new Color(0.85f, 0.85f, 0.88f), 0.8f, 0.7f);
                    GameObject cap = Prim(PrimitiveType.Cylinder, "Polo", root.transform,
                        new Vector3(0f, 0.34f, 0f), new Vector3(0.10f, 0.03f, 0.10f), capMat);
                    Object.DestroyImmediate(cap.GetComponent<Collider>());
                    break;
                }
                case CircuitComponentType.Resistor:
                {
                    GameObject bodyGo = Prim(PrimitiveType.Cylinder, "Corpo", root.transform,
                        new Vector3(0f, 0.14f, 0f), new Vector3(0.14f, 0.16f, 0.14f), mat);
                    bodyGo.transform.localRotation = Quaternion.Euler(0f, 0f, 90f);
                    Object.DestroyImmediate(bodyGo.GetComponent<Collider>());

                    Material bandMat = Mat("M_Resistor_Faixa", BandColor, 0f, 0.3f);
                    for (int i = 0; i < 3; i++)
                    {
                        GameObject band = Prim(PrimitiveType.Cylinder, "Faixa_" + i, root.transform,
                            new Vector3(-0.07f + i * 0.07f, 0.14f, 0f), new Vector3(0.155f, 0.012f, 0.155f), bandMat);
                        band.transform.localRotation = Quaternion.Euler(0f, 0f, 90f);
                        Object.DestroyImmediate(band.GetComponent<Collider>());
                    }

                    Material legMat = Mat("M_Resistor_Perna", new Color(0.75f, 0.75f, 0.78f), 0.85f, 0.65f);
                    for (int s = -1; s <= 1; s += 2)
                    {
                        GameObject leg = Prim(PrimitiveType.Cylinder, "Perna_" + s, root.transform,
                            new Vector3(0.22f * s, 0.14f, 0f), new Vector3(0.03f, 0.06f, 0.03f), legMat);
                        leg.transform.localRotation = Quaternion.Euler(0f, 0f, 90f);
                        Object.DestroyImmediate(leg.GetComponent<Collider>());
                    }
                    break;
                }
                case CircuitComponentType.Wire:
                default:
                {
                    GameObject ring = Prim(PrimitiveType.Cylinder, "Rolo", root.transform,
                        new Vector3(0f, 0.09f, 0f), new Vector3(0.32f, 0.045f, 0.32f), mat);
                    Object.DestroyImmediate(ring.GetComponent<Collider>());

                    GameObject ring2 = Prim(PrimitiveType.Cylinder, "Rolo2", root.transform,
                        new Vector3(0f, 0.17f, 0f), new Vector3(0.26f, 0.04f, 0.26f), mat);
                    Object.DestroyImmediate(ring2.GetComponent<Collider>());
                    break;
                }
            }

            var carryable = root.AddComponent<Carryable>();
            carryable.kind = Carryable.Kind.CircuitPart;
            carryable.partType = type;

            string path = PrefabsFolder + "/" + name + ".prefab";
            GameObject prefab = PrefabUtility.SaveAsPrefabAsset(root, path);
            Object.DestroyImmediate(root);
            return prefab;
        }

        internal static GameObject MakeExtinguisherPrefab(string name, Material particleMat)
        {
            var root = new GameObject(name);

            var rb = root.AddComponent<Rigidbody>();
            rb.mass = 2f;
            rb.interpolation = RigidbodyInterpolation.Interpolate;

            var col = root.AddComponent<CapsuleCollider>();
            col.center = new Vector3(0f, 0.3f, 0f);
            col.height = 0.62f;
            col.radius = 0.14f;

            Material bodyMat = Mat("M_Extintor", ExtinguisherColor, 0.3f, 0.55f);
            GameObject body = Prim(PrimitiveType.Cylinder, "Corpo", root.transform,
                new Vector3(0f, 0.26f, 0f), new Vector3(0.26f, 0.26f, 0.26f), bodyMat);
            Object.DestroyImmediate(body.GetComponent<Collider>());

            Material metalMat = Mat("M_Extintor_Metal", new Color(0.25f, 0.25f, 0.28f), 0.85f, 0.6f);
            GameObject valve = Prim(PrimitiveType.Cylinder, "Valvula", root.transform,
                new Vector3(0f, 0.56f, 0f), new Vector3(0.09f, 0.06f, 0.09f), metalMat);
            Object.DestroyImmediate(valve.GetComponent<Collider>());

            GameObject nozzle = Prim(PrimitiveType.Cylinder, "Bico", root.transform,
                new Vector3(0f, 0.56f, 0.18f), new Vector3(0.05f, 0.13f, 0.05f), metalMat);
            nozzle.transform.localRotation = Quaternion.Euler(90f, 0f, 0f);
            Object.DestroyImmediate(nozzle.GetComponent<Collider>());

            var sprayGo = new GameObject("Jato");
            sprayGo.transform.SetParent(root.transform, false);
            sprayGo.transform.localPosition = new Vector3(0f, 0.56f, 0.3f);
            // o cone já emite ao longo do +Z local, que aqui é a frente do jogador
            ParticleSystem spray = MakeParticles(sprayGo, particleMat,
                color: new Color(0.95f, 0.97f, 1f, 0.55f), lifetime: 0.45f, speed: 9f, size: 0.4f,
                rate: 90f, coneAngle: 14f, radius: 0.04f, gravity: 0f);

            var carryable = root.AddComponent<Carryable>();
            carryable.kind = Carryable.Kind.FireExtinguisher;

            var item = root.AddComponent<FireExtinguisherItem>();
            item.sprayParticles = spray;
            item.range = 3.4f;
            item.extinguishPerSecond = 50f;

            string path = PrefabsFolder + "/" + name + ".prefab";
            GameObject prefab = PrefabUtility.SaveAsPrefabAsset(root, path);
            Object.DestroyImmediate(root);
            return prefab;
        }

        // =====================================================================
        // Helpers de baixo nível
        // =====================================================================

        internal static void EnsureFolders()
        {
            foreach (string folder in new[] { ScenesFolder, PrefabsFolder, MaterialsFolder })
            {
                if (!AssetDatabase.IsValidFolder(folder))
                    AssetDatabase.CreateFolder("Assets", Path.GetFileName(folder));
            }
        }

        /// <summary>
        /// Qual pipeline está REALMENTE ativo. Não adianta o pacote do URP estar
        /// instalado: se nenhum Render Pipeline Asset estiver atribuído, quem roda
        /// é o Built-in — e aí todo material com shader de URP fica rosa choque.
        /// </summary>
        internal static RenderPipelineAsset ActivePipeline()
        {
            return QualitySettings.renderPipeline != null
                ? QualitySettings.renderPipeline
                : GraphicsSettings.defaultRenderPipeline;
        }

        internal static bool UsingUrp()
        {
            RenderPipelineAsset rp = ActivePipeline();
            return rp != null && rp.GetType().Name.Contains("Universal");
        }

        /// <summary>Shader opaco correto para o pipeline ativo.</summary>
        internal static Shader LitShader()
        {
            if (UsingUrp())
            {
                Shader urp = Shader.Find("Universal Render Pipeline/Lit");
                if (urp != null) return urp;
            }

            Shader builtin = Shader.Find("Standard");
            if (builtin != null) return builtin;

            // último recurso: qualquer um dos dois que existir
            return Shader.Find("Universal Render Pipeline/Lit") ?? Shader.Find("Diffuse");
        }

        /// <summary>Shader de partícula correto para o pipeline ativo.</summary>
        internal static Shader ParticleShader()
        {
            if (UsingUrp())
            {
                Shader urp = Shader.Find("Universal Render Pipeline/Particles/Unlit");
                if (urp != null) return urp;
            }

            // no Built-in o shader legado já é aditivo por padrão, sem precisar
            // configurar surface/blend na mão
            return Shader.Find("Legacy Shaders/Particles/Additive")
                   ?? Shader.Find("Particles/Standard Unlit")
                   ?? Shader.Find("Sprites/Default");
        }

        internal static Material Mat(string name, Color color, float metallic, float smoothness)
        {
            string path = $"{MaterialsFolder}/{name}.mat";
            Material m = AssetDatabase.LoadAssetAtPath<Material>(path);
            if (m == null)
            {
                m = new Material(LitShader());
                AssetDatabase.CreateAsset(m, path);
            }
            m.shader = LitShader();
            if (m.HasProperty("_BaseColor")) m.SetColor("_BaseColor", color);
            if (m.HasProperty("_Color")) m.SetColor("_Color", color);
            if (m.HasProperty("_Metallic")) m.SetFloat("_Metallic", metallic);
            if (m.HasProperty("_Smoothness")) m.SetFloat("_Smoothness", smoothness);
            if (m.HasProperty("_Glossiness")) m.SetFloat("_Glossiness", smoothness);
            EditorUtility.SetDirty(m);
            return m;
        }

        internal static Material ParticleMat(string name)
        {
            string path = $"{MaterialsFolder}/{name}.mat";
            Material m = AssetDatabase.LoadAssetAtPath<Material>(path);
            Shader s = ParticleShader();

            if (m == null)
            {
                m = new Material(s);
                AssetDatabase.CreateAsset(m, path);
            }
            m.shader = s;
            if (m.HasProperty("_Surface")) m.SetFloat("_Surface", 1f);  // URP: transparente
            if (m.HasProperty("_Blend")) m.SetFloat("_Blend", 1f);      // URP: aditivo
            if (m.HasProperty("_Mode")) m.SetFloat("_Mode", 4f);        // Built-in: aditivo
            m.renderQueue = 3000;
            EditorUtility.SetDirty(m);
            return m;
        }

        internal static GameObject Prim(PrimitiveType type, string name, Transform parent, Vector3 localPos,
            Vector3 localScale, Material mat)
        {
            GameObject go = GameObject.CreatePrimitive(type);
            go.name = name;
            if (parent != null) go.transform.SetParent(parent, false);
            go.transform.localPosition = localPos;
            go.transform.localScale = localScale;
            if (mat != null)
            {
                var r = go.GetComponent<Renderer>();
                if (r != null) r.sharedMaterial = mat;
            }
            return go;
        }

        internal static ParticleSystem MakeParticles(GameObject host, Material mat, Color color, float lifetime,
            float speed, float size, float rate, float coneAngle, float radius, float gravity, int burstCount = 0,
            ParticleSystemShapeType shapeType = ParticleSystemShapeType.Cone)
        {
            ParticleSystem ps = host.GetComponent<ParticleSystem>();
            if (ps == null) ps = host.AddComponent<ParticleSystem>();

            var main = ps.main;
            main.duration = 2f;
            main.loop = burstCount == 0;
            main.playOnAwake = false;
            main.startLifetime = lifetime;
            main.startSpeed = speed;
            main.startSize = size;
            main.startColor = color;
            main.gravityModifier = gravity;
            main.simulationSpace = ParticleSystemSimulationSpace.World;
            main.maxParticles = 400;

            var emission = ps.emission;
            emission.rateOverTime = rate;
            if (burstCount > 0)
                emission.SetBursts(new[] { new ParticleSystem.Burst(0f, (short)burstCount) });

            var shape = ps.shape;
            shape.enabled = true;
            shape.shapeType = shapeType;
            shape.angle = coneAngle;
            shape.radius = radius;

            var colorOverLifetime = ps.colorOverLifetime;
            colorOverLifetime.enabled = true;
            var grad = new Gradient();
            grad.SetKeys(
                new[] { new GradientColorKey(Color.white, 0f), new GradientColorKey(Color.white, 1f) },
                new[] { new GradientAlphaKey(1f, 0f), new GradientAlphaKey(0f, 1f) });
            colorOverLifetime.color = new ParticleSystem.MinMaxGradient(grad);

            var renderer = host.GetComponent<ParticleSystemRenderer>();
            if (renderer != null && mat != null)
            {
                renderer.sharedMaterial = mat;
                renderer.renderMode = ParticleSystemRenderMode.Billboard;
                renderer.shadowCastingMode = ShadowCastingMode.Off;
                renderer.receiveShadows = false;
            }

            ps.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
            return ps;
        }

        // ---------- UI ----------

        internal static Font UIFont()
        {
            Font f = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            if (f == null) f = Resources.GetBuiltinResource<Font>("Arial.ttf");
            return f;
        }

        internal static Sprite UISprite() => AssetDatabase.GetBuiltinExtraResource<Sprite>("UI/Skin/UISprite.psd");
        internal static Sprite BackgroundSprite() => AssetDatabase.GetBuiltinExtraResource<Sprite>("UI/Skin/Background.psd");
        internal static Sprite KnobSprite() => AssetDatabase.GetBuiltinExtraResource<Sprite>("UI/Skin/Knob.psd");

        internal static RectTransform NewRect(string name, Transform parent, Vector2 anchor, Vector2 pivot,
            Vector2 anchoredPos, Vector2 size)
        {
            var go = new GameObject(name, typeof(RectTransform));
            go.transform.SetParent(parent, false);
            var rt = go.GetComponent<RectTransform>();
            rt.anchorMin = rt.anchorMax = anchor;
            rt.pivot = pivot;
            rt.anchoredPosition = anchoredPos;
            rt.sizeDelta = size;
            return rt;
        }

        internal static void Stretch(RectTransform rt, float padding = 0f)
        {
            rt.anchorMin = Vector2.zero;
            rt.anchorMax = Vector2.one;
            rt.offsetMin = new Vector2(padding, padding);
            rt.offsetMax = new Vector2(-padding, -padding);
        }

        internal static Image MakeImage(string name, Transform parent, Vector2 anchor, Vector2 pivot,
            Vector2 pos, Vector2 size, Color color)
        {
            RectTransform rt = NewRect(name, parent, anchor, pivot, pos, size);
            var img = rt.gameObject.AddComponent<Image>();
            img.sprite = BackgroundSprite();
            img.type = Image.Type.Sliced;
            img.color = color;
            return img;
        }

        internal static Text MakeText(string name, Transform parent, Vector2 anchor, Vector2 pivot,
            Vector2 pos, Vector2 size, string content, int fontSize, TextAnchor align, Color color,
            bool shadow = true, float lineSpacing = 1f)
        {
            RectTransform rt = NewRect(name, parent, anchor, pivot, pos, size);
            var t = rt.gameObject.AddComponent<Text>();
            t.font = UIFont();
            t.fontSize = fontSize;
            t.alignment = align;
            t.color = color;
            t.text = content;
            t.lineSpacing = lineSpacing;
            t.horizontalOverflow = HorizontalWrapMode.Wrap;
            t.verticalOverflow = VerticalWrapMode.Overflow;
            t.raycastTarget = false;
            t.supportRichText = true;

            // sombra dura por baixo: separa o texto de qualquer fundo e é o que
            // mais aumenta a legibilidade sem mudar a fonte
            if (shadow)
            {
                var sh = rt.gameObject.AddComponent<Shadow>();
                sh.effectColor = new Color(0f, 0f, 0f, 0.8f);
                sh.effectDistance = new Vector2(2f, -2f);
            }
            return t;
        }

        internal static Button MakeButton(string name, Transform parent, Vector2 anchor, Vector2 pivot,
            Vector2 pos, Vector2 size, string label, int fontSize, Color color)
        {
            RectTransform rt = NewRect(name, parent, anchor, pivot, pos, size);
            var img = rt.gameObject.AddComponent<Image>();
            img.sprite = UISprite();
            img.type = Image.Type.Sliced;
            img.color = color;

            var btn = rt.gameObject.AddComponent<Button>();
            btn.targetGraphic = img;
            var colors = btn.colors;
            colors.normalColor = Color.white;
            colors.highlightedColor = new Color(1.15f, 1.15f, 1.15f, 1f);
            colors.pressedColor = new Color(0.75f, 0.75f, 0.75f, 1f);
            colors.fadeDuration = 0.05f;
            btn.colors = colors;

            Text t = MakeText("Texto", rt, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f),
                Vector2.zero, size, label, fontSize, TextAnchor.MiddleCenter, Color.white);
            Stretch(t.rectTransform, 4f);
            return btn;
        }

        internal static Slider MakeSlider(string name, Transform parent, Vector2 anchor, Vector2 pivot,
            Vector2 pos, Vector2 size, bool withHandle, out Image fillImage)
        {
            RectTransform rt = NewRect(name, parent, anchor, pivot, pos, size);
            var slider = rt.gameObject.AddComponent<Slider>();
            slider.direction = Slider.Direction.LeftToRight;
            slider.wholeNumbers = false;

            // fundo
            RectTransform bg = NewRect("Background", rt, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f),
                Vector2.zero, Vector2.zero);
            bg.anchorMin = new Vector2(0f, 0.25f);
            bg.anchorMax = new Vector2(1f, 0.75f);
            bg.offsetMin = Vector2.zero;
            bg.offsetMax = Vector2.zero;
            var bgImg = bg.gameObject.AddComponent<Image>();
            bgImg.sprite = BackgroundSprite();
            bgImg.type = Image.Type.Sliced;
            bgImg.color = new Color(0.15f, 0.16f, 0.20f, 1f);

            // área de preenchimento
            RectTransform fillArea = NewRect("Fill Area", rt, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f),
                new Vector2(-5f, 0f), new Vector2(-20f, 0f));
            fillArea.anchorMin = new Vector2(0f, 0.25f);
            fillArea.anchorMax = new Vector2(1f, 0.75f);

            RectTransform fill = NewRect("Fill", fillArea, new Vector2(0f, 0f), new Vector2(0.5f, 0.5f),
                Vector2.zero, new Vector2(10f, 0f));
            fillImage = fill.gameObject.AddComponent<Image>();
            fillImage.sprite = UISprite();
            fillImage.type = Image.Type.Sliced;
            fillImage.color = new Color(0.25f, 0.80f, 0.45f, 1f);
            slider.fillRect = fill;

            if (withHandle)
            {
                RectTransform handleArea = NewRect("Handle Slide Area", rt, new Vector2(0.5f, 0.5f),
                    new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(-20f, 0f));
                handleArea.anchorMin = new Vector2(0f, 0f);
                handleArea.anchorMax = new Vector2(1f, 1f);

                RectTransform handle = NewRect("Handle", handleArea, new Vector2(0f, 0f), new Vector2(0.5f, 0.5f),
                    Vector2.zero, new Vector2(34f, 0f));
                var handleImg = handle.gameObject.AddComponent<Image>();
                handleImg.sprite = KnobSprite();
                handleImg.color = Color.white;
                slider.handleRect = handle;
                slider.targetGraphic = handleImg;
            }

            return slider;
        }

        internal static void RegisterSceneInBuildSettings() => RegisterSceneInBuildSettings(ScenePath, false);

        /// <summary>
        /// Garante que a cena está no Build Settings (sem ela SceneManager.LoadScene
        /// falha em runtime) e limpa referências a cenas que não existem mais.
        /// </summary>
        internal static void RegisterSceneInBuildSettings(string scenePath, bool makeFirst)
        {
            var scenes = new List<EditorBuildSettingsScene>();
            bool found = false;

            foreach (var entry in EditorBuildSettings.scenes)
            {
                if (entry.path == scenePath) { found = true; scenes.Add(new EditorBuildSettingsScene(scenePath, true)); }
                else if (File.Exists(entry.path)) scenes.Add(entry);
            }

            if (!found) scenes.Add(new EditorBuildSettingsScene(scenePath, true));

            if (makeFirst)
            {
                scenes.RemoveAll(e => e.path == scenePath);
                scenes.Insert(0, new EditorBuildSettingsScene(scenePath, true));
            }

            EditorBuildSettings.scenes = scenes.ToArray();
        }
    }
}
