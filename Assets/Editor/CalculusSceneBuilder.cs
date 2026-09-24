using ElectroCooked.Calculus;
using ElectroCooked.Core;
using ElectroCooked.Managers;
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
    /// Gera do zero a sala de Cálculo (Aula_Calculo) — o minigame estilo Bully:
    /// o professor pergunta na lousa e o aluno corre até o círculo do chão com a
    /// resposta certa. Mesmo padrão do ElectroCookedSceneBuilder: rebuild dá pra
    /// iterar no layout sem arrastar nada.
    /// </summary>
    public static class CalculusSceneBuilder
    {
        internal const string ScenePath = ElectroCookedSceneBuilder.ScenesFolder + "/Aula_Calculo.unity";

        // Paleta da sala de cálculo: tons de quadro-verde e giz, bem diferente da
        // sala de circuitos para a troca de matéria ser óbvia.
        static readonly Color FloorColor = new Color(0.66f, 0.64f, 0.58f);
        static readonly Color WallColor = new Color(0.45f, 0.42f, 0.38f);
        static readonly Color ChalkboardColor = new Color(0.06f, 0.28f, 0.20f);
        static readonly Color ChalkFrameColor = new Color(0.35f, 0.26f, 0.16f);
        static readonly Color ZoneIdleColor = new Color(0.16f, 0.18f, 0.24f);
        static readonly Color ZoneHoverColor = new Color(0.30f, 0.55f, 0.90f);
        static readonly Color ZoneCorrectColor = new Color(0.20f, 0.80f, 0.45f);
        static readonly Color ZoneWrongColor = new Color(0.90f, 0.28f, 0.22f);
        static readonly Color ProfessorRobesColor = new Color(0.20f, 0.24f, 0.32f);

        static readonly Vector3[] ZonePositions =
        {
            new Vector3(-5.4f, 0f, 2.8f),
            new Vector3( 5.4f, 0f, 2.8f),
            new Vector3(-5.4f, 0f, -2.8f),
            new Vector3( 5.4f, 0f, -2.8f),
        };

        [MenuItem("ElectroCooked/Construir sala de cálculo", false, 12)]
        public static void BuildCalculusMenu()
        {
            if (EditorApplication.isPlaying)
            {
                EditorUtility.DisplayDialog("ElectroCooked", "Saia do modo Play antes de construir.", "Ok");
                return;
            }
            if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) return;
            BuildCalculusScene(showDialog: true);
            ElectroCookedSceneBuilder.RegisterSceneInBuildSettings(ScenePath, false);
        }

        public static void BuildCalculusScene(bool showDialog)
        {
            ElectroCookedSceneBuilder.EnsureFolders();
            ElectroCookedSceneBuilder.ApplyPalette();

            Material floorMat = ElectroCookedSceneBuilder.Mat("M_Calc_Piso", FloorColor, 0f, 0.25f);
            Material wallMat = ElectroCookedSceneBuilder.Mat("M_Calc_Parede", WallColor, 0f, 0.12f);
            Material boardMat = ElectroCookedSceneBuilder.Mat("M_Calc_Lousa", ChalkboardColor, 0f, 0.2f);
            Material frameMat = ElectroCookedSceneBuilder.Mat("M_Calc_Moldura", ChalkFrameColor, 0.1f, 0.3f);
            Material robeMat = ElectroCookedSceneBuilder.Mat("M_Calc_Professor", ProfessorRobesColor, 0f, 0.35f);

            Material[] zoneMats = new Material[4];
            for (int i = 0; i < 4; i++)
            {
                zoneMats[i] = ElectroCookedSceneBuilder.Mat("M_Calc_Zona_" + (i + 1),
                    ZoneIdleColor, 0f, 0.4f);
            }

            Scene scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);

            RenderSettings.ambientMode = AmbientMode.Trilight;
            RenderSettings.ambientSkyColor = new Color(0.60f, 0.60f, 0.58f);
            RenderSettings.ambientEquatorColor = new Color(0.44f, 0.42f, 0.40f);
            RenderSettings.ambientGroundColor = new Color(0.25f, 0.24f, 0.22f);
            RenderSettings.fog = false;

            ElectroCookedSceneBuilder.BuildLighting();
            ElectroCookedSceneBuilder.BuildCamera();
            BuildRoom(floorMat, wallMat, boardMat, frameMat);
            BuildProfessor(robeMat);

            GameObject player = BuildPlayer();
            var zones = BuildZones(zoneMats);

            BuildManagers(player, zones);
            BuildUI(player);

            Undo.ClearAll();
            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene, ScenePath);
            ElectroCookedSceneBuilder.RegisterSceneInBuildSettings(ScenePath, false);

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();

            Selection.activeGameObject = player;
            Debug.Log($"[ElectroCooked] Sala de cálculo construída em {ScenePath}. " +
                      "Aperte Play: WASD anda até a resposta certa.");
            if (showDialog)
                EditorUtility.DisplayDialog("ElectroCooked",
                    "Sala de CÁLCULO criada!\n\n" +
                    "Aperte PLAY:\n" +
                    "  WASD  - andar até o círculo com a resposta certa\n\n" +
                    "O professor pergunta e os 4 círculos acendem no chão.\n" +
                    "3 erros (ou tempo esgotado) = aula encerrada.",
                    "Bora!");
        }

        // =====================================================================

        static void BuildRoom(Material floorMat, Material wallMat, Material boardMat, Material frameMat)
        {
            var room = new GameObject("Sala_de_Calculo");

            GameObject floor = ElectroCookedSceneBuilder.Prim(PrimitiveType.Plane, "Piso", room.transform,
                Vector3.zero, new Vector3(1.6f, 1f, 1.2f), floorMat);
            floor.isStatic = true;

            ElectroCookedSceneBuilder.MakeWall(room.transform, "Parede_Fundo", new Vector3(0f, 1.5f, 6f),
                new Vector3(16f, 3f, 0.4f), wallMat);
            ElectroCookedSceneBuilder.MakeWall(room.transform, "Parede_Frente", new Vector3(0f, 1.5f, -6f),
                new Vector3(16f, 3f, 0.4f), wallMat);
            ElectroCookedSceneBuilder.MakeWall(room.transform, "Parede_Esq", new Vector3(-8f, 1.5f, 0f),
                new Vector3(0.4f, 3f, 12f), wallMat);
            ElectroCookedSceneBuilder.MakeWall(room.transform, "Parede_Dir", new Vector3(8f, 1.5f, 0f),
                new Vector3(0.4f, 3f, 12f), wallMat);

            GameObject board = ElectroCookedSceneBuilder.Prim(PrimitiveType.Cube, "Lousa", room.transform,
                new Vector3(0f, 2f, 5.72f), new Vector3(9f, 2.6f, 0.12f), boardMat);
            Object.DestroyImmediate(board.GetComponent<Collider>());
            board.isStatic = true;

            GameObject frame = ElectroCookedSceneBuilder.Prim(PrimitiveType.Cube, "Moldura", room.transform,
                new Vector3(0f, 2f, 5.8f), new Vector3(9.4f, 0.2f, 0.06f), frameMat);
            Object.DestroyImmediate(frame.GetComponent<Collider>());
        }

        static void BuildProfessor(Material robeMat)
        {
            var teacher = new GameObject("Professor");

            Material skinMat = ElectroCookedSceneBuilder.Mat("M_Calc_Professor_Rosto", new Color(0.75f, 0.58f, 0.45f), 0f, 0.4f);

            GameObject podium = ElectroCookedSceneBuilder.Prim(PrimitiveType.Cube, "Mesinha", teacher.transform,
                new Vector3(0f, 0.6f, -5.5f), new Vector3(1.4f, 1.2f, 0.9f), robeMat);
            podium.isStatic = true;

            GameObject body = ElectroCookedSceneBuilder.Prim(PrimitiveType.Capsule, "Corpo", teacher.transform,
                new Vector3(0f, 1.6f, -5.5f), new Vector3(0.66f, 0.55f, 0.66f), robeMat);
            Object.DestroyImmediate(body.GetComponent<Collider>());

            GameObject head = ElectroCookedSceneBuilder.Prim(PrimitiveType.Sphere, "Cabeca", teacher.transform,
                new Vector3(0f, 2.35f, -5.5f), new Vector3(0.45f, 0.45f, 0.45f), skinMat);
            Object.DestroyImmediate(head.GetComponent<Collider>());
        }

        static GameObject BuildPlayer()
        {
            var player = new GameObject("Aluno");
            player.transform.position = new Vector3(0f, 0.1f, -2.5f);

            var cc = player.AddComponent<CharacterController>();
            cc.height = 1.7f;
            cc.radius = 0.35f;
            cc.center = new Vector3(0f, 0.85f, 0f);
            cc.slopeLimit = 50f;
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
            controller.moveSpeed = 5.5f;

            return player;
        }

        static AnswerZone[] BuildZones(Material[] mats)
        {
            var zones = new AnswerZone[4];
            for (int i = 0; i < 4; i++)
            {
                var root = new GameObject("Zona_" + (i + 1));
                root.transform.position = ZonePositions[i];

                GameObject circle = ElectroCookedSceneBuilder.Prim(PrimitiveType.Cylinder, "Circulo", root.transform,
                    new Vector3(0f, 0.06f, 0f), new Vector3(1.3f, 0.05f, 1.3f), mats[i]);
                Object.DestroyImmediate(circle.GetComponent<Collider>());

                GameObject ring = ElectroCookedSceneBuilder.Prim(PrimitiveType.Cylinder, "Anel", root.transform,
                    new Vector3(0f, 0.11f, 0f), new Vector3(1.45f, 0.015f, 1.45f),
                    ElectroCookedSceneBuilder.Mat("M_Calc_Zona_Anel", ZoneHoverColor, 0f, 0.5f));
                Object.DestroyImmediate(ring.GetComponent<Collider>());

                var canvasGo = new GameObject("Rotulo", typeof(RectTransform));
                canvasGo.transform.SetParent(root.transform, false);
                canvasGo.transform.localPosition = new Vector3(0f, 1.05f, 0f);
                canvasGo.transform.localScale = Vector3.one * 0.006f;
                var canvas = canvasGo.AddComponent<Canvas>();
                canvas.renderMode = RenderMode.WorldSpace;
                var rt = canvasGo.GetComponent<RectTransform>();
                rt.sizeDelta = new Vector2(520f, 110f);

                Image bg = ElectroCookedSceneBuilder.MakeImage("Fundo", rt, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f),
                    Vector2.zero, new Vector2(520f, 110f), new Color(0.05f, 0.06f, 0.09f, 0.94f));
                ElectroCookedSceneBuilder.Stretch(bg.rectTransform);

                Text txt = ElectroCookedSceneBuilder.MakeText("Texto", bg.rectTransform, new Vector2(0.5f, 0.5f),
                    new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(500f, 96f), "", 56, TextAnchor.MiddleCenter,
                    Color.white);
                ElectroCookedSceneBuilder.Stretch(txt.rectTransform, 8f);

                canvasGo.AddComponent<Billboard>();

                var zone = root.AddComponent<AnswerZone>();
                zone.label = txt;
                zone.surfaceRenderer = circle.GetComponent<Renderer>();
                zone.idleColor = ZoneIdleColor;
                zone.highlightColor = ZoneHoverColor;
                zone.correctFlashColor = ZoneCorrectColor;
                zone.wrongFlashColor = ZoneWrongColor;

                zones[i] = zone;
            }
            return zones;
        }

        // =====================================================================
        // Managers
        // =====================================================================

        static void BuildManagers(GameObject player, AnswerZone[] zones)
        {
            var go = new GameObject("_Managers");

            var score = go.AddComponent<ScoreManager>();
            score.pointsForFullGrade = 8f;
            score.penaltyPerMistake = 0.25f;

            var game = go.AddComponent<GameManager>();
            game.subjectId = SubjectIds.Calculus;
            game.subjectName = "CALCULO I";
            game.classDurationSeconds = 300f;
            game.secondsPerDifficultyStep = 90f;

            var calc = go.AddComponent<CalculusGameManager>();
            calc.player = player.transform;
            calc.zones = zones;
            calc.startingLives = 3;
            calc.zoneRadius = 1.05f;
            calc.delayBetweenQuestions = 1.2f;
            calc.questionGraceSeconds = 0.5f;
        }

        // =====================================================================
        // UI de tela
        // =====================================================================

        static void BuildUI(GameObject player)
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
            Image topBar = ElectroCookedSceneBuilder.MakeImage("BarraSuperior", canvasRt,
                new Vector2(0.5f, 1f), new Vector2(0.5f, 1f),
                new Vector2(0f, -10f), new Vector2(860f, 62f), new Color(0.05f, 0.06f, 0.09f, 0.92f));

            Text timeText = ElectroCookedSceneBuilder.MakeText("Tempo", topBar.rectTransform,
                new Vector2(0f, 0.5f), new Vector2(0f, 0.5f),
                new Vector2(22f, 0f), new Vector2(170f, 52f), "05:00", 40, TextAnchor.MiddleLeft, Color.white);
            Text gradeText = ElectroCookedSceneBuilder.MakeText("Nota", topBar.rectTransform,
                new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f),
                new Vector2(20f, 0f), new Vector2(220f, 52f), "NOTA 0.0", 32, TextAnchor.MiddleCenter,
                new Color(0.45f, 0.95f, 0.60f));
            Text mistakesText = ElectroCookedSceneBuilder.MakeText("Erros", topBar.rectTransform,
                new Vector2(1f, 0.5f), new Vector2(1f, 0.5f),
                new Vector2(-66f, 0f), new Vector2(180f, 52f), "ERROS 0", 26, TextAnchor.MiddleRight,
                new Color(0.98f, 0.55f, 0.42f));
            Text comboText = ElectroCookedSceneBuilder.MakeText("Combo", topBar.rectTransform,
                new Vector2(1f, 0.5f), new Vector2(1f, 0.5f),
                new Vector2(-22f, 0f), new Vector2(120f, 52f), "COMBO x1", 22, TextAnchor.MiddleRight,
                new Color(1f, 0.80f, 0.25f));
            comboText.gameObject.SetActive(false);

            Text hint = ElectroCookedSceneBuilder.MakeText("DicaControles", canvasRt,
                new Vector2(0.5f, 0f), new Vector2(0.5f, 0f),
                new Vector2(0f, 16f), new Vector2(1160f, 34f), "", 20, TextAnchor.MiddleCenter,
                new Color(1f, 1f, 1f, 0.7f));

            // ---- painel de resumo (boletim), igual na sala de circuitos ----
            Image summary = ElectroCookedSceneBuilder.MakeImage("PainelResumo", canvasRt,
                new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f),
                Vector2.zero, new Vector2(660f, 430f), new Color(0.04f, 0.05f, 0.08f, 0.98f));
            Text summaryText = ElectroCookedSceneBuilder.MakeText("Texto", summary.rectTransform,
                new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f),
                Vector2.zero, new Vector2(600f, 380f), "", 28, TextAnchor.MiddleCenter, Color.white,
                shadow: true, lineSpacing: 1.25f);
            ElectroCookedSceneBuilder.Stretch(summaryText.rectTransform, 28f);
            summary.gameObject.SetActive(false);

            var hud = canvasGo.AddComponent<HUDUI>();
            hud.timeText = timeText;
            hud.gradeText = gradeText;
            hud.mistakesText = mistakesText;
            hud.comboText = comboText;
            hud.controlsHintText = hint;
            hud.controlsHintOverride = "WASD andar ate o circulo com a resposta certa";
            hud.deliveredLabel = "Respostas certas:";
            hud.mistakeLabel = "Erros:";
            hud.summaryPanel = summary.gameObject;
            hud.summaryText = summaryText;

            // ---- painel central da pergunta ----
            BuildCalculusPanel(canvasGo, canvasRt, hud);
        }

        static void BuildCalculusPanel(GameObject canvasGo, RectTransform canvasRt, HUDUI hud)
        {
            Image panel = ElectroCookedSceneBuilder.MakeImage("PainelCalculo", canvasRt,
                new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f),
                new Vector2(0f, 120f), new Vector2(760f, 250f), new Color(0.04f, 0.05f, 0.08f, 0.97f));

            ElectroCookedSceneBuilder.MakeText("Cabecalho", panel.rectTransform,
                new Vector2(0.5f, 1f), new Vector2(0.5f, 1f),
                new Vector2(0f, -8f), new Vector2(720f, 30f), "PROFESSOR(A) PERGUNTA:", 22,
                TextAnchor.MiddleCenter, new Color(0.98f, 0.82f, 0.35f));

            Text question = ElectroCookedSceneBuilder.MakeText("Pergunta", panel.rectTransform,
                new Vector2(0.5f, 1f), new Vector2(0.5f, 1f),
                new Vector2(0f, -42f), new Vector2(720f, 92f), "", 42, TextAnchor.MiddleCenter, Color.white,
                shadow: true, lineSpacing: 1.1f);

            Slider timer = ElectroCookedSceneBuilder.MakeSlider("BarraTempo", panel.rectTransform,
                new Vector2(0.5f, 1f), new Vector2(0.5f, 1f),
                new Vector2(0f, -148f), new Vector2(700f, 18f), false, out Image timerFill);
            timer.interactable = false;
            timer.minValue = 0f;
            timer.maxValue = 1f;
            timer.value = 1f;

            Text lives = ElectroCookedSceneBuilder.MakeText("Vidas", panel.rectTransform,
                new Vector2(0, 0f), new Vector2(0f, 0f),
                new Vector2(20f, 14f), new Vector2(200f, 40f), "VIDAS: 3", 24, TextAnchor.MiddleLeft, Color.white);

            Text feedback = ElectroCookedSceneBuilder.MakeText("Feedback", panel.rectTransform,
                new Vector2(1f, 0f), new Vector2(1f, 0f),
                new Vector2(-20f, 14f), new Vector2(400f, 40f), "", 24, TextAnchor.MiddleRight, Color.white,
                shadow: true);

            var calcHud = canvasGo.AddComponent<CalculusHUD>();
            calcHud.rootPanel = panel.gameObject;
            calcHud.questionText = question;
            calcHud.timerBar = timer;
            calcHud.timerBarFill = timerFill;
            calcHud.livesText = lives;
            calcHud.feedbackText = feedback;

            var calcManager = Object.FindFirstObjectByType<CalculusGameManager>();
            if (calcManager != null) calcManager.hud = calcHud;
        }
    }
}