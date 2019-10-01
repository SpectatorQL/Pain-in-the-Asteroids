using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace PTA
{
    public class PTACollider : MonoBehaviour
    {
        public PTAEntity Self;
        public BoxCollider2D BoxCollider;
        
        public PTAMain World;
        
        void DetermineEntityFate(PTAEntity entity)
        {
            float spawnChance = Random.value;
            if(spawnChance < World.WaveData.PowerupStayChance)
            {
                World.FreeEntities.Add(entity);
            }
            else
            {
                spawnChance = Random.value;
                if(spawnChance < World.WaveData.WildPowerupSpawnChance)
                {
                    World.FreeEntities.Add(entity);
                }
                else
                {
                    PTAEntity.TurnIntoWildPowerup(World, entity);
                }
            }
        }

        void DetachEntitiesOnDeath(PTAEntity entity)
        {
            // NOTE(SpectatorQL): I would really like to put these in a union and just loop through an array, but oh well.
            PTAEntity lTurret = Self.LTurretSlot;
            PTAEntity rTurret = Self.RTurretSlot;
            PTAEntity drive = Self.DriveSlot;

            if(lTurret != null)
            {
                PTAEntity.DetachEntity(lTurret);
                Self.LTurretSlot = null;

                DetermineEntityFate(lTurret);
            }
            if(rTurret != null)
            {
                PTAEntity.DetachEntity(rTurret);
                Self.RTurretSlot = null;

                DetermineEntityFate(rTurret);
            }
            if(drive != null)
            {
                PTAEntity.DetachEntity(drive);
                Self.DriveSlot = null;

                DetermineEntityFate(drive);
            }
        }

        // NOTE(SpectatorQL): Some day I need to learn how to handle a mess like this...
        // Like, where does this code _actually_ belong? What should it _actually_ do?
        // Create messages for the world to process? Act immediately, like I do here?
        // Or maybe an entirely different approach is "the right thing to do"?
        void OnCollisionEnter2D(Collision2D collision)
        {
            PTACollider collider = collision.gameObject.GetComponent<PTACollider>();
            if(collider != null)
            {
                PTAEntity other = collider.Self;
                if(Self.EntityTypeID == EntityType.Player
                   || Self.EntityTypeID == EntityType.Enemy
                   || Self.EntityTypeID == EntityType.WildPowerup)
                {
                    if(other.EntityTypeID == EntityType.Powerup)
                    {
                        if(other.PowerupTypeID == PowerupType.Turret)
                        {
                            if(Self.LTurretSlot == null)
                            {
                                PTAEntity.AttachEntity(other, Self, World.EntityAlignment.Points.LTurretPosition);
                                Self.LTurretSlot = other;
                                --World.WaveData.PowerupCount;
                            }
                            else if(Self.RTurretSlot == null)
                            {
                                PTAEntity.AttachEntity(other, Self, World.EntityAlignment.Points.RTurretPosition);
                                Self.RTurretSlot = other;
                                --World.WaveData.PowerupCount;
                            }
                        }
                        else if(other.PowerupTypeID == PowerupType.Drive)
                        {
                            if(Self.DriveSlot == null)
                            {
                                PTAEntity.AttachEntity(other, Self, World.EntityAlignment.Points.DrivePosition);
                                Self.DriveSlot = other;
                                --World.WaveData.PowerupCount;
                            }
                        }
                    }

                    else if(other.EntityTypeID == EntityType.Bullet)
                    {
                        World.FreeEntities.Add(other);
                        
                        --Self.Data.Health;
                        if(Self.Data.Health == 0)
                        {
                            DetachEntitiesOnDeath(Self);
                            World.FreeEntities.Add(Self);

                            if(Self.EntityTypeID == EntityType.Enemy)
                            {
                                --World.WaveData.EnemyCount;
                                --World.WaveData.EnemiesOnScreen;
                            }
                        }
                    }

                    else if(Self.EntityTypeID == EntityType.Player
                        && other.EntityTypeID == EntityType.Enemy)
                    {
                        if(!World.Invincibility)
                        {
                            World.StartCoroutine(World.TemporaryInvincibility());

                            World.FreeEntities.Add(other);

                            --Self.Data.Health;
                            if(Self.Data.Health == 0)
                            {
                                World.FreeEntities.Add(Self.LTurretSlot);
                                World.FreeEntities.Add(Self.RTurretSlot);
                                World.FreeEntities.Add(Self.DriveSlot);
                                World.FreeEntities.Add(Self);
                                Debug.Log("Guess I'll die.");
                            }
                        }
                    }

                    else if(Self.EntityTypeID == EntityType.Player
                        && other.EntityTypeID == EntityType.WildPowerup)
                    {
                        if(!World.Invincibility)
                        {
                            World.StartCoroutine(World.TemporaryInvincibility());

                            World.FreeEntities.Add(other);

                            --Self.Data.Health;
                            if(Self.Data.Health == 0)
                            {
                                World.FreeEntities.Add(Self.LTurretSlot);
                                World.FreeEntities.Add(Self.RTurretSlot);
                                World.FreeEntities.Add(Self.DriveSlot);
                                World.FreeEntities.Add(Self);
                                Debug.Log("Guess I'll die.");
                            }
                        }
                    }

                    else if(Self.EntityTypeID == EntityType.Enemy
                        && other.EntityTypeID == EntityType.Player)
                    {
                        --Self.Data.Health;
                        if(Self.Data.Health == 0)
                        {
                            DetachEntitiesOnDeath(Self);
                            World.FreeEntities.Add(Self);

                            --World.WaveData.EnemyCount;
                            --World.WaveData.EnemiesOnScreen;
                        }
                    }

                    else if(Self.EntityTypeID == EntityType.WildPowerup
                        && other.EntityTypeID == EntityType.Player)
                    {
                        --Self.Data.Health;
                        if(Self.Data.Health == 0)
                        {
                            DetachEntitiesOnDeath(Self);
                            World.FreeEntities.Add(Self);
                        }
                    }
#if false
                    else
                    {
                        Debug.Log($"Unknown collision occured!\nSelf: {Self.EntityTypeID}, Other: {other.EntityTypeID}");
                    }
#endif
                }
            }
        }
    }
}
