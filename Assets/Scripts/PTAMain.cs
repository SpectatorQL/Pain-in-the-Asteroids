using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEngine;

namespace PTA
{
    public class EnumNamedArrayAttribute : PropertyAttribute
    {
        public string[] Names;
        
        public EnumNamedArrayAttribute(Type enumType)
        {
            Names = enumType.GetEnumNames();
        }
    }
    
    public delegate void Move(PTAMain world, PTAEntity entity);
    public static class MoveFunctions
    {
        public static void MoveStub(PTAMain world, PTAEntity entity)
        {
            entity.Rigidbody.velocity = Vector2.zero;
        }
        
        public static void SineMove(PTAMain world, PTAEntity entity)
        {
            if(!entity.HasSpawned)
                return;

            entity.Rigidbody.velocity = Vector2.zero;

            Vector2 entityPosition = entity.Transform.position;

            Vector2 newPosition = entity.Transform.position;
            
            newPosition.x += entity.Data.MovementSpeed;
            float sineValue = Mathf.Sin(entity.Data.TSine);
            float sineRange = 0.10f;
            sineValue = Mathf.Clamp(sineValue, -sineRange, sineRange);
            newPosition.y += sineValue;

            entity.Rigidbody.MovePosition(newPosition);

            float rotationAngle = Mathf.Atan2(newPosition.y - entityPosition.y, newPosition.x - entityPosition.x) * Mathf.Rad2Deg;
            Vector3 eulers = entity.Transform.eulerAngles;
            eulers.z = rotationAngle;
            entity.Transform.eulerAngles = eulers;

            if(entity.Data.TSine > (2.0f * Mathf.PI))
            {
                entity.Data.TSine -= (2.0f * Mathf.PI);
            }
            entity.Data.TSine += 0.15f;
        }
        
        public static void LinearMove(PTAMain world, PTAEntity entity)
        {
            if(!entity.HasSpawned)
                return;

            entity.Rigidbody.velocity = Vector2.zero;

            Vector2 entityPosition = entity.Transform.position;

            Vector2 newPosition = entityPosition + entity.Data.MoveDirection.normalized * entity.Data.MovementSpeed;
            entity.Rigidbody.MovePosition(newPosition);

            float rotationAngle = Mathf.Atan2(newPosition.y - entityPosition.y, newPosition.x - entityPosition.x) * Mathf.Rad2Deg;
            Vector3 eulers = entity.Transform.eulerAngles;
            eulers.z = rotationAngle;
            entity.Transform.eulerAngles = eulers;
        }

        public static void HomingMove(PTAMain world, PTAEntity entity)
        {
            entity.Rigidbody.velocity = Vector2.zero;

            Vector2 entityPosition = entity.Transform.position;

            Vector2 direction = (Vector2)world.PlayerEntity.Transform.position - entityPosition;
            Vector2 newPosition = entityPosition + direction.normalized * entity.Data.MovementSpeed;
            entity.Rigidbody.MovePosition(newPosition);

            float rotationAngle = Mathf.Atan2(newPosition.y - entityPosition.y, newPosition.x - entityPosition.x) * Mathf.Rad2Deg;
            Vector3 eulers = entity.Transform.eulerAngles;
            eulers.z = rotationAngle;
            entity.Transform.eulerAngles = eulers;
        }
    }
    
    public delegate void Think(PTAMain world, PTAEntity entity, float dt);
    public static class ThinkFunctions
    {
        public static void ThinkStub(PTAMain world, PTAEntity entity, float dt)
        {
        }
        
        public static void HostileThink(PTAMain world, PTAEntity entity, float dt)
        {
            float timeToSpawn = entity.Data.TimeToSpawn;
            Color fadingEntityColor = entity.Renderer.material.color;
            if(timeToSpawn > 0)
            {
                if(fadingEntityColor.a < 0)
                {
                    fadingEntityColor.a = 1.0f;
                }
                fadingEntityColor.a -= dt;
                entity.Renderer.material.color = fadingEntityColor;
                timeToSpawn -= dt;
                entity.Data.TimeToSpawn = timeToSpawn;

                if(timeToSpawn > 0)
                    return;
            }
            else
            {
                fadingEntityColor.a = 1.0f;
                entity.Renderer.material.color = fadingEntityColor;

                entity.Collider.BoxCollider.enabled = true;
                entity.HasSpawned = true;
            }
        }
        
