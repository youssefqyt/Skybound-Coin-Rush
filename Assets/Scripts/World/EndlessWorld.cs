using System;
using System.Collections.Generic;
using UnityEngine;

namespace Skybound
{
    /// <summary>
    /// Procedural endless skyway: pools every gameplay entity, spawns curated fair
    /// patterns and resolves collision in lane space (independent of the 2D projection).
    /// </summary>
    public class EndlessWorld : MonoBehaviour
    {
        public event Action<Coin> CoinCollected;
        public event Action<PowerUpType, Vector3> PowerUpCollected;
        public event Action<Obstacle> NearMiss;
        public event Action<Obstacle> PlayerHitObstacle;
        public event Action<Obstacle> PassedObstacle;

        public SkyHunter Hunter { get; private set; }
        public int ActiveObstacles { get { return obstacles.Count; } }
        public int ActiveCoins { get { return coins.Count; } }
        public float Difficulty01 { get { return difficulty; } }

        /// <summary>
        /// Test hook: disables automatic pattern/power-up spawns so a scenario can be
        /// driven deterministically. Gameplay never toggles this.
        /// </summary>
        public bool SpawningEnabled { get; set; } = true;

        private const float SpawnZ = 155f;
        private const float DespawnZ = -14f;

        private readonly List<Obstacle> obstacles = new List<Obstacle>();
        private readonly List<Coin> coins = new List<Coin>();
        private readonly List<PowerUpPickup> pickups = new List<PowerUpPickup>();

        private SimplePool<Obstacle> obstaclePool;
        private SimplePool<Coin> coinPool;
        private SimplePool<PowerUpPickup> pickupPool;

        private Transform obstacleRoot;
        private Transform coinRoot;
        private Transform pickupRoot;

        private System.Random rng;
        private float spawnCounter;
        private float spawnInterval = 13f;
        private float powerTimer = 9f;
        private float difficulty = 0.15f;
        private float difficultyBias;
        private float movingDistance;
        private int patternCount;
        private int safeLane = 1;

        public static EndlessWorld Create(Transform parent)
        {
            GameObject go = RuntimeUtil.Create("EndlessWorld", parent);
            EndlessWorld world = go.AddComponent<EndlessWorld>();
            world.Build();
            return world;
        }

        private void Build()
        {
            obstacleRoot = RuntimeUtil.Create("Obstacles", transform).transform;
            coinRoot = RuntimeUtil.Create("Coins", transform).transform;
            pickupRoot = RuntimeUtil.Create("Pickups", transform).transform;

            obstaclePool = new SimplePool<Obstacle>(() => Obstacle.Create(obstacleRoot), obstacleRoot, 20);
            coinPool = new SimplePool<Coin>(() => Coin.Create(coinRoot), coinRoot, 84);
            pickupPool = new SimplePool<PowerUpPickup>(() => PowerUpPickup.Create(pickupRoot), pickupRoot, 4);

            Hunter = SkyHunter.Create(transform);
            rng = new System.Random(Environment.TickCount);
        }

        public void SetDifficultyBias(float bias)
        {
            difficultyBias = Mathf.Clamp01(bias);
            RecalculateDifficulty();
        }

        private void RecalculateDifficulty()
        {
            float distanceFactor = Mathf.Clamp01(movingDistance / 2600f);
            difficulty = Mathf.Clamp01(distanceFactor * 0.72f + difficultyBias * 0.5f);
            spawnInterval = Mathf.Lerp(14.5f, 7.4f, difficulty);
        }

        public void ResetRun()
        {
            ClearAll();
            rng = new System.Random(Environment.TickCount + UnityEngine.Random.Range(0, 99999));
            spawnCounter = 0f;
            powerTimer = 9f;
            movingDistance = 0f;
            patternCount = 0;
            safeLane = 1;
            difficulty = Mathf.Clamp01(difficultyBias * 0.5f + 0.06f);
            spawnInterval = Mathf.Lerp(14.5f, 7.4f, difficulty);
            if (Hunter != null) Hunter.Show();
        }

        public void EnterMenu()
        {
            ClearAll();
            if (Hunter != null) Hunter.Hide();
        }

