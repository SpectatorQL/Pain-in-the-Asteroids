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
                   || Self.EntityTypeID == EntityType.Enemy)
                {
                    if(other.EntityTypeID == EntityType.Turret)
                    {
                        if(Self.LTurretSlot == null)
                        {
                            PTAEntity.AttachEntity(other, Self, World.EntityAlignment.Points.LTurretPosition);
                            Self.LTurretSlot = other;
                        }
                        else if(Self.RTurretSlot == null)
                        {
                            PTAEntity.AttachEntity(other, Self, World.EntityAlignment.Points.RTurretPosition);
                            Self.RTurretSlot = other;
                        }
                    }
                    else if(other.EntityTypeID == EntityType.Propulsion)
                    {
                        if(Self.PropulsionSlot == null)
                        {
                            PTAEntity.AttachEntity(other, Self, World.EntityAlignment.Points.PropulsionPosition);
                            Self.PropulsionSlot = other;
                        }
                    }

                    else if(other.EntityTypeID == EntityType.Bullet)
                    {
                        World.FreeEntities.Add(other);
                        
                        --Self.Data.Health;
                        if(Self.Data.Health == 0)
                        {
                            // TODO(SpectatorQL): Percent chance for detached entities to become enemies.
                            // TODO(SpectatorQL): Percent chance for detached entities to stay on the map.
                            PTAEntity lTurret = Self.LTurretSlot;
                            PTAEntity rTurret = Self.RTurretSlot;
                            PTAEntity propulsion = Self.PropulsionSlot;
                            if(lTurret != null)
                            {
                                PTAEntity.DetachEntity(lTurret);
                                World.FreeEntities.Add(lTurret);
                                Self.LTurretSlot = null;
                            }
                            if(rTurret != null)
                            {
                                PTAEntity.DetachEntity(rTurret);
                                World.FreeEntities.Add(rTurret);
                                Self.RTurretSlot = null;
                            }
                            if(propulsion != null)
                            {
                                PTAEntity.DetachEntity(propulsion);
                                World.FreeEntities.Add(propulsion);
                                Self.PropulsionSlot = null;
                            }
                            
                            World.FreeEntities.Add(Self);

                            if(Self.IsHostile)
                            {
                                --World.WaveData.EnemyCount;
                                --World.WaveData.EnemiesOnScreen;
                            }
                            Debug.Log("You are dead. Not a big suprise.");
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
                                World.FreeEntities.Add(Self.PropulsionSlot);
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
                            // TODO(SpectatorQL): Percent chance for detached entities to become enemies.
                            // TODO(SpectatorQL): Percent chance for detached entities to stay on the map.
                            PTAEntity lTurret = Self.LTurretSlot;
                            PTAEntity rTurret = Self.RTurretSlot;
                            PTAEntity propulsion = Self.PropulsionSlot;
                            if(lTurret != null)
                            {
                                PTAEntity.DetachEntity(lTurret);
                                World.FreeEntities.Add(lTurret);
                                Self.LTurretSlot = null;
                            }
                            if(rTurret != null)
                            {
                                PTAEntity.DetachEntity(rTurret);
                                World.FreeEntities.Add(rTurret);
                                Self.RTurretSlot = null;
                            }
                            if(propulsion != null)
                            {
                                PTAEntity.DetachEntity(propulsion);
                                World.FreeEntities.Add(propulsion);
                                Self.PropulsionSlot = null;
                            }

                            World.FreeEntities.Add(Self);

                            if(Self.IsHostile)
                            {
                                --World.WaveData.EnemyCount;
                                --World.WaveData.EnemiesOnScreen;
                            }
                            Debug.Log("You are dead. Not a big surprise.");
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