        public static void PlayerThink(PTAMain world, PTAEntity entity, float dt)
        {
            PTAInput input = world.Input;

            // TODO(SpectatorQL): As of right now, the player moves faster than all of the other entities,
            // because this runs in Update(). Do we want to keep it this way?
            Vector3 moveVector = new Vector3(input.LX, input.LY);
            moveVector.Normalize();

            float speed = entity.Data.MovementSpeed;
            Vector2 oldPosition = entity.Transform.position;
            Vector2 newPosition = entity.Transform.position + moveVector * speed;
            entity.Transform.position = newPosition;

            float angle = 0.0f;
            // TODO(SpectatorQL): Fix auto-snapping to mouse position !!!
            if((input.RX != 0.0f) || (input.RY != 0.0f))
            {
                angle = Mathf.Rad2Deg * Mathf.Atan2(input.RY, input.RX);
            }
            else
            {
                Vector3 mousePosition = Input.mousePosition;
                mousePosition.z = 10.0f;
                Vector3 worldMousePosition = Camera.main.ScreenToWorldPoint(mousePosition);
                angle = Mathf.Rad2Deg * Mathf.Atan2(worldMousePosition.y - newPosition.y, worldMousePosition.x - newPosition.x);
            }
            
            Quaternion newRotation = entity.Transform.rotation;
            newRotation.eulerAngles = new Vector3(0.0f, 0.0f, angle);
            entity.Transform.rotation = newRotation;
            
            float runningAttackSpeed = entity.Data.RunningAttackSpeed;
            if(input.Fire)
            {
                if(runningAttackSpeed <= 0)
                {
                    Vector2 fireDirection = (newPosition - oldPosition).normalized;
                    PTAEntity newBullet = PTAEntity.CreateFriendlyBullet(world);
                    if(newBullet != null)
                    {
                        newBullet.Transform.position = oldPosition;
                        newBullet.Data.MoveDirection = fireDirection;
                    }
                    
                    runningAttackSpeed = entity.Data.AttackSpeed;
                }
            }
            
            if(runningAttackSpeed > 0)
            {
                runningAttackSpeed -= dt;
            }
            entity.Data.RunningAttackSpeed = runningAttackSpeed;
        }
    }
    
    public class PTAFreeEntities
    {
        PTAEntity[] Entities = new PTAEntity[PTAMain.MAX_ENTITIES];
        int Top = -1;
        
        public void Add(PTAEntity entity)
        {
            if(entity == null)
                return;
            if(!entity.IsActive)
                return;
            
            entity.GameObject.SetActive(false);
            entity.IsActive = false;
            ++Top;
            Entities[Top] = entity;
        }
        
        public PTAEntity GetNext()
        {
            PTAEntity result = null;
            
            if(Top != -1)
            {
                result = Entities[Top];
                result.GameObject.SetActive(true);
                result.IsActive = true;

                Entities[Top] = null;
                --Top;
            }
            
            return result;
        }
    }
    
    [Serializable]
    public class PTAEnemyProbability
    {
        public float[] Values;
        public const float PROBABILITY_TOTAL = 1.0f;
#if UNITY_EDITOR
        public int WaveCount;
        public int EnemyTypeCount;
#endif
    }
    
    public class PTAWaveData
    {
        public int EnemyCount;
        public int EnemiesOnScreen;
        public int MaxSpawnedEnemiesOnScreen = 8;

        public int PowerupCount;
        public float MaxPowerupsOnScreen = 10;
        public float PowerupWaitTime = 5.0f;
        public float MinPowerupWaitTime = 2.5f;
        public float RunningPowerupTime;
        public float PowerupStayChance;