        public void SetHunterThreat(float value)
        {
            if (Hunter != null) Hunter.SetThreat(value);
        }

        public void HunterLunge()
        {
            if (Hunter != null) Hunter.Lunge();
        }

        public void HideHunter()
        {
            if (Hunter != null) Hunter.Hide();
        }

        public void ClearAll()
        {
            for (int i = 0; i < obstacles.Count; i++) obstaclePool.Release(obstacles[i]);
            for (int i = 0; i < coins.Count; i++) coinPool.Release(coins[i]);
            for (int i = 0; i < pickups.Count; i++) pickupPool.Release(pickups[i]);
            obstacles.Clear();
            coins.Clear();
            pickups.Clear();
        }

        public void Tick(float deltaTime, float speed, bool playing, PlayerController player)
        {
            if (deltaTime <= 0f) return;

            if (Hunter != null)
                Hunter.Tick(deltaTime, player != null ? player.VisualX : 0f);

            float move = playing ? speed * deltaTime : 0f;
            if (playing)
            {
                movingDistance += move;
                RecalculateDifficulty();
                spawnCounter += move;
                powerTimer -= deltaTime;

                if (SpawningEnabled && spawnCounter >= spawnInterval)
                {
                    spawnCounter -= spawnInterval;
                    SpawnPattern();
                }
                if (SpawningEnabled && powerTimer <= 0f)
                {
                    powerTimer = UnityEngine.Random.Range(22f, 34f);
                    SpawnPowerUp();
                }
            }

            UpdateObstacles(deltaTime, move, playing, player);
            UpdateCoins(deltaTime, move, playing, player);
            UpdatePickups(deltaTime, move, playing, player);
        }

        // ------------------------------------------------------------- UPDATES

        private void UpdateObstacles(float dt, float move, bool playing, PlayerController player)
        {
            float speed = dt > 0.0001f ? move / dt : 0f;
            for (int i = obstacles.Count - 1; i >= 0; i--)
            {
                Obstacle obstacle = obstacles[i];
                obstacle.Tick(dt, speed, playing);
                ResolveObstacle(obstacle, player, playing);
                if (obstacle.Z < DespawnZ)
                {
                    obstaclePool.Release(obstacle);
                    obstacles.RemoveAt(i);
                }
            }
        }

        private void ResolveObstacle(Obstacle obstacle, PlayerController player, bool playing)
        {
            if (!playing || player == null) return;

            if (!obstacle.Passed && obstacle.Z <= obstacle.Depth && obstacle.Z >= -obstacle.Depth)
            {
                float laneDistance = Mathf.Abs(obstacle.WorldX - player.VisualX);
                // The committed lane (updated the instant a swipe is registered) is
                // authoritative. VisualX trails the swipe by the smooth lane-slide,
                // and GameManager ticks the world one frame before PlayerController
                // moves the player, so a purely positional test used to damage the
                // player while they were already sliding into a different lane.
                bool sameLane = obstacle.Lane == player.Lane && laneDistance < 1.15f;
                if (sameLane)
                {
                    bool hit = WouldHit(obstacle, player);
                    if (hit)
                    {
                        obstacle.Flash();
                        obstacle.MarkPassed(false);
                        if (PlayerHitObstacle != null) PlayerHitObstacle(obstacle);
                        player.TakeHit();
                    }
                    else if (obstacle.Kind != ObstacleKind.Gap)
                    {
                        obstacle.MarkPassed(false);
                    }
                }
            }

            if (!obstacle.Passed && obstacle.Z < -0.45f)
            {
                float laneDistance = Mathf.Abs(obstacle.WorldX - player.VisualX);
                bool near = laneDistance >= 1.45f && laneDistance <= 3.65f;
                obstacle.MarkPassed(near);
                if (near && (obstacle.Requirement == DodgeRequirement.AvoidLane ||
                             obstacle.Requirement == DodgeRequirement.HighJump))
                {
                    if (NearMiss != null) NearMiss(obstacle);
                }
                if (PassedObstacle != null) PassedObstacle(obstacle);
            }
        }

