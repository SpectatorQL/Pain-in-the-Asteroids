using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;

namespace PTA
{
    [System.Serializable]
    public struct AlignmentPoints
    {
        public Vector2 LTurretPosition;
        public Vector2 RTurretPosition;
        public Vector2 DrivePosition;
    }
    
    public class PTAAlignment : MonoBehaviour
    {
        public AlignmentPoints Points;

#if UNITY_EDITOR
        public bool DrawGizmos = true;

        Vector3 RotateVectorAroundOrigin(Vector3 vector, Vector3 origin, float angle)
        {
            Vector3 result = vector;

            result.x = (((vector.x - origin.x) * Mathf.Cos(angle)) - ((vector.y - origin.y) * Mathf.Sin(angle)) + origin.x);
            result.y = (((vector.x - origin.x) * Mathf.Sin(angle)) + ((vector.y - origin.y) * Mathf.Cos(angle)) + origin.y);
            
            return result;
        }

        void OnDrawGizmos()
        {
            if(DrawGizmos)
            {
                Gizmos.color = Color.magenta;

                PTACollider[] entityColliders = FindObjectsOfType<PTACollider>();
                for(int i = 0;
                    i < entityColliders.Length;
                    ++i)
                {
                    float cubeSize = 0.1f;
                    Vector2 size = new Vector2(cubeSize, cubeSize);
                    Vector2 entityOrigin = entityColliders[i].gameObject.transform.position;
                    float entityAngle = entityColliders[i].gameObject.transform.rotation.eulerAngles.z;

                    // TODO(SpectatorQL): Investigate why this doesn't work as intended.
                    Vector3 rotatedLTurret = RotateVectorAroundOrigin(entityOrigin + Points.LTurretPosition, entityOrigin, entityAngle);

                    Gizmos.DrawCube(rotatedLTurret, size);
                    Gizmos.DrawCube(entityOrigin + Points.RTurretPosition, size);
                    Gizmos.DrawCube(entityOrigin + Points.DrivePosition, size);
                }
            }
        }
#endif
    }
}
