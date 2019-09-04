﻿using System;
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

        Turret,
        Propulsion,

        Bullet,

        Count
    }

    public enum EnemyType
    {
        Weak,
        Strong,
        Uber,

        Count
    }

    // TODO(SpectatorQL): Alignment !!!
    [Serializable]
    public class PTAEntity
    {
        public EntityData Data;
        public EntityType EntityTypeID;
        public EnemyType EnemyTypeID;
        public bool IsActive;
        public bool IsHostile;
        public bool Spawned;

        // TODO(SpectatorQL): Figure out whether we really need all of the stuff that's in a GameObject!
        // NOTE(SpectatorQL): We can't get rid of this because of SetActive() and layer.
        public GameObject GameObject;
        public Transform Transform;
        public Rigidbody2D Rigidbody;
        public SpriteRenderer Renderer;
        public PTACollider Collider;

        public Move Move;
        public Think Think;

        public PTAEntity BodySlot;
        public PTAEntity LTurretSlot;
        public PTAEntity RTurretSlot;
        public PTAEntity PropulsionSlot;

        public static void AttachEntity(PTAEntity entity, PTAEntity newParent, Vector3 alignPoint)
        {
            entity.BodySlot = null;
            entity.Collider.BoxCollider.enabled = false;

            entity.Transform.parent = newParent.Transform;
            alignPoint.x /= newParent.Transform.localScale.x;
            alignPoint.y /= newParent.Transform.localScale.y;
            entity.Transform.localPosition = alignPoint;

            entity.Transform.localRotation = Quaternion.identity;
        }

        public static void DetachEntity(PTAEntity entity)
        {
            entity.Transform.parent = null;
            entity.BodySlot = entity;
            entity.Collider.BoxCollider.enabled = true;
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


        public static int PlayerLayer;
        public static int ThingsLayer;

        public static PTAEntity CreateEntity(PTAMain world, EntityType entityType)
        {
            PTAEntity entity = world.FreeEntities.GetNext();
            if(entity == null)
            {
                entity = PTAEntity.CreateEntity(world);
                if(entity == null)
                {
                    // TODO(SpectatorQL): Do _something_ when this happens.
                    Debug.Assert(false, "__PANIC__");
                    return null;
                }
            }

            Material entityMaterial = entity.Renderer.material;
            switch(entityType)
            {
                case EntityType.Player:
                {
                    entity.GameObject.layer = PlayerLayer;
                    entity.Transform.localScale = new Vector3(2.0f, 2.0f, 2.0f);

                    entityMaterial.color = Color.white;

                    entity.Data = new EntityData();
                    entity.Data.Health = 1;
                    entity.Data.AttackSpeed = 1.5f;
                    entity.Data.MovementSpeed = 0.1f;

                    entity.Think = ThinkFunctions.PlayerThink;
                    break;
                }

                case EntityType.Enemy:
                {
                    entity.GameObject.layer = ThingsLayer;
                    entity.Transform.localScale = new Vector3(2.0f, 2.0f, 2.0f);

                    entity.Spawned = false;

                    EntityData entityData = new EntityData();
                    EnemyType enemyType = DetermineEnemyType(world);
                    switch(enemyType)
                    {
                        case EnemyType.Weak:
                        {
                            entityMaterial.color = Color.red;

                            entityData.Health = 1;
                            break;
                        }
                        case EnemyType.Strong:
                        {
                            entityMaterial.color = Color.yellow;

                            entityData.Health = 2;
                            break; 
                        }
                        case EnemyType.Uber:
                        {
                            entityMaterial.color = Color.green;

                            entityData.Health = 3;
                            break;
                        }
                    }
                    
                    entityData.MovementSpeed = 0.08f;
                    entityData.TimeToSpawn = 2.0f;

                    entity.EnemyTypeID = enemyType;
                    entity.Data = entityData;
                    break;
                }

                case EntityType.Turret:
                {
                    entity.Transform.localScale = new Vector3(1.0f, 1.0f, 1.0f);

                    entityMaterial.color = Color.white;

                    entity.Move = MoveFunctions.MoveStub;
                    entity.Think = ThinkFunctions.ThinkStub;

                    entity.GameObject.layer = ThingsLayer;
                    break;
                }

                case EntityType.Propulsion:
                {
                    entity.Transform.localScale = new Vector3(1.0f, 1.0f, 1.0f);

                    entityMaterial.color = Color.white;

                    entity.Move = MoveFunctions.MoveStub;
                    entity.Think = ThinkFunctions.ThinkStub;

                    entity.GameObject.layer = ThingsLayer;
                    break;
                }

                case EntityType.Bullet:
                {
                    entity.Transform.localScale = new Vector3(1.0f, 1.0f, 1.0f);

                    entity.Collider.BoxCollider.enabled = true;

                    entityMaterial.color = Color.white;

                    entity.Move = MoveFunctions.MoveStub;
                    entity.Think = ThinkFunctions.ThinkStub;

                    entity.Data.MovementSpeed = 0.3f;
                    break;
                }


                case EntityType.Invalid:
                {
                    Debug.LogError("Invalid entity type!");
                    break;
                }
                default:
                {
                    Debug.LogError("Invalid default case!");
                    break;
                }
            }

            entity.EntityTypeID = entityType;
            entity.IsHostile = false;

            entity.Renderer.sprite = world.Sprites[(int)entityType];

            return entity;
        }

        static PTAEntity CreateEntity(PTAMain world)
        {
            if(world.RunningEntityIndex >= PTAMain.ENTITY_COUNT)
            {
                Debug.LogError("PTAEntity limit reached!");
                return null;
            }

            PTAEntity entity = world.Entities[world.RunningEntityIndex];
            GameObject entityObject = GameObject.Instantiate(world.EntityPrefab);

            entity.GameObject = entityObject;
            entity.Transform = entityObject.transform;
            entity.Rigidbody = entityObject.GetComponent<Rigidbody2D>();
            entity.Renderer = entityObject.GetComponent<SpriteRenderer>();
            entity.Collider = entityObject.GetComponent<PTACollider>();
            entity.Collider.BoxCollider = entityObject.GetComponent<BoxCollider2D>();
            entity.Collider.Self = entity;
            entity.Collider.World = world;

            entity.BodySlot = entity;

            entity.Move = MoveFunctions.MoveStub;
            entity.Think = ThinkFunctions.ThinkStub;

            entity.IsActive = true;

            ++world.RunningEntityIndex;

            return entity;
        }
    }
}
