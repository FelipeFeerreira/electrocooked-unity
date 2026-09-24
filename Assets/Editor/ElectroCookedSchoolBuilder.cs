using System.Collections.Generic;
using ElectroCooked.Core;
using ElectroCooked.Player;
using ElectroCooked.School;
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
    /// Constrói o corredor da escola — o "hub" no estilo Bully. Você anda pelo
    /// corredor, chega na porta da matéria e entra na aula. Por enquanto só
    /// Circuitos I está aberta; as outras portas existem para dar a sensação de
    /// grade curricular e são o gancho para as próximas matérias.
    ///
    /// Usa os mesmos helpers do ElectroCookedSceneBuilder, então a escola e a
    /// sala de aula compartilham paleta, materiais e estilo de UI.
    /// </summary>
    public static class ElectroCookedSchoolBuilder
    {
        const string SchoolScenePath = ElectroCookedSceneBuilder.ScenesFolder + "/Escola.unity";

        static readonly Color CorridorFloor = new Color(0.62f, 0.60f, 0.56f);
        static readonly Color CorridorWall = new Color(0.46f, 0.50f, 0.56f);
        static readonly Color LockerColor = new Color(0.20f, 0.38f, 0.45f);
        static readonly Color TrimColor = new Color(0.30f, 0.33f, 0.38f);

        struct DoorSpec
        {
            public string subjectId, display, scene;
            public bool open;
            public float x;
        }

        static readonly DoorSpec[] Doors =
        {
            new DoorSpec { subjectId = SubjectIds.Circuits,  display = "CIRCUITOS I", scene = SceneNames.CircuitClass, open = true,  x = -15f },
            new DoorSpec { subjectId = SubjectIds.Calculus,  display = "CALCULO I",   scene = SceneNames.CalculusClass, open = true,  x = -5f },
            new DoorSpec { subjectId = SubjectIds.Physics,   display = "FISICA",      scene = "",                      open = false, x = 5f },
            new DoorSpec { subjectId = SubjectIds.Chemistry, display = "QUIMICA",     scene = "",                      open = false, x = 15f },
        };

        [MenuItem("ElectroCooked/Construir TUDO (escola + aula)", false, -10)]
        public static void BuildEverything()
        {
            if (EditorApplication.isPlaying)
            {
                EditorUtility.DisplayDialog("ElectroCooked", "Saia do modo Play antes de construir.", "Ok");
                return;
            }
            if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) return;

            ElectroCookedSceneBuilder.BuildPlayableScene(showDialog: false);
            CalculusSceneBuilder.BuildCalculusScene(showDialog: false);
            BuildSchoolScene(showDialog: false);

            // a escola é a primeira cena: é por ela que o jogo começa
            ElectroCookedSceneBuilder.RegisterSceneInBuildSettings(SchoolScenePath, true);
            ElectroCookedSceneBuilder.RegisterSceneInBuildSettings(ElectroCookedSceneBuilder.ScenePath, false);
            ElectroCookedSceneBuilder.RegisterSceneInBuildSettings(CalculusSceneBuilder.ScenePath, false);

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();

            EditorUtility.DisplayDialog("ElectroCooked",
                "Escola + aulas prontas!\n\n" +
                "A cena ESCOLA já está aberta. Aperte PLAY e:\n\n" +
                "  WASD  - andar pelo corredor\n" +
                "  E     - entrar em CIRCUITOS I ou CALCULO I\n\n" +
                "Circuitos:\n" +
                "  E pegar/encaixar   ESPACO extintor\n" +
                "  C calculadora      F formulas\n\n" +
                "Calculo:\n" +
                "  corra ate o circulo com a resposta certa\n\n" +
                "No fim da aula: R repete, ENTER volta pra escola.",
                "Bora!");
        }

        [MenuItem("ElectroCooked/Construir só a escola", false, 11)]
        public static void BuildSchoolMenu()
        {
            if (EditorApplication.isPlaying) return;
            if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) return;
            BuildSchoolScene(showDialog: true);
            ElectroCookedSceneBuilder.RegisterSceneInBuildSettings(SchoolScenePath, true);
        }

        [MenuItem("ElectroCooked/Zerar boletim do aluno", false, 40)]
        public static void ResetReport()
        {
            StudentRecord.ResetAll();
            Debug.Log("[ElectroCooked] Boletim zerado.");
        }

        static void BuildSchoolScene(bool showDialog)
        {
            ElectroCookedSceneBuilder.EnsureFolders();
            ElectroCookedSceneBuilder.ApplyPalette();

            Material floorMat = ElectroCookedSceneBuilder.Mat("M_Escola_Piso", CorridorFloor, 0f, 0.25f);
            Material wallMat = ElectroCookedSceneBuilder.Mat("M_Escola_Parede", CorridorWall, 0f, 0.12f);
            Material lockerMat = ElectroCookedSceneBuilder.Mat("M_Escola_Armario", LockerColor, 0.2f, 0.4f);
            Material trimMat = ElectroCookedSceneBuilder.Mat("M_Escola_Rodape", TrimColor, 0.1f, 0.3f);
            Material doorMat = ElectroCookedSceneBuilder.Mat("M_Escola_Porta", new Color(0.30f, 0.31f, 0.36f), 0f, 0.35f);

            Scene scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);

            RenderSettings.ambientMode = AmbientMode.Trilight;
            RenderSettings.ambientSkyColor = new Color(0.58f, 0.60f, 0.66f);
            RenderSettings.ambientEquatorColor = new Color(0.44f, 0.44f, 0.47f);
            RenderSettings.ambientGroundColor = new Color(0.26f, 0.25f, 0.24f);
            RenderSettings.fog = false;

            ElectroCookedSceneBuilder.BuildLighting();
            BuildCorridor(floorMat, wallMat, lockerMat, trimMat);

            GameObject player = BuildSchoolPlayer();
            Camera cam = BuildFollowCamera(player.transform);
            var controller = player.GetComponent<PlayerController>();
            if (controller != null) controller.cameraTransform = cam.transform;

            var doors = new List<ClassroomDoor>();
            foreach (DoorSpec spec in Doors)
                doors.Add(BuildDoor(spec, doorMat));

            BuildSchoolUI(player, doors);

            Undo.ClearAll();
            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene, SchoolScenePath);
            AssetDatabase.SaveAssets();

            Debug.Log($"[ElectroCooked] Escola construída em {SchoolScenePath}. " +
                      "WASD anda, E entra na sala de CIRCUITOS I.");

            if (showDialog)
                EditorUtility.DisplayDialog("ElectroCooked",
                    "Corredor da escola pronto!\n\nAperte PLAY: WASD anda, E entra em CIRCUITOS I.", "Ok");
        }

        // =====================================================================

        static void BuildCorridor(Material floorMat, Material wallMat, Material lockerMat, Material trimMat)
        {
            var root = new GameObject("Corredor");

            // corredor de 44 x 16
            GameObject floor = ElectroCookedSceneBuilder.Prim(PrimitiveType.Plane, "Piso", root.transform,
                Vector3.zero, new Vector3(4.4f, 1f, 1.6f), floorMat);
            floor.isStatic = true;

            // faixa central no chão, ajuda a ler a direção do corredor
            Material stripeMat = ElectroCookedSceneBuilder.Mat("M_Escola_Faixa", new Color(0.78f, 0.72f, 0.40f), 0f, 0.2f);
            GameObject stripe = ElectroCookedSceneBuilder.Prim(PrimitiveType.Cube, "Faixa", root.transform,
                new Vector3(0f, 0.01f, 0f), new Vector3(42f, 0.02f, 0.5f), stripeMat);
            Object.DestroyImmediate(stripe.GetComponent<Collider>());

            Wall(root.transform, "Parede_Fundo", new Vector3(0f, 2f, 8f), new Vector3(44f, 4f, 0.5f), wallMat);
            Wall(root.transform, "Parede_Frente", new Vector3(0f, 2f, -8f), new Vector3(44f, 4f, 0.5f), wallMat);
            Wall(root.transform, "Parede_Esq", new Vector3(-22f, 2f, 0f), new Vector3(0.5f, 4f, 16f), wallMat);
            Wall(root.transform, "Parede_Dir", new Vector3(22f, 2f, 0f), new Vector3(0.5f, 4f, 16f), wallMat);

            // armários ao longo da parede da frente — a cara de corredor de colégio
            for (int i = 0; i < 18; i++)
            {
                float x = -19f + i * 2.25f;
                GameObject locker = ElectroCookedSceneBuilder.Prim(PrimitiveType.Cube, $"Armario_{i}", root.transform,
                    new Vector3(x, 1.1f, -7.3f), new Vector3(2f, 2.2f, 0.7f), lockerMat);
                locker.isStatic = true;

                GameObject vent = ElectroCookedSceneBuilder.Prim(PrimitiveType.Cube, "Frisa", root.transform,
                    new Vector3(x, 1.85f, -6.93f), new Vector3(1.4f, 0.12f, 0.06f), trimMat);
                Object.DestroyImmediate(vent.GetComponent<Collider>());
            }

            // rodapé da parede do fundo
            GameObject baseboard = ElectroCookedSceneBuilder.Prim(PrimitiveType.Cube, "Rodape", root.transform,
                new Vector3(0f, 0.2f, 7.7f), new Vector3(44f, 0.4f, 0.12f), trimMat);
            Object.DestroyImmediate(baseboard.GetComponent<Collider>());
        }

        static void Wall(Transform parent, string name, Vector3 pos, Vector3 scale, Material mat)
        {
            GameObject w = ElectroCookedSceneBuilder.Prim(PrimitiveType.Cube, name, parent, pos, scale, mat);
            w.isStatic = true;
        }

        static GameObject BuildSchoolPlayer()
        {
            var player = new GameObject("Aluno");
            player.transform.position = new Vector3(-15f, 0.1f, -2.5f);

            var cc = player.AddComponent<CharacterController>();
            cc.height = 1.7f;
            cc.radius = 0.35f;
            cc.center = new Vector3(0f, 0.85f, 0f);
            cc.stepOffset = 0.35f;

            Material bodyMat = ElectroCookedSceneBuilder.Mat("M_Aluno", ElectroCookedSceneBuilder.PlayerColor, 0f, 0.35f);
            Material accentMat = ElectroCookedSceneBuilder.Mat("M_Aluno_Detalhe", ElectroCookedSceneBuilder.PlayerAccent, 0f, 0.4f);
            Material markerMat = ElectroCookedSceneBuilder.Mat("M_Aluno_Marcador", ElectroCookedSceneBuilder.PlayerMarker, 0f, 0.55f);

            GameObject body = ElectroCookedSceneBuilder.Prim(PrimitiveType.Capsule, "Corpo", player.transform,
                new Vector3(0f, 0.85f, 0f), new Vector3(0.7f, 0.6f, 0.7f), bodyMat);
            Object.DestroyImmediate(body.GetComponent<Collider>());

            GameObject head = ElectroCookedSceneBuilder.Prim(PrimitiveType.Sphere, "Cabeca", player.transform,
                new Vector3(0f, 1.62f, 0f), new Vector3(0.5f, 0.5f, 0.5f), accentMat);
            Object.DestroyImmediate(head.GetComponent<Collider>());

            GameObject nose = ElectroCookedSceneBuilder.Prim(PrimitiveType.Cube, "Frente", player.transform,
                new Vector3(0f, 1.6f, 0.28f), new Vector3(0.16f, 0.16f, 0.22f), markerMat);
            Object.DestroyImmediate(nose.GetComponent<Collider>());

            var controller = player.AddComponent<PlayerController>();
            controller.moveSpeed = 6.5f;
            return player;
        }

        static Camera BuildFollowCamera(Transform target)
        {
            var camGo = new GameObject("Main Camera");
            camGo.tag = "MainCamera";
            camGo.transform.rotation = Quaternion.Euler(50f, 0f, 0f);

            Camera cam = camGo.AddComponent<Camera>();
            cam.fieldOfView = 50f;
            cam.nearClipPlane = 0.3f;
            cam.farClipPlane = 160f;
            cam.clearFlags = CameraClearFlags.SolidColor;
            cam.backgroundColor = new Color(0.07f, 0.08f, 0.11f);
            camGo.AddComponent<AudioListener>();

            var follow = camGo.AddComponent<CameraFollow>();
            follow.target = target;
            follow.offset = new Vector3(0f, 12f, -9.5f);
            follow.smoothTime = 0.16f;
            follow.followX = true;
            follow.followZ = true;

            camGo.transform.position = target.position + follow.offset;
            return cam;
        }

        static ClassroomDoor BuildDoor(DoorSpec spec, Material frameMat)
        {
            var root = new GameObject("Porta_" + spec.display.Replace(' ', '_'));
            root.transform.position = new Vector3(spec.x, 0f, 7.4f);

            // batente
            GameObject frame = ElectroCookedSceneBuilder.Prim(PrimitiveType.Cube, "Batente", root.transform,
                new Vector3(0f, 1.8f, 0.25f), new Vector3(4f, 3.6f, 0.3f), frameMat);
            frame.isStatic = true;

            // a folha da porta é o que muda de cor: verde se aberta, cinza se fechada
            Material leafMat = ElectroCookedSceneBuilder.Mat("M_Porta_" + spec.subjectId,
                spec.open ? new Color(0.20f, 0.75f, 0.45f) : new Color(0.30f, 0.31f, 0.36f), 0f, 0.4f);
            GameObject leaf = ElectroCookedSceneBuilder.Prim(PrimitiveType.Cube, "Folha", root.transform,
                new Vector3(0f, 1.55f, 0.05f), new Vector3(3f, 3.1f, 0.2f), leafMat);
            Object.DestroyImmediate(leaf.GetComponent<Collider>());

            // luz que acende quando o aluno chega perto
            var lightGo = new GameObject("Luz");
            lightGo.transform.SetParent(root.transform, false);
            lightGo.transform.localPosition = new Vector3(0f, 2.6f, -1.2f);
            Light light = lightGo.AddComponent<Light>();
            light.type = LightType.Point;
            light.color = spec.open ? new Color(0.5f, 1f, 0.7f) : new Color(1f, 0.7f, 0.4f);
            light.intensity = 5f;
            light.range = 8f;
            light.enabled = false;

            // placa acima da porta
            var canvasGo = new GameObject("Placa", typeof(RectTransform));
            canvasGo.transform.SetParent(root.transform, false);
            canvasGo.transform.localPosition = new Vector3(0f, 3.95f, -0.1f);
            canvasGo.transform.localScale = Vector3.one * 0.0085f;
            var canvas = canvasGo.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.WorldSpace;
            var rt = canvasGo.GetComponent<RectTransform>();
            rt.sizeDelta = new Vector2(520f, 110f);

            Image bg = ElectroCookedSceneBuilder.MakeImage("Fundo", rt, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f),
                Vector2.zero, new Vector2(520f, 110f), new Color(0.05f, 0.06f, 0.09f, 0.94f));
            ElectroCookedSceneBuilder.Stretch(bg.rectTransform);

            string label = spec.open ? spec.display : spec.display + "  (fechada)";
            Text t = ElectroCookedSceneBuilder.MakeText("Texto", bg.rectTransform, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f),
                Vector2.zero, new Vector2(500f, 100f), label, 54, TextAnchor.MiddleCenter,
                spec.open ? Color.white : new Color(0.72f, 0.74f, 0.78f));
            ElectroCookedSceneBuilder.Stretch(t.rectTransform, 8f);

            canvasGo.AddComponent<Billboard>();

            var door = root.AddComponent<ClassroomDoor>();
            door.subjectId = spec.subjectId;
            door.displayName = spec.display;
            door.sceneToLoad = spec.scene;
            door.isOpen = spec.open;
            door.interactRange = 3.4f;
            door.doorRenderer = leaf.GetComponent<Renderer>();
            door.doorLight = light;
            return door;
        }

        // =====================================================================

        static void BuildSchoolUI(GameObject player, List<ClassroomDoor> doors)
        {
            var eventSystemGo = new GameObject("EventSystem");
            eventSystemGo.AddComponent<EventSystem>();
#if ENABLE_INPUT_SYSTEM
            var module = eventSystemGo.AddComponent<InputSystemUIInputModule>();
            module.AssignDefaultActions();
#else
            eventSystemGo.AddComponent<StandaloneInputModule>();
#endif

            var canvasGo = new GameObject("Canvas_Escola", typeof(RectTransform));
            var canvas = canvasGo.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            var scaler = canvasGo.AddComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1280f, 720f);
            scaler.screenMatchMode = CanvasScaler.ScreenMatchMode.MatchWidthOrHeight;
            scaler.matchWidthOrHeight = 0.5f;
            canvasGo.AddComponent<GraphicRaycaster>();
            var canvasRt = canvasGo.GetComponent<RectTransform>();

            Image titleBar = ElectroCookedSceneBuilder.MakeImage("BarraTitulo", canvasRt,
                new Vector2(0.5f, 1f), new Vector2(0.5f, 1f),
                new Vector2(0f, -10f), new Vector2(600f, 54f), new Color(0.05f, 0.06f, 0.09f, 0.92f));
            Text title = ElectroCookedSceneBuilder.MakeText("Texto", titleBar.rectTransform,
                new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f),
                Vector2.zero, new Vector2(580f, 46f), "", 28, TextAnchor.MiddleCenter, Color.white);
            ElectroCookedSceneBuilder.Stretch(title.rectTransform, 8f);

            // boletim no canto superior direito
            Image reportPanel = ElectroCookedSceneBuilder.MakeImage("PainelBoletim", canvasRt,
                new Vector2(1f, 1f), new Vector2(1f, 1f),
                new Vector2(-18f, -18f), new Vector2(320f, 200f), new Color(0.05f, 0.06f, 0.09f, 0.92f));
            Text report = ElectroCookedSceneBuilder.MakeText("Texto", reportPanel.rectTransform,
                new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f),
                Vector2.zero, new Vector2(292f, 176f), "", 22, TextAnchor.UpperLeft, Color.white,
                shadow: true, lineSpacing: 1.3f);
            ElectroCookedSceneBuilder.Stretch(report.rectTransform, 16f);

            // aviso grande de interação, na parte de baixo
            Image promptBg = ElectroCookedSceneBuilder.MakeImage("FundoAviso", canvasRt,
                new Vector2(0.5f, 0f), new Vector2(0.5f, 0f),
                new Vector2(0f, 74f), new Vector2(760f, 64f), new Color(0.05f, 0.06f, 0.09f, 0.90f));
            Text prompt = ElectroCookedSceneBuilder.MakeText("Texto", promptBg.rectTransform,
                new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f),
                Vector2.zero, new Vector2(740f, 56f), "", 32, TextAnchor.MiddleCenter, Color.white);
            ElectroCookedSceneBuilder.Stretch(prompt.rectTransform, 8f);

            Text hint = ElectroCookedSceneBuilder.MakeText("Dica", canvasRt,
                new Vector2(0.5f, 0f), new Vector2(0.5f, 0f),
                new Vector2(0f, 20f), new Vector2(900f, 34f), "", 20, TextAnchor.MiddleCenter,
                new Color(1f, 1f, 1f, 0.7f));

            var hud = canvasGo.AddComponent<SchoolHUD>();
            hud.player = player.transform;
            hud.doors = new List<ClassroomDoor>(doors);
            hud.titleText = title;
            hud.promptText = prompt;
            hud.reportText = report;
            hud.hintText = hint;
        }
    }
}
