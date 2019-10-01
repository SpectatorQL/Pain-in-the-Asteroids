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
    
    public delegate void Move(PTAEntity entity);
    public static class MoveFunctions
    {
        public static void MoveStub(PTAEntity entity)
        {
            entity.Rigidbody.velocity = Vector2.zero;
        }
        
        public static void SineMove(PTAEntity entity)
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
        
        public static void LinearMove(PTAEntity entity)
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

        public static void WildPowerupThink(PTAMain world, PTAEntity entity, float dt)
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
        
        public static void PlayerThink(PTAMain world, PTAEntity entity, float dt)
        {
            // TODO(SpectatorQL): Android controls will be _very_ different from this.
            // TODO(SpectatorQL): Move input management out of here.
            float speed = entity.Data.MovementSpeed;
            Vector2 newPosition = entity.Transform.position;
            if(Input.GetKey(KeyCode.W))
            {
                newPosition.y += speed;
            }
            if(Input.GetKey(KeyCode.S))
            {
                newPosition.y -= speed;
            }
            if(Input.GetKey(KeyCode.A))
            {
                newPosition.x -= speed;
            }
            if(Input.GetKey(KeyCode.D))
            {
                newPosition.x += speed;
            }
            
            bool firing = false;
            if(Input.GetMouseButton(0))
            {
                firing = true;
            }
            
            Vector3 mousePosition = Input.mousePosition;
            mousePosition.z = 10.0f;
            Vector3 worldMousePosition = Camera.main.ScreenToWorldPoint(mousePosition);
            float angle = Mathf.Atan2(worldMousePosition.y - newPosition.y, worldMousePosition.x - newPosition.x);
            angle = Mathf.Rad2Deg * angle;
            
            
            entity.Transform.position = newPosition;
            Quaternion newRotation = entity.Transform.rotation;
            newRotation.eulerAngles = new Vector3(0.0f, 0.0f, angle);
            entity.Transform.rotation = newRotation;
            
            float runningAttackSpeed = entity.Data.RunningAttackSpeed;
            if(firing)
            {
                if(runningAttackSpeed <= 0)
                {
                    Vector2 fireDirection = worldMousePosition - entity.Transform.position;
                    PTAEntity newBullet = PTAEntity.CreateEntity(world, EntityType.Bullet);
                    if(newBullet != null)
                    {
                        newBullet.Transform.position = entity.Transform.position;
                        newBullet.Data.MoveDirection = fireDirection.normalized;
                        newBullet.Move = MoveFunctions.LinearMove;
                        newBullet.GameObject.layer = PTAEntity.PlayerLayer;
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
        PTAEntity[] Entities = new PTAEntity[PTAMain.ENTITY_COUNT];
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

        public int CurrentWave;
        public int MaxWave = 9;

        public float PowerupStayChance;
        public float WildPowerupSpawnChance;
    }
    
    public class PTAMain : MonoBehaviour
    {
        public const int ENTITY_COUNT = 4096;
        public PTAEntity[] Entities;
        public int RunningEntityIndex;
        // TODO(SpectatorQL): Callers should not be allowed to use this directly due to the fact that it doesn't re-initialize entities!
        public PTAFreeEntities FreeEntities = new PTAFreeEntities();
        public PTAEntity PlayerEntity;

        public PTAWaveData WaveData = new PTAWaveData();
        public PTAEnemyProbability EnemyProbability;
        
        public GameObject EntityPrefab;
        public GameObject WallPrefab;
        
        public PTAAlignment EntityAlignment;
        
        public bool Invincibility;

        struct PlayAreaDimensions
        {
            public float MinX;
            public float MinY;
            public float MaxX;
            public float MaxY;
            public Vector2 Center;
        }

        PlayAreaDimensions PlayArea;

        PTAUI UI;
        
        [EnumNamedArray(typeof(EntityType))]
        public Sprite[] Sprites = new Sprite[(int)EntityType.Count];
        [EnumNamedArray(typeof(PowerupType))]
        public Sprite[] PowerupSprites = new Sprite[(int)PowerupType.Count];

        Vector2 GenerateEntityPosition()
        {
            Vector2 result = new Vector2();

            float safetyNet = 1.0f;
            result.x = UnityEngine.Random.Range(PlayArea.MinX + safetyNet, PlayArea.MaxX - safetyNet);
            result.y = UnityEngine.Random.Range(PlayArea.MinY + safetyNet, PlayArea.MaxY - safetyNet);

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
            

            Vector2 bottomLeft = Camera.main.ScreenToWorldPoint(new Vector3(0, 0, 10));
            Vector2 topRight = Camera.main.ScreenToWorldPoint(new Vector3(Screen.width, Screen.height, 10));
            float offscreenOffset = 1.0f;
            PlayArea.MinX = bottomLeft.x - offscreenOffset;
            PlayArea.MinY = bottomLeft.y - offscreenOffset;
            PlayArea.MaxX = topRight.x + offscreenOffset;
            PlayArea.MaxY = topRight.y + offscreenOffset;
            PlayArea.Center = Camera.main.ScreenToWorldPoint(new Vector3(Screen.width / 2, Screen.height / 2, 10));

            Vector2 xWallSize = new Vector2(1.0f, PlayArea.MaxY - PlayArea.MinY);
            Vector2 yWallSize = new Vector2(PlayArea.MaxX - PlayArea.MinX, 1.0f);

            const int WALL_COUNT = 2;
            for(WallType i = 0;
                i < WallType.Count;
                ++i)
            {
                for(int j = 0;
                    j < WALL_COUNT;
                    ++j)
                {
                    PTAWall wall = Instantiate(WallPrefab).GetComponent<PTAWall>();
                    wall.World = this;
                    wall.BoxCollider = wall.gameObject.GetComponent<BoxCollider2D>();
                    wall.WallTypeID = i;
                    if(wall.WallTypeID == WallType.XWall)
                    {
                        wall.BoxCollider.size = xWallSize;
                    }
                    else if(wall.WallTypeID == WallType.YWall)
                    {
                        wall.BoxCollider.size = yWallSize;
                    }

                    if(j == 0)
                    {
                        if(wall.WallTypeID == WallType.XWall)
                        {
                            wall.transform.position = new Vector2(PlayArea.MinX, PlayArea.Center.y);
                        }
                        else if(wall.WallTypeID == WallType.YWall)
                        {
                            wall.transform.position = new Vector2(PlayArea.Center.x, PlayArea.MinY);
                        }
                    }
                    else if(j == 1)
                    {
                        if(wall.WallTypeID == WallType.XWall)
                        {
                            wall.transform.position = new Vector2(PlayArea.MaxX, PlayArea.Center.y);
                        }
                        else if(wall.WallTypeID == WallType.YWall)
                        {
                            wall.transform.position = new Vector2(PlayArea.Center.x, PlayArea.MaxY);
                        }
                    }
                    else
                    {
                        Debug.LogError($"Error! Too many walls of type {i} have been spawned!");
                    }
                }
            }


            Entities = new PTAEntity[ENTITY_COUNT];
            for(int i = 0;
                i < ENTITY_COUNT;
                ++i)
            {
                Entities[i] = new PTAEntity();
            }
            
            PlayerEntity = PTAEntity.CreateEntity(this, EntityType.Player);
            
#if UNITY_EDITOR
            PTAEntity turretL = PTAEntity.CreateTurretPowerup(this);
            turretL.Transform.position = GenerateEntityPosition();
            ++WaveData.PowerupCount;
            
            PTAEntity turretR = PTAEntity.CreateTurretPowerup(this);
            turretR.Transform.position = GenerateEntityPosition();
            ++WaveData.PowerupCount;

            PTAEntity freeTurret = PTAEntity.CreateTurretPowerup(this);
            freeTurret.Transform.position = GenerateEntityPosition();
            ++WaveData.PowerupCount;

            PTAEntity drive = PTAEntity.CreateDrivePowerup(this);
            drive.Transform.position = GenerateEntityPosition();
            ++WaveData.PowerupCount;
#else
            Invincibility = false;
#endif

            UI = FindObjectOfType<PTAUI>();
        }
        
        // TODO(SpectatorQL): Is it necessary to split Move and Think?
        void FixedUpdate()
        {
            for(int i = 0;
                i < ENTITY_COUNT;
                ++i)
            {
                PTAEntity entity = Entities[i];
                if(entity.IsActive)
                {
                    entity.Move(entity);
                }
            }
        }

        public IEnumerator TemporaryInvincibility()
        {
            Invincibility = true;
            yield return new WaitForSeconds(2.0f);

            Invincibility = false;
            yield return null;
        }

        void Update()
        {
            float dt = Time.deltaTime;

            Debug.Assert(WaveData.EnemyCount >= 0);
            Debug.Assert(WaveData.EnemiesOnScreen >= 0);
            if(WaveData.EnemyCount > 0)
            {
                Debug.Assert(WaveData.EnemiesOnScreen <= WaveData.EnemyCount
                    && WaveData.EnemiesOnScreen >= 0);
                // TODO(SpectatorQL): Do I want "rogue" detached entities to increment EnemiesOnScreen? I don't think I do but we'll see.
                while(WaveData.EnemiesOnScreen < WaveData.MaxSpawnedEnemiesOnScreen
                    && WaveData.EnemiesOnScreen < WaveData.EnemyCount)
                {
                    PTAEntity hostileEntity = PTAEntity.CreateEntity(this, EntityType.Enemy);
                    if(hostileEntity != null)
                    {
                        hostileEntity.HasSpawned = false;

                        hostileEntity.Move = MoveFunctions.SineMove;
                        hostileEntity.Data.MoveDirection = UnityEngine.Random.insideUnitCircle;
                        hostileEntity.Think = ThinkFunctions.HostileThink;

                        hostileEntity.Transform.position = GenerateEntityPosition();

                        hostileEntity.Collider.BoxCollider.enabled = false;
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

            if(WaveData.PowerupCount < WaveData.MaxPowerupsOnScreen)
            {
                if(WaveData.RunningPowerupTime < 0)
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
                        powerupEntity.Transform.position = GenerateEntityPosition();
                    }

                    WaveData.RunningPowerupTime = WaveData.PowerupWaitTime;
                    ++WaveData.PowerupCount;
                }
            }
            WaveData.RunningPowerupTime -= dt;


            for(int i = 0;
                i < ENTITY_COUNT;
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
