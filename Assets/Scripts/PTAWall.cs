using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace PTA
{
    public enum WallType
    {
        X,
        Y,

        Count
    }

    public class PTAWall : MonoBehaviour
    {
        public PTAMain World;
        public BoxCollider2D BoxCollider;
        
        public WallType WallTypeID;

        public static PTAWall CreateWall(PTAMain world, GameObject wallPrefab, Vector2 position, Vector2 wallSize, WallType wallType)
        {
            PTAWall wall = Instantiate(wallPrefab).GetComponent<PTAWall>();
            wall.World = world;
            wall.BoxCollider = wall.gameObject.GetComponent<BoxCollider2D>();
            wall.BoxCollider.size = wallSize;
            wall.transform.position = position;
            wall.WallTypeID = wallType;

            return wall;
        }

        void OnCollisionEnter2D(Collision2D collision)
        {
            PTACollider collider = collision.gameObject.GetComponent<PTACollider>();
            if(collider != null)
            {
                PTAEntity entity = collider.Self;
                if(entity.EntityTypeID == EntityType.Bullet)
                {
                    World.FreeEntities.Add(entity);
                }
                else
                {
                    float safetyNet = 0.25f;
                    Vector2 oldPosition = collision.gameObject.transform.position;
                    Vector2 newPosition = new Vector2();
                    if(WallTypeID == WallType.X)
                    {
                        if(oldPosition.x > 0)
                        {
                            newPosition.x = -(oldPosition.x - safetyNet);
                        }
                        else
                        {
                            newPosition.x = -(oldPosition.x + safetyNet);
                        }
                        newPosition.y = oldPosition.y;
                    }
                    else if(WallTypeID == WallType.Y)
                    {
                        newPosition.x = oldPosition.x;
                        if(transform.position.y > 0)
                        {
                            newPosition.y = -(oldPosition.y - safetyNet);
                        }
                        else
                        {
                            newPosition.y = -(oldPosition.y + safetyNet);
                        }
                    }

                    collision.gameObject.transform.position = newPosition;
                }
            }
        }
    }
}
