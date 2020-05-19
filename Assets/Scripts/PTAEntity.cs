using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace PTA
{
    [Serializable]
    public struct EntityData
    {
        public Vector2 MoveDirection;

        public int Health;
        public float AttackSpeed;
        public float RunningAttackSpeed;
        public float MovementSpeed;
        public float TSine;
        public float TimeToSpawn;
    }

    public enum EntityType
    {
        Invalid,
        Player,
        Enemy,

        Bullet,

        Powerup,
        WildPowerup,

        Count
    }

    public enum EnemyType
    {
        Weak,
        Strong,
        Uber,

        Count
    }

    public enum PowerupType
    {
        Turret,
        Drive,

        Count
    }

    // TODO(SpectatorQL): Alignment !!!
    [Serializable]
    public class PTAEntity
    {
        public EntityData Data;
        public uint EntityID;
        public EntityType EntityTypeID;
        public EnemyType EnemyTypeID;
        public PowerupType PowerupTypeID;
        public bool IsActive;
        public bool HasSpawned;

        // TODO(SpectatorQL): Figure out whether we really need all of the stuff that's in a GameObject!
        // NOTE(SpectatorQL): We can't get rid of this because of SetActive() and layer.
        public GameObject GameObject;
        public Transform Transform;
        public Rigidbody2D Rigidbody;
        public SpriteRenderer Renderer;
        public PTACollider Collider;

        public Move Move;
        public Think Think;

        public PTAEntity ParentSlot;
        public PTAEntity LTurretSlot;
        public PTAEntity RTurretSlot;
        public PTAEntity DriveSlot;

        public static void AttachEntity(PTAEntity entity, PTAEntity newParent, Vector3 alignPoint)
        {
            entity.Collider.BoxCollider.enabled = false;

            entity.Transform.parent = newParent.Transform;
            alignPoint.x /= newParent.Transform.localScale.x;
            alignPoint.y /= newParent.Transform.localScale.y;
            entity.Transform.localPosition = alignPoint;

            entity.Transform.localRotation = Quaternion.identity;

            entity.ParentSlot = newParent;
        }

        public static void DetachEntity(PTAEntity entity)
        {
            entity.Transform.parent = null;
            entity.Collider.BoxCollider.enabled = true;

            entity.ParentSlot = null;
        }


        public static int PlayerLayer;
        public static int ThingsLayer;

        public static PTAEntity TurnIntoWildPowerup(PTAMain world, PTAEntity entity)
        {
            entity.Move = MoveFunctions.HomingMove;
            entity.Think = ThinkFunctions.ThinkStub;

            entity.EntityTypeID = EntityType.WildPowerup;
            entity.Data.Health = 1;
            entity.Data.MovementSpeed = 0.08f;

            return entity;
        }

        public static PTAEntity CreateTurretPowerup(PTAMain world)
        {
            PTAEntity entity = CreatePowerupInternal(world);

            PowerupType powerupType = PowerupType.Turret;
            entity.Renderer.sprite = world.PowerupSprites[(int)powerupType];
            entity.PowerupTypeID = powerupType;

            return entity;
        }

        public static PTAEntity CreateDrivePowerup(PTAMain world)
        {
            PTAEntity entity = CreatePowerupInternal(world);

            PowerupType powerupType = PowerupType.Drive;
            entity.Renderer.sprite = world.PowerupSprites[(int)powerupType];
            entity.PowerupTypeID = powerupType;

            return entity;
        }

        static PTAEntity CreatePowerupInternal(PTAMain world)
        {
            PTAEntity entity = GetFreeEntity(world);

            entity.Transform.localScale = new Vector3(1.0f, 1.0f, 1.0f);
            entity.Transform.rotation = Quaternion.identity;

            entity.Renderer.material.color = Color.white;

            entity.Move = MoveFunctions.MoveStub;
            entity.Think = ThinkFunctions.ThinkStub;

            entity.GameObject.layer = ThingsLayer;

            entity.EntityTypeID = EntityType.Powerup;
            entity.HasSpawned = true;

            return entity;
        }

        public static PTAEntity CreateFriendlyBullet(PTAMain world)
        {
            PTAEntity entity = CreateBulletInternal(world);

            entity.Move = MoveFunctions.LinearMove;
            entity.Think = ThinkFunctions.ThinkStub;
            entity.GameObject.layer = PlayerLayer;

            entity.Renderer.material.color = Color.white;

            entity.Data.MovementSpeed = 0.3f;

            return entity;
        }

        static PTAEntity CreateBulletInternal(PTAMain world)
        {
            PTAEntity entity = GetFreeEntity(world);

            entity.Transform.localScale = new Vector3(1.0f, 1.0f, 1.0f);
            entity.Transform.rotation = Quaternion.identity;

            entity.Collider.BoxCollider.enabled = true;

            entity.EntityTypeID = EntityType.Bullet;
            entity.HasSpawned = true;

            entity.Renderer.sprite = world.Sprites[(int)EntityType.Bullet];

            return entity;
        }

        static EnemyType DetermineEnemyType(PTAMain world)
        {
            EnemyType result = EnemyType.Weak;
            // NOTE(SpectatorQL): If randomPoint equals 1 there's a chance that the following loop code will cause an off-by-one error.
            // It is extremely unlikely, because floating point precision is dumb enough to allow me to do this shit 99.999999% of the time.
            // But at some point in the future this thing WILL crash and I'll be extremely sad when it does.
            // That's also why I think I should use integers instead of floats for probability-based things and only
            // cast to float when absolutely necessary.
            // NOTE(SpectatorQL): Ok, so the thing is like this. .NET System.Random.NextDouble() function
            // will _NEVER_ return 1.0 but Unity Random.value property _CAN_ possibly return 1.0.
            // This the single most retarded thing I have encountered in programming up to this point in time {2019-08-20, 14:33}.
            float randomPoint = UnityEngine.Random.value;
            if(randomPoint == 1.0f)
            {
                randomPoint = 0.99999999999999978f;
            }

            // NOTE(SpectatorQL): Because Unity is dogshit.
            float[] enemyProb = world.EnemyProbability.Values;
            int enemyTypeCount = (int)EnemyType.Count;
            for(int i = 0;
                i < enemyTypeCount;
                ++i)
            {
                int index = world.WaveData.CurrentWave * enemyTypeCount + i;
                Debug.Assert(index < enemyProb.Length);
                if(randomPoint < enemyProb[index])
                {
                    result = (EnemyType)i;
                    break;
                }
                else
                {
                    randomPoint -= enemyProb[i];
                }
            }

            Debug.Assert(result < EnemyType.Count);
            return result;
        }

        public static PTAEntity CreateEnemy(PTAMain world)
        {
            PTAEntity entity = GetFreeEntity(world);

            entity.GameObject.layer = ThingsLayer;
            entity.Transform.rotation = Quaternion.identity;
            entity.Transform.localScale = new Vector3(2.0f, 2.0f, 2.0f);

            EntityData entityData = new EntityData();
            EnemyType enemyType = DetermineEnemyType(world);
            switch(enemyType)
            {
                case EnemyType.Weak:
                {
                    entity.Renderer.material.color = Color.red;

                    entityData.Health = 1;
                    entityData.MovementSpeed = 0.08f;
                    entityData.TimeToSpawn = 2.0f;
                    break;
                }
                case EnemyType.Strong:
                {
                    entity.Renderer.material.color = Color.yellow;

                    entityData.Health = 2;
                    entityData.MovementSpeed = 0.08f;
                    entityData.TimeToSpawn = 2.0f;
                    break;
                }
                case EnemyType.Uber:
                {
                    entity.Renderer.material.color = Color.green;

                    entityData.Health = 3;
                    entityData.MovementSpeed = 0.08f;
                    entityData.TimeToSpawn = 2.0f;
                    break;
                }
            }
            entity.EnemyTypeID = enemyType;
            entity.Data = entityData;

            entity.EntityTypeID = EntityType.Enemy;
            entity.HasSpawned = true;

            entity.Renderer.sprite = world.Sprites[(int)EntityType.Enemy];

            return entity;
        }

        public static PTAEntity CreatePlayer(PTAMain world)
        {
            PTAEntity entity = GetFreeEntity(world);

            entity.GameObject.layer = PlayerLayer;

            entity.Transform.localScale = new Vector3(2.0f, 2.0f, 2.0f);
            entity.Transform.rotation = Quaternion.identity;

            entity.Renderer.material.color = Color.white;
            entity.Renderer.sprite = world.Sprites[(int)EntityType.Player];

            entity.Data = new EntityData
            {
                Health = 1,
                AttackSpeed = 1.5f,
                MovementSpeed = 0.1f
            };

            entity.Think = ThinkFunctions.PlayerThink;

            entity.EntityTypeID = EntityType.Player;
            entity.HasSpawned = true;

            return entity;
        }

        static PTAEntity GetFreeEntity(PTAMain world)
        {
            PTAEntity entity = world.FreeEntities.GetNext();
            if(entity == null)
            {
                entity = CreateEntity(world);
                if(entity == null)
                {
                    // TODO(SpectatorQL): Do _something_ when this happens.
                    Debug.Assert(false, "__PANIC__");
                    return null;
                }
            }

            return entity;
        }

        static PTAEntity CreateEntity(PTAMain world)
        {
            if(world.EntityCount >= PTAMain.MAX_ENTITIES)
            {
                Debug.LogError("PTAEntity limit reached!");
                return null;
            }

            PTAEntity entity = world.Entities[world.EntityCount];
            GameObject entityObject = GameObject.Instantiate(world.EntityPrefab);

            entity.GameObject = entityObject;
            entity.Transform = entityObject.transform;
            entity.Rigidbody = entityObject.GetComponent<Rigidbody2D>();
            entity.Renderer = entityObject.GetComponent<SpriteRenderer>();
            entity.Collider = entityObject.GetComponent<PTACollider>();
            entity.Collider.BoxCollider = entityObject.GetComponent<BoxCollider2D>();
            entity.Collider.Self = entity;
            entity.Collider.World = world;

            entity.Move = MoveFunctions.MoveStub;
            entity.Think = ThinkFunctions.ThinkStub;

            entity.IsActive = true;

            entity.EntityID = (uint)world.EntityCount;
            ++world.EntityCount;

            return entity;
        }
    }
}
