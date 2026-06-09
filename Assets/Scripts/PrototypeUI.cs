using UnityEngine;

namespace SundaeDiver
{
    /// <summary>
    /// Prototype UI drawn with IMGUI (OnGUI) so the game is fully playable with no
    /// Canvas/EventSystem setup. The rotate buttons use GUI.RepeatButton for natural
    /// hold-to-spin. Replace this with a uGUI/TextMeshPro Canvas for production — the
    /// GameManager already exposes everything the UI reads.
    /// </summary>
    public class PrototypeUI : MonoBehaviour
    {
        public GameManager gm;
        [Tooltip("Show live State / Distance / FallSpeed / Banana Y / Item count for debugging.")]
        public bool showDebug = true;

        private GUIStyle _title, _label, _small, _btn, _scoop;

        private void BuildStyles(float s)
        {
            if (_title != null) return;
            _title = new GUIStyle(GUI.skin.label) { fontSize = (int)(46 * s), fontStyle = FontStyle.Bold, alignment = TextAnchor.MiddleCenter };
            _label = new GUIStyle(GUI.skin.label) { fontSize = (int)(24 * s), fontStyle = FontStyle.Bold };
            _small = new GUIStyle(GUI.skin.label) { fontSize = (int)(18 * s), alignment = TextAnchor.MiddleCenter };
            _btn   = new GUIStyle(GUI.skin.button) { fontSize = (int)(22 * s), fontStyle = FontStyle.Bold };
            _scoop = new GUIStyle(GUI.skin.button) { fontSize = (int)(26 * s), fontStyle = FontStyle.Bold };
        }

        private void OnGUI()
        {
            if (gm == null || gm.Input == null) return;
            float s = Screen.height / 800f;
            BuildStyles(s);

            // reset hold flags at the start of each GUI frame
            if (Event.current.type == EventType.Layout) { gm.Input.uiCW = false; gm.Input.uiCCW = false; }

            switch (gm.State)
            {
                case GameState.Menu:    DrawMenu(s); break;
                case GameState.Ready:   DrawHud(s); DrawPrompt(s); break;
                case GameState.Diving:  DrawHud(s); DrawRotButtons(s); break;
                case GameState.Landing: DrawHud(s); break;
                case GameState.Result:  DrawHud(s); DrawResults(s); break;
            }

            if (showDebug) DrawDebug(s);
        }

        private void DrawDebug(float s)
        {
            var style = new GUIStyle(GUI.skin.label)
            {
                fontSize = (int)(16 * s),
                alignment = TextAnchor.LowerLeft,
            };
            string txt =
                $"State: {gm.State}\n" +
                $"Distance: {gm.Distance:0.0} / {(gm.Current != null ? gm.Current.depth : 0f):0.0}\n" +
                $"FallSpeed: {gm.FallSpeed:0.0}   BananaY: {gm.BananaY:0.0}\n" +
                $"Items spawned: {gm.ItemCount}";
            GUI.color = new Color(0f, 0f, 0f, 0.35f);
            GUI.DrawTexture(new Rect(8 * s, Screen.height - 110 * s, 360 * s, 102 * s), Texture2D.whiteTexture);
            GUI.color = Color.white;
            GUI.Label(new Rect(16 * s, Screen.height - 108 * s, 360 * s, 96 * s), txt, style);
        }

        private void DrawHud(float s)
        {
            GUI.Label(new Rect(16 * s, 12 * s, 400 * s, 40 * s),
                      "Score  " + Mathf.RoundToInt(gm.Score.LiveScore), _label);

            // scoop rating (plain text so it renders in the default font)
            int live = gm.Score.LiveScoops();
            var pr = new Rect(Screen.width - 240 * s, 12 * s, 220 * s, 40 * s);
            var right = new GUIStyle(_label) { alignment = TextAnchor.MiddleRight };
            GUI.Label(pr, "Scoops  " + live + " / 3", right);

            // progress bar
            float h = Screen.height * 0.5f;
            var track = new Rect(Screen.width - 14 * s, Screen.height * 0.22f, 8 * s, h);
            GUI.color = new Color(0, 0, 0, 0.12f); GUI.DrawTexture(track, Texture2D.whiteTexture);
            GUI.color = new Color(0.95f, 0.55f, 0.2f);
            GUI.DrawTexture(new Rect(track.x, track.y, track.width, h * gm.Progress), Texture2D.whiteTexture);
            GUI.color = Color.white;
        }

        private void DrawPrompt(float s)
        {
            GUI.Label(new Rect(0, Screen.height * 0.6f, Screen.width, 50 * s),
                      "Swipe down to dive", _small);
        }