        public int CurrentWave;
        public int MaxWave = 9;

        public int WildPowerupCount;
        public float WildPowerupSpawnChance;
    }

    [Serializable]
    public struct PTACheatCodes
    {
        public bool Invincibility;
        public bool RapidFire;
        public float RapidFireSpeed;
    }

    public class PTAMain : MonoBehaviour
    {
        public PTAPlatform Platform;
        public PTAInput Input;

        public const int MAX_ENTITIES = 4096;
        public PTAEntity[] Entities;
        public int EntityCount;
        // TODO(SpectatorQL): Callers should not be allowed to use this directly due to the fact that it doesn't re-initialize entities!
        public PTAFreeEntities FreeEntities = new PTAFreeEntities();
        public PTAEntity PlayerEntity;

        public PTAWaveData WaveData = new PTAWaveData();
        public PTAEnemyProbability EnemyProbability;
        
        public GameObject EntityPrefab;
        public GameObject WallPrefab;
        
        public PTAAlignment EntityAlignment;

        public PTACheatCodes Cheats;

        Rect PlayArea;
        int CurrentScreenWidth;
        int CurrentScreenHeight;

        public PTAWall[] Walls;

        PTAUI UI;
        
        [EnumNamedArray(typeof(EntityType))]
        public Sprite[] Sprites = new Sprite[(int)EntityType.Count];
        [EnumNamedArray(typeof(PowerupType))]
        public Sprite[] PowerupSprites = new Sprite[(int)PowerupType.Count];

        static Vector2 GenerateEntityPosition(Rect playArea)
        {
            Vector2 result = new Vector2();

            float safetyNet = 1.0f;
            result.x = UnityEngine.Random.Range(playArea.min.x + safetyNet, playArea.max.x - safetyNet);
            result.y = UnityEngine.Random.Range(playArea.min.y + safetyNet, playArea.max.y - safetyNet);

            return result;
        }

        static Rect CalculatePlayArea(int screenWidth, int screenHeight)
        {
            Rect result;

            Vector2 bottomLeft = Camera.main.ScreenToWorldPoint(new Vector3(0, 0, 10));
            Vector2 topRight = Camera.main.ScreenToWorldPoint(new Vector3(screenWidth, screenHeight, 10));
            float offscreenOffset = 1.0f;
            float minX = bottomLeft.x - offscreenOffset;
            float minY = bottomLeft.y - offscreenOffset;
            float maxX = topRight.x + offscreenOffset;
            float maxY = topRight.y + offscreenOffset;

            result = Rect.MinMaxRect(minX, minY, maxX, maxY);

            return result;
        }

