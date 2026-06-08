using System.Collections.Generic;
using UnityEngine;

namespace SundaeDiver
{
    /// <summary>
    /// Scoring math, mirroring the prototype. Live score = toppings collected minus
    /// hit penalties. Final score adds the intact-banana bonus and the landing bonus.
    /// Scoops (1-3) are awarded by comparing the total to a % of the level's max.
    /// </summary>
    public class ScoreSystem
    {
        private readonly GameConfig _config;

        public int LiveScore { get; private set; }     // toppings - penalties (during the dive)
        public int ToppingScore { get; private set; }
        public int MaxToppingScore { get; private set; }
        public int MaxScore { get; private set; }

        // breakdown of the last finalised run
        public int FinalScore { get; private set; }
        public int IntactScore { get; private set; }
        public int LandingScore { get; private set; }
        public float LandingAccuracy { get; private set; }
        public int Scoops { get; private set; }
        public bool Failed { get; private set; }

        public readonly Dictionary<ToppingType, int> Collected = new Dictionary<ToppingType, int>();

        public ScoreSystem(GameConfig config) { _config = config; }

        public void BeginLevel(List<LevelItem> items)
        {
            LiveScore = 0; ToppingScore = 0; FinalScore = 0;
            IntactScore = 0; LandingScore = 0; LandingAccuracy = 0f;
            Scoops = 0; Failed = false;
            Collected.Clear();

            MaxToppingScore = 0;
            foreach (var it in items)
                if (!it.IsObstacle) MaxToppingScore += Toppings.Value(it.Topping);
            MaxScore = MaxToppingScore + _config.intactMax + _config.landingMax;
        }

        public void Collect(ToppingType t)
        {
            int v = Toppings.Value(t);
            ToppingScore += v;
            LiveScore += v;
            Collected[t] = Collected.TryGetValue(t, out var n) ? n + 1 : 1;
        }

        public void Penalty()
        {
            LiveScore = Mathf.Max(0, LiveScore - _config.hitPenalty);
        }

        public int LiveScoops() => ScoopsFor(LiveScore);

        public void Finalize(int chunks, float landingAccuracy)
        {
            LandingAccuracy = landingAccuracy;
            float intact = (float)chunks / _config.maxChunks;
            IntactScore = Mathf.RoundToInt(_config.intactMax * intact);
            LandingScore = Mathf.RoundToInt(_config.landingMax * landingAccuracy);
            FinalScore = ToppingScore + IntactScore + LandingScore;
            Scoops = ScoopsFor(FinalScore);
            Failed = false;
        }

        public void Fail()
        {
            IntactScore = 0; LandingScore = 0; LandingAccuracy = 0f;
            FinalScore = 0; Scoops = 0; Failed = true;
        }

        private int ScoopsFor(int score)
        {
            float pct = MaxScore > 0 ? (float)score / MaxScore : 0f;
            int s = 0;
            foreach (var th in _config.scoopThresholds)
                if (pct >= th) s++;
            return s;
        }
    }
}