        private static bool WouldHit(Obstacle obstacle, PlayerController player)
        {
            float jump = player.JumpHeight;
            switch (obstacle.Requirement)
            {
                case DodgeRequirement.Jump:
                    return jump < obstacle.Clearance;
                case DodgeRequirement.HighJump:
                    return jump < obstacle.Clearance;
                case DodgeRequirement.Slide:
                    return !player.IsSliding || jump > 0.45f;
                default:
                    return jump < obstacle.Clearance;
            }
        }

        private void UpdateCoins(float dt, float move, bool playing, PlayerController player)
        {
            float magnet = 0f;
            PowerUpManager powers = GameManager.Instance != null ? GameManager.Instance.PowerUps : null;
            if (powers != null) magnet = powers.MagnetRadius;

            for (int i = coins.Count - 1; i >= 0; i--)
            {
                Coin coin = coins[i];
                if (!coin.gameObject.activeSelf)
                {
                    coins.RemoveAt(i);
                    coinPool.Release(coin);
                    continue;
                }

                if (playing) coin.Advance(move);
                coin.Tick(dt, 0f, false);

                if (playing && !coin.Collected && magnet > 0f && coin.Z < 30f && coin.Z > -2f)
                {
                    float targetHeight = (player != null ? player.JumpHeight : 0f) + 0.9f;
                    coin.MagnetPull(player != null ? player.VisualX : 0f, targetHeight, 5.5f, dt);
                }

                if (playing && !coin.Collected && player != null)
                {
                    float zDist = Mathf.Abs(coin.Z);
                    float xDist = Mathf.Abs(coin.WorldX - player.VisualX);
                    float yDist = Mathf.Abs(coin.Height - (0.9f + player.JumpHeight));
                    if (zDist < 1.1f && xDist < 1.15f && yDist < 1.15f)
                    {
                        coin.Collect();
                        if (CoinCollected != null) CoinCollected(coin);
                    }
                }

                if (coin.Z < -6f)
                {
                    coins.RemoveAt(i);
                    coinPool.Release(coin);
                }
            }
        }

        private void UpdatePickups(float dt, float move, bool playing, PlayerController player)
        {
            for (int i = pickups.Count - 1; i >= 0; i--)
            {
                PowerUpPickup pickup = pickups[i];
                if (!pickup.gameObject.activeSelf)
                {
                    pickups.RemoveAt(i);
                    pickupPool.Release(pickup);
                    continue;
                }

                if (playing) pickup.Advance(move);
                pickup.Tick(dt, 0f, false);

                if (playing && !pickup.Collected && player != null)
                {
                    float zDist = Mathf.Abs(pickup.Z);
                    float xDist = Mathf.Abs(pickup.WorldX - player.VisualX);
                    float yDist = Mathf.Abs(pickup.Height - (0.9f + player.JumpHeight));
                    if (zDist < 1.25f && xDist < 1.3f && yDist < 1.7f)
                    {
                        pickup.Collect();
                        if (PowerUpCollected != null)
                            PowerUpCollected(pickup.Type, pickup.transform.position);
                    }
                }

                if (pickup.Z < -6f)
                {
                    pickups.RemoveAt(i);
                    pickupPool.Release(pickup);
                }
            }
        }

        // ------------------------------------------------------------- SPAWNING

        private void SpawnPattern()
        {
            patternCount++;
            int roll = rng.Next(0, 100);
            if (patternCount <= 2)
            {
                CoinLine();
                return;
            }

            if (roll < 22) SingleObstacle();
            else if (roll < 36) DoubleObstacle();
            else if (roll < 50) JumpRow();
            else if (roll < 62) SlideRow();
            else if (roll < 73) GapRow();
            else if (roll < 84) CoinLine();
            else if (roll < 93) VehicleCluster();
            else Slalom();
        }

        private ObstacleKind RandomAvoidKind()
        {
            int roll = rng.Next(0, 100);
            if (difficulty > 0.55f && roll < 38) return ObstacleKind.Train;
            if (roll < 60) return ObstacleKind.Block;
            if (roll < 82) return ObstacleKind.Car;
            return ObstacleKind.Crate;
        }