        void Start()
        {
            PTAEntity.PlayerLayer = LayerMask.NameToLayer("Player");
            PTAEntity.ThingsLayer = LayerMask.NameToLayer("Things");
            if(PTAEntity.PlayerLayer == -1
               || PTAEntity.ThingsLayer == -1)
            {
                Debug.LogError("Layers are missing! Check the layers settings in Edit/Project Settings/Tags and Layers !");
            }

            CurrentScreenWidth = Screen.width;
            CurrentScreenHeight = Screen.height;
            PlayArea = CalculatePlayArea(CurrentScreenWidth, CurrentScreenHeight);

            Vector2 xWallSize = new Vector2(1.0f, PlayArea.max.y - PlayArea.min.y);
            Vector2 yWallSize = new Vector2(PlayArea.max.x - PlayArea.min.x, 1.0f);
            Walls = new PTAWall[4]
            {
                PTAWall.CreateWall(this, WallPrefab, new Vector2(PlayArea.min.x, PlayArea.center.y), xWallSize, WallType.X),
                PTAWall.CreateWall(this, WallPrefab, new Vector2(PlayArea.max.x, PlayArea.center.y), xWallSize, WallType.X),
                PTAWall.CreateWall(this, WallPrefab, new Vector2(PlayArea.center.x, PlayArea.min.y), yWallSize, WallType.Y),
                PTAWall.CreateWall(this, WallPrefab, new Vector2(PlayArea.center.x, PlayArea.max.y), yWallSize, WallType.Y),
            };

            Entities = new PTAEntity[MAX_ENTITIES];
            for(int i = 0;
                i < MAX_ENTITIES;
                ++i)
            {
                Entities[i] = new PTAEntity();
            }
            
            PlayerEntity = PTAEntity.CreatePlayer(this);
            
#if UNITY_EDITOR
            PTAEntity turretL = PTAEntity.CreateTurretPowerup(this);
            turretL.Transform.position = GenerateEntityPosition(PlayArea);
            ++WaveData.PowerupCount;
            
            PTAEntity turretR = PTAEntity.CreateTurretPowerup(this);
            turretR.Transform.position = GenerateEntityPosition(PlayArea);
            ++WaveData.PowerupCount;

            PTAEntity freeTurret = PTAEntity.CreateTurretPowerup(this);
            freeTurret.Transform.position = GenerateEntityPosition(PlayArea);
            ++WaveData.PowerupCount;

            PTAEntity drive = PTAEntity.CreateDrivePowerup(this);
            drive.Transform.position = GenerateEntityPosition(PlayArea);
            ++WaveData.PowerupCount;
#else
            Cheats.Invincibility = false;
            Cheats.RapidFire = false;
#endif

            UI = FindObjectOfType<PTAUI>();
        }
        
        // TODO(SpectatorQL): Is it necessary to split Move and Think? 
        void FixedUpdate()
        {
            for(int i = 0;
                i < MAX_ENTITIES;
                ++i)
            {
                PTAEntity entity = Entities[i];
                if(entity.IsActive)
                {
                    entity.Move(this, entity);
                }
            }
        }

        public IEnumerator TemporaryInvincibility()
        {
            Cheats.Invincibility = true;
            yield return new WaitForSeconds(2.0f);

            Cheats.Invincibility = false;
            yield return null;
        }

