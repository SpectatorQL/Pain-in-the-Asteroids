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
            if(entity.EntityTypeID == EntityType.Enemy
                && !entity.Spawned)
                return;

            entity.Rigidbody.velocity = Vector2.zero;
            
            Vector2 newPosition = entity.Transform.position;
            
            newPosition.x += entity.Data.MovementSpeed;
            float sineValue = Mathf.Sin(entity.Data.TSine);
            float sineRange = 0.10f;
            sineValue = Mathf.Clamp(sineValue, -sineRange, sineRange);
            newPosition.y += sineValue;
            
            entity.Rigidbody.MovePosition(newPosition);
            
            if(entity.Data.TSine > (2.0f * Mathf.PI))
            {
                entity.Data.TSine -= (2.0f * Mathf.PI);
            }
            entity.Data.TSine += 0.15f;
        }
        
        public static void LinearMove(PTAEntity entity)
        {
            if(entity.EntityTypeID == EntityType.Enemy
                && !entity.Spawned)
                return;

            entity.Rigidbody.velocity = Vector2.zero;
            
            Vector2 newPosition = (Vector2)entity.Transform.position + entity.Data.MoveDirection.normalized * entity.Data.MovementSpeed;
            entity.Rigidbody.MovePosition(newPosition);
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
                --Top;
            }
            
            return result;
        }
    }
    
    [Serializable]
    public class PTAEnemyProbability
    {
        // TODO(SpectatorQL): Make an editor extension for setting these!
        public float[] Values = new float[PTAWaveData.MAX_WAVE * (int)EnemyType.Count];
        public const float PROBABILITY_TOTAL = 1.0f;
    }

    [Serializable]
    public class PTAWaveData
    {
        public int EnemyCount;
        public int EnemiesOnScreen;

        public int CurrentWave;
        public const int MAX_WAVE = 20;
    }
    
    public class PTAMain : MonoBehaviour
    {
        public const int ENTITY_COUNT = 4096;
        public PTAEntity[] Entities;
        public int RunningEntityIndex;
        // TODO(SpectatorQL): Callers should not be allowed to use this directly due to the fact that it doesn't re-initialize entities!
        public PTAFreeEntities FreeEntities = new PTAFreeEntities();
        public PTAEntity PlayerEntity;

        public PTAWaveData WaveData;
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
            PTAEntity turretL = PTAEntity.CreateEntity(this, EntityType.Turret);
            turretL.Transform.position = new Vector2(-9.0f, -3.0f);
            
            PTAEntity turretR = PTAEntity.CreateEntity(this, EntityType.Turret);
            turretR.Transform.position = new Vector2(5.0f, -3.0f);
            
            PTAEntity freeTurret = PTAEntity.CreateEntity(this, EntityType.Turret);
            freeTurret.Transform.position = new Vector2(8.0f, 1.5f);
            
            PTAEntity propulsion = PTAEntity.CreateEntity(this, EntityType.Propulsion);
            propulsion.Transform.position = new Vector2(0.0f, -2.5f);
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
        
        IEnumerator WaitBeforeActivatingEntity(PTAEntity entity)
        {
            yield return new WaitForSeconds(2.0f);

            entity.Collider.BoxCollider.enabled = true;
            entity.Spawned = true;
            yield return null;
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

            if(WaveData.EnemyCount <= 0)
            {
                if(WaveData.CurrentWave < PTAWaveData.MAX_WAVE)
                {
                    int nextWave = WaveData.CurrentWave + 1;
                    WaveData.EnemyCount = nextWave;
                    WaveData.EnemiesOnScreen = nextWave;

                    int newEnemies = WaveData.EnemyCount;
                    WaveData.EnemiesOnScreen = newEnemies;
                    while(newEnemies > 0)
                    {
                        PTAEntity hostileEntity = PTAEntity.CreateEntity(this, EntityType.Enemy);
                        if(hostileEntity != null)
                        {
                            hostileEntity.IsHostile = true;
                            hostileEntity.Move = MoveFunctions.LinearMove;
                            hostileEntity.Data.MoveDirection = UnityEngine.Random.insideUnitCircle;
                            hostileEntity.Think = ThinkFunctions.HostileThink;

                            Vector3 entityPosition = new Vector3();
                            entityPosition.x = UnityEngine.Random.Range(PlayArea.MinX, PlayArea.MaxX);
                            entityPosition.y = UnityEngine.Random.Range(PlayArea.MinY, PlayArea.MaxY);
                            hostileEntity.Transform.position = entityPosition;

                            hostileEntity.Collider.BoxCollider.enabled = false;
                            StartCoroutine(WaitBeforeActivatingEntity(hostileEntity));
                        }

                        --newEnemies;
                    }

                    WaveData.CurrentWave = nextWave;

                    UI.WaveText.text = $"Wave: {WaveData.CurrentWave}";
                }
                else
                {
                    // TODO(SpectatorQL): End screen.
                    Debug.Log("YOU WIN!!!");
                }
            }
            else if(WaveData.EnemiesOnScreen == 0)
            {
                int newEnemies = WaveData.CurrentWave;
                if(newEnemies > WaveData.EnemyCount)
                {
                    newEnemies = WaveData.EnemyCount;
                }
                WaveData.EnemiesOnScreen = newEnemies;
                while(newEnemies > 0)
                {
                    PTAEntity hostileEntity = PTAEntity.CreateEntity(this, EntityType.Enemy);
                    if(hostileEntity != null)
                    {
                        hostileEntity.IsHostile = true;
                        hostileEntity.Move = MoveFunctions.LinearMove;
                        hostileEntity.Data.MoveDirection = UnityEngine.Random.insideUnitCircle;
                        hostileEntity.Think = ThinkFunctions.HostileThink;

                        Vector3 entityPosition = new Vector3();
                        entityPosition.x = UnityEngine.Random.Range(PlayArea.MinX, PlayArea.MaxX);
                        entityPosition.y = UnityEngine.Random.Range(PlayArea.MinY, PlayArea.MaxY);
                        hostileEntity.Transform.position = entityPosition;

                        hostileEntity.Collider.BoxCollider.enabled = false;
                        StartCoroutine(WaitBeforeActivatingEntity(hostileEntity));
                    }

                    --newEnemies;
                }
            }

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
