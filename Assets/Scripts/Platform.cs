using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace PTA
{
    [System.Serializable]
    public class PTAInput
    {
        public float LX;
        public float LY;
        public float RX;
        public float RY;

        public float MouseX;
        public float MouseY;

        public bool Fire;
    }

    public abstract class PTAPlatform : ScriptableObject
    {
        public abstract void GetInput(PTAInput input);
    }

    [CreateAssetMenu]
    public class Win32 : PTAPlatform
    {
        public override void GetInput(PTAInput input)
        {
            // TODO(SpectatorQL): IsAnalog!
            input.LX = Input.GetAxis("LX");
            input.LY = Input.GetAxis("LY");
            input.RX = Input.GetAxis("RX");
            input.RY = Input.GetAxis("RY");

            input.MouseX = Input.mousePosition.x;
            input.MouseY = Input.mousePosition.y;

            input.Fire = Input.GetMouseButton(0);
        }
    }
}