        private void SingleObstacle()
        {
            int lane = rng.Next(0, GameConfig.LaneCount);
            ObstacleKind kind = PickKind();
            SpawnObstacle(kind, lane, SpawnZ);
            if (kind == ObstacleKind.JumpBarrier)
                SpawnCoinLine(lane, SpawnZ - 6f, rng.Next(3, 6), 1.7f, 1.6f, true);
            else if (rng.Next(0, 100) < 62)
                SpawnCoinLine(SafeLaneExcept(lane), SpawnZ + 4f, rng.Next(4, 8), 1.9f, 0.9f, false);
        }

        private ObstacleKind PickKind()
        {
            int roll = rng.Next(0, 100);
            if (roll < 26) return ObstacleKind.JumpBarrier;
            if (roll < 48) return ObstacleKind.SlideGate;
            if (roll < 64) return ObstacleKind.Block;
            if (roll < 80) return ObstacleKind.Car;
            if (difficulty < 0.25f) return ObstacleKind.Crate;
            if (roll < 88) return ObstacleKind.Crate;
            if (roll < 95) return ObstacleKind.Drone;
            return ObstacleKind.Laser;
        }

        private void DoubleObstacle()
        {
            int openLane = rng.Next(0, GameConfig.LaneCount);
            ObstacleKind a = RandomAvoidKind();
            ObstacleKind b = RandomAvoidKind();
            for (int lane = 0; lane < GameConfig.LaneCount; lane++)
            {
                if (lane == openLane) continue;
                SpawnObstacle(lane == (openLane + 1) % 3 ? a : b, lane, SpawnZ);
            }
            SpawnCoinLine(openLane, SpawnZ + 2f, rng.Next(4, 9), 1.9f, 0.9f, false);
        }

        private void JumpRow()
        {
            int lanes = difficulty > 0.35f ? GameConfig.LaneCount : (1 + rng.Next(0, 2));
            for (int i = 0; i < lanes; i++)
            {
                int lane = (safeLane + i) % GameConfig.LaneCount;
                SpawnObstacle(ObstacleKind.JumpBarrier, lane, SpawnZ);
            }
            int coinLane = rng.Next(0, GameConfig.LaneCount);
            SpawnCoinLine(coinLane, SpawnZ - 7f, rng.Next(4, 7), 1.7f, 1.5f, true);
            safeLane = (safeLane + 1) % GameConfig.LaneCount;
        }

        private void SlideRow()
        {
            int lanes = difficulty > 0.4f ? GameConfig.LaneCount : (1 + rng.Next(0, 2));
            for (int i = 0; i < lanes; i++)
            {
                int lane = (safeLane + i) % GameConfig.LaneCount;
                SpawnObstacle(rng.Next(0, 100) < 25 ? ObstacleKind.Laser : ObstacleKind.SlideGate,
                    lane, SpawnZ);
            }
            SpawnCoinLine(rng.Next(0, GameConfig.LaneCount), SpawnZ - 6.5f, rng.Next(4, 8), 1.7f, 0.55f, false);
            safeLane = (safeLane + 2) % GameConfig.LaneCount;
        }

        private void GapRow()
        {
            // Number of lanes that become holes; at least one lane always stays open.
            int gaps = difficulty > 0.55f ? 2 : 1;
            int open = rng.Next(0, GameConfig.LaneCount);
            int spawned = 0;
            for (int lane = 0; lane < GameConfig.LaneCount && spawned < gaps; lane++)
            {
                if (lane == open) continue;
                SpawnObstacle(ObstacleKind.Gap, lane, SpawnZ);
                spawned++;
            }
            SpawnCoinLine(open, SpawnZ - 4f, rng.Next(4, 8), 1.8f, 0.9f, false);
        }

        private void CoinLine()
        {
            int lane = rng.Next(0, GameConfig.LaneCount);
            SpawnCoinLine(lane, SpawnZ, rng.Next(6, 11), 1.8f, 0.9f, false);
            if (difficulty > 0.3f && rng.Next(0, 100) < 45)
            {
                SpawnObstacle(PickKind(), SafeLaneExcept(lane), SpawnZ + 3f);
            }
        }