        void Update()
        {
            float dt = Time.deltaTime;

            Platform.GetInput(Input);

            int screenWidth = Screen.width;
            int screenHeight = Screen.height;
            if(screenWidth != CurrentScreenWidth || screenHeight != CurrentScreenHeight)
            {
                CurrentScreenWidth = screenWidth;
                CurrentScreenHeight = screenHeight;
                Rect newPlayArea = CalculatePlayArea(CurrentScreenWidth, CurrentScreenHeight);

                float xFactor = newPlayArea.width / PlayArea.width;
                float yFactor = newPlayArea.height / PlayArea.height;

                for(int entityIndex = 0;
                    entityIndex < EntityCount;
                    ++entityIndex)
                {
                    PTAEntity entity = Entities[entityIndex];

                    if(entity.ParentSlot == null)
                    {
                        Vector3 oldP = entity.Transform.position;
                        Vector3 newP = new Vector3();
                        newP.x = oldP.x * xFactor;
                        newP.y = oldP.y * yFactor;

                        entity.Transform.position = newP;
                    }
                }

                for(int wallIndex = 0;
                    wallIndex < Walls.Length;
                    ++wallIndex)
                {
                    PTAWall wall = Walls[wallIndex];

                    Vector3 oldP = wall.transform.position;
                    Vector3 newP = new Vector3();
                    newP.x = oldP.x * xFactor;
                    newP.y = oldP.y * yFactor;

                    wall.transform.position = newP;

                    Vector2 oldSize = wall.BoxCollider.size;
                    Vector2 newSize = new Vector2();
                    newSize.x = oldSize.x * xFactor;
                    newSize.y = oldSize.y * yFactor;

                    wall.BoxCollider.size = newSize;
                }

                PlayArea = newPlayArea;
            }


            Debug.Assert(WaveData.EnemyCount >= 0);
            Debug.Assert(WaveData.EnemiesOnScreen >= 0);
            if(WaveData.EnemyCount > 0)
            {
                Debug.Assert(WaveData.EnemiesOnScreen <= WaveData.EnemyCount
                    && WaveData.EnemiesOnScreen >= 0);
                while(WaveData.EnemiesOnScreen < WaveData.MaxSpawnedEnemiesOnScreen
                    && WaveData.EnemiesOnScreen < WaveData.EnemyCount)
                {
                    PTAEntity enemyEntity = PTAEntity.CreateEnemy(this);
                    if(enemyEntity != null)
                    {
                        enemyEntity.HasSpawned = false;

                        enemyEntity.Move = MoveFunctions.SineMove;
                        enemyEntity.Data.MoveDirection = UnityEngine.Random.insideUnitCircle;
                        enemyEntity.Think = ThinkFunctions.HostileThink;

                        enemyEntity.Transform.position = GenerateEntityPosition(PlayArea);

                        enemyEntity.Collider.BoxCollider.enabled = false;
                    }

                    ++WaveData.EnemiesOnScreen;
                    Debug.Assert(WaveData.EnemiesOnScreen <= WaveData.EnemyCount);
                    Debug.Assert(WaveData.EnemiesOnScreen <= WaveData.MaxSpawnedEnemiesOnScreen);
                    Debug.Assert(WaveData.EnemiesOnScreen == Entities.Count(ent =>
                    {
                        return ent.EntityTypeID == EntityType.Enemy && ent.IsActive;
                    }));
                }
            }
            else
            {
                Debug.Assert(WaveData.WildPowerupCount >= 0);
                if(WaveData.WildPowerupCount == 0)
                {
                    int nextWave = WaveData.CurrentWave + 1;
                    if(nextWave < WaveData.MaxWave)
                    {
                        int newEnemyCount = nextWave;
                        WaveData.EnemyCount = newEnemyCount;

                        if(nextWave % 5 == 0
                            && WaveData.PowerupWaitTime > WaveData.MinPowerupWaitTime)
                        {
                            WaveData.PowerupWaitTime -= 0.5f;
                        }

                        UI.WaveText.text = $"Wave: {nextWave}";

                        WaveData.CurrentWave = nextWave;
                    }
                    else
                    {
                        // TODO(SpectatorQL): End screen.
                        WaveData.EnemyCount = 0;
                        Debug.Log("YOU WIN!!!");
                    }
                }
            }


            if(WaveData.PowerupCount < WaveData.MaxPowerupsOnScreen)
            {
                if(WaveData.RunningPowerupTime <= 0)
                {
                    // NOTE(SpectatorQL): Oh, so this thing is _exclusive_ but the float version is _inclusive_. Gotta love Unity...
                    PowerupType powerupType = (PowerupType)UnityEngine.Random.Range(0, (int)PowerupType.Count);
                    Debug.Assert(powerupType < PowerupType.Count);

                    PTAEntity powerupEntity = null;
                    switch(powerupType)
                    {
                        case PowerupType.Turret:
                        {
                            powerupEntity = PTAEntity.CreateTurretPowerup(this);
                            break;
                        }
                        case PowerupType.Drive:
                        {
                            powerupEntity = PTAEntity.CreateDrivePowerup(this);
                            break;
                        }
                    }

                    if(powerupEntity != null)
                    {
                        powerupEntity.Transform.position = GenerateEntityPosition(PlayArea);
                    }

                    WaveData.RunningPowerupTime = WaveData.PowerupWaitTime;
                    ++WaveData.PowerupCount;
                }
            }
            if(WaveData.RunningPowerupTime > 0)
            {
                WaveData.RunningPowerupTime -= dt;
            }


            if(Cheats.RapidFire)
            {
                PlayerEntity.Data.AttackSpeed = Cheats.RapidFireSpeed;
            }


            for(int i = 0;
                i < MAX_ENTITIES;
                ++i)
            {
                PTAEntity entity = Entities[i];
                if(entity.IsActive)
                {
                    entity.Think(this, entity, dt);
                }
            }
        }
    }
}
