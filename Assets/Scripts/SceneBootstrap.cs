using System.Collections.Generic;
using UnityEngine;

namespace SundaeDiver
{
    /// <summary>
    /// One-stop entry point. Add an empty GameObject to a scene, attach this, press
    /// Play. It builds the camera, banana, board, generator, GameManager and UI, and
    /// wires everything — no manual scene setup required.
    ///
    /// Optional: assign a GameConfig asset and LevelData assets in the inspector to
    /// override the runtime defaults.
    /// </summary>
    public class SceneBootstrap : MonoBehaviour
    {
        [Tooltip("Optional. Leave empty to use built-in defaults.")]
        public GameConfig config;
        [Tooltip("Optional. Leave empty to use the two built-in demo levels.")]
        public List<LevelData> levels = new List<LevelData>();

        private void Awake()
        {
            Application.targetFrameRate = 60;
            Screen.orientation = ScreenOrientation.Portrait;

            if (config == null) config = GameConfig.CreateDefault();
            if (levels == null || levels.Count == 0) levels = LevelData.CreateDefaults();

            // --- Camera ---
            var cam = Camera.main;
            if (cam == null)
            {
                var camGo = new GameObject("Main Camera");
                camGo.tag = "MainCamera";
                cam = camGo.AddComponent<Camera>();
                camGo.AddComponent<AudioListener>();
            }
            cam.orthographic = true;
            cam.orthographicSize = config.orthoSize;
            cam.backgroundColor = new Color(1f, 0.96f, 0.89f); // cream kitchen wall
            cam.clearFlags = CameraClearFlags.SolidColor;
            cam.transform.position = new Vector3(0f, 0f, -10f);

            // --- Banana (player) ---
            var bananaSprite = SpriteFactory.Ellipse(170, 92,
                new Color(1f, 0.82f, 0.25f), new Color(0.90f, 0.66f, 0.11f));
            var bananaGo = SpriteFactory.NewSpriteObject("Banana", bananaSprite, 10);
            var banana = bananaGo.AddComponent<BananaController>();
            banana.config = config;

            // --- Diving board (static, at y = 0) ---
            var boardSprite = SpriteFactory.RoundRect(380, 44, 18, new Color(0.62f, 0.78f, 0.95f));
            var board = SpriteFactory.NewSpriteObject("DivingBoard", boardSprite, 2);
            board.transform.position = new Vector3(0f, 0.55f, 0f);

            // --- Camera follow ---
            var rig = cam.gameObject.AddComponent<CameraRig>();
            rig.config = config;
            rig.target = bananaGo.transform;

            // --- Items root ---
            var itemsRoot = new GameObject("Items").transform;

            // --- Game manager (+ prefab library & sundae presenter for the placeholder demo) ---
            var gmGo = new GameObject("GameManager");
            var prefabLib = gmGo.AddComponent<PrefabLibrary>();   // empty -> generator uses placeholders
            var presenter = gmGo.AddComponent<SundaePresenter>();
            presenter.garnishLibrary = prefabLib;
            var gm = gmGo.AddComponent<GameManager>();
            gm.config = config;
            gm.levels = levels;
            gm.banana = banana;
            gm.itemsRoot = itemsRoot;
            gm.prefabs = prefabLib;
            gm.presenter = presenter;
            gm.Init();

            // --- UI ---
            var uiGo = new GameObject("PrototypeUI");
            var ui = uiGo.AddComponent<PrototypeUI>();
            ui.gm = gm;
        }
    }
}