        private void VehicleCluster()
        {
            int trainLane = rng.Next(0, GameConfig.LaneCount);
            SpawnObstacle(ObstacleKind.Train, trainLane, SpawnZ);
            int otherLane = SafeLaneExcept(trainLane);
            if (difficulty > 0.45f)
                SpawnObstacle(rng.Next(0, 100) < 50 ? ObstacleKind.Car : ObstacleKind.Crate,
                    otherLane, SpawnZ + 9f);
            int openLane = 3 - trainLane - otherLane;
            if (openLane < 0 || openLane > 2) openLane = SafeLaneExcept(trainLane);
            SpawnCoinLine(openLane, SpawnZ + 1f, rng.Next(5, 9), 1.8f, 0.9f, false);
        }

        private void Slalom()
        {
            int laneA = rng.Next(0, GameConfig.LaneCount);
            int laneB = rng.Next(0, GameConfig.LaneCount);
            if (laneB == laneA) laneB = (laneA + 1) % GameConfig.LaneCount;
            ObstacleKind kind = PickKind();
            SpawnObstacle(kind, laneA, SpawnZ);
            SpawnObstacle(PickKind(), laneB, SpawnZ + 11f);
            SpawnCoinLine(SafeLaneExcept(laneA, laneB), SpawnZ + 5f, 5, 1.9f, 0.9f, false);
        }

        private int SafeLaneExcept(params int[] lanes)
        {
            for (int attempt = 0; attempt < 8; attempt++)
            {
                int candidate = rng.Next(0, GameConfig.LaneCount);
                bool blocked = false;
                for (int i = 0; i < lanes.Length; i++)
                    if (lanes[i] == candidate) blocked = true;
                if (!blocked) return candidate;
            }
            return 1;
        }

        private void SpawnObstacle(ObstacleKind kind, int lane, float z)
        {
            Obstacle obstacle = obstaclePool.Get();
            obstacle.Setup(kind, lane, z, difficulty);
            obstacles.Add(obstacle);
        }

        private void SpawnCoin(CoinType type, int lane, float z, float height)
        {
            Coin coin = coinPool.Get();
            coin.Setup(type, lane, z, height);
            coins.Add(coin);
        }

        private void SpawnCoinLine(int lane, float zStart, int count, float spacing, float height, bool arc)
        {
            for (int i = 0; i < count; i++)
            {
                float t = count <= 1 ? 0f : i / (float)(count - 1);
                float h = height;
                if (arc) h += Mathf.Sin(t * Mathf.PI) * 1.35f;
                CoinType type = RollCoinType();
                SpawnCoin(type, lane, zStart + i * spacing, h);
            }
        }

        private CoinType RollCoinType()
        {
            int roll = rng.Next(0, 1000);
            if (roll < 22) return CoinType.Special;
            if (roll < 130) return CoinType.Gold;
            if (roll < 430) return CoinType.Silver;
            return CoinType.Bronze;
        }

        private void SpawnPowerUp()
        {
            int lane = rng.Next(0, GameConfig.LaneCount);
            PowerUpType type = (PowerUpType)rng.Next(0, (int)PowerUpType.Count);
            PowerUpPickup pickup = pickupPool.Get();
            pickup.Setup(type, lane, SpawnZ + 6f);
            pickups.Add(pickup);
        }

        /// <summary>Spawns a tutorial row directly ahead (used by the guided tutorial).</summary>
        public void SpawnTutorialRow(ObstacleKind kind, int lane)
        {
            SpawnObstacle(kind, lane, 34f);
        }

        /// <summary>Test hook: spawns one hazard at an exact depth and returns it.</summary>
        public Obstacle SpawnTestObstacle(ObstacleKind kind, int lane, float z)
        {
            SpawnObstacle(kind, lane, z);
            return obstacles[obstacles.Count - 1];
        }

        public void SpawnTutorialCoins(int lane, int count)
        {
            SpawnCoinLine(lane, 30f, count, 2.2f, 0.9f, false);
        }
    }
}