        private void DrawRotButtons(float s)
        {
            float bw = Screen.height * 0.13f;
            var rCCW = new Rect(Screen.width - bw * 2 - 30 * s, Screen.height - bw - 24 * s, bw, bw);
            var rCW  = new Rect(Screen.width - bw - 18 * s,     Screen.height - bw - 24 * s, bw, bw);
            if (GUI.RepeatButton(rCCW, "CCW", _scoop)) gm.Input.uiCCW = true;
            if (GUI.RepeatButton(rCW,  "CW",  _scoop)) gm.Input.uiCW  = true;
        }

        private void DrawMenu(float s)
        {
            GUI.Label(new Rect(0, Screen.height * 0.12f, Screen.width, 70 * s), "SUNDAE DIVER", _title);
            GUI.Label(new Rect(0, Screen.height * 0.22f, Screen.width, 40 * s),
                      "Dive, dodge, collect, and stick the landing.", _small);

            var levels = gm.levels;
            int n = levels != null ? levels.Count : 0;
            float bw = Screen.width * 0.5f, bh = Screen.height * 0.11f, gap = 12 * s;
            float y0 = Screen.height * 0.38f;
            for (int i = 0; i < n; i++)
            {
                var r = new Rect(Screen.width * 0.5f - bw * 0.5f, y0 + i * (bh + gap), bw, bh);
                string name = levels[i] != null ? levels[i].levelName : ("Level " + (i + 1));
                if (GUI.Button(r, name, _btn)) gm.SelectLevel(i);
            }
            if (n == 0)
                GUI.Label(new Rect(0, y0, Screen.width, bh), "No levels assigned on the GameManager.", _small);
        }

        private void DrawResults(float s)
        {
            var sc = gm.Score;
            // When the splat/sundae reveal is active (success), don't paint over the
            // built sundae — use a light top banner + bottom stat panel instead.
            bool showSundae = gm.presenter != null && !sc.Failed;

            if (!showSundae)
            {
                GUI.color = new Color(1f, 0.96f, 0.88f, 0.92f);
                GUI.DrawTexture(new Rect(0, 0, Screen.width, Screen.height), Texture2D.whiteTexture);
                GUI.color = Color.white;
            }
            else
            {
                // translucent top banner only
                GUI.color = new Color(1f, 0.96f, 0.88f, 0.65f);
                GUI.DrawTexture(new Rect(0, 0, Screen.width, Screen.height * 0.14f), Texture2D.whiteTexture);
                GUI.color = Color.white;
            }

            string[] titles = { "So close - try again!", "Tasty dive!", "Delicious! Two scoops!", "PERFECT SUNDAE!" };
            string title = sc.Failed ? "SPLAT! No banana made it" : titles[sc.Scoops];
            GUI.Label(new Rect(0, Screen.height * (showSundae ? 0.02f : 0.16f), Screen.width, 80 * s), title, _title);

            if (!showSundae)
                GUI.Label(new Rect(0, Screen.height * 0.30f, Screen.width, 50 * s),
                          sc.Failed ? "" : ("Scoops:  " + sc.Scoops + " / 3"), _title);

            string body = sc.Failed
                ? "Your banana was completely eaten\nbefore it reached the dish."
                : $"Scoops {sc.Scoops}/3     Toppings +{sc.ToppingScore}     Banana intact +{sc.IntactScore}\n" +
                  $"Landing ({Mathf.RoundToInt(sc.LandingAccuracy * 100f)}%) +{sc.LandingScore}     Total {sc.FinalScore}";

            if (showSundae)
            {
                GUI.color = new Color(1f, 0.96f, 0.88f, 0.85f);
                GUI.DrawTexture(new Rect(0, Screen.height * 0.62f, Screen.width, Screen.height * 0.38f), Texture2D.whiteTexture);
                GUI.color = Color.white;
                GUI.Label(new Rect(0, Screen.height * 0.63f, Screen.width, 90 * s), body, _small);
            }
            else
            {
                GUI.Label(new Rect(0, Screen.height * 0.42f, Screen.width, 200 * s), body, _small);
            }
            float bw = Screen.width * 0.32f, bh = Screen.height * 0.09f, y = Screen.height * 0.72f;
            if (GUI.Button(new Rect(Screen.width * 0.5f - bw - 10 * s, y, bw, bh), "Dive Again", _btn)) gm.Retry();
            if (GUI.Button(new Rect(Screen.width * 0.5f + 10 * s, y, bw, bh), "Menu", _btn)) gm.ToMenu();
        }
    }
}
