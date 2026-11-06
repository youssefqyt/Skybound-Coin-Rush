using UnityEngine;

namespace Skybound
{
    /// <summary>Immutable snapshot of one finished run, shared by missions, achievements and saving.</summary>
    public class RunSummary
    {
        public int Score;
        public float Distance;
        public int Coins;
        public int CoinCount;
        public int GoldCoins;
        public int SilverCoins;
        public int BronzeCoins;
        public int SpecialCoins;
        public int BestCombo;
        public float Time;
        public bool Perfect;
        public int Jumps;
        public int Slides;
        public int NearMisses;
        public int PowerUps;
        public float TopSpeed;
        public int LevelIndex = -1;
        public bool LevelCompleted;
        public int Stars;
        public bool Revived;
        public bool NewBestScore;
        public bool NewBestDistance;

        public bool IsEndless { get { return LevelIndex < 0; } }
    }
}
