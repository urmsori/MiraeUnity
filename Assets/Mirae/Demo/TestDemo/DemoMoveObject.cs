using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Mirae.Demo
{
    public class DemoMoveObject : MonoBehaviour
    {
        private RectTransform rt;
        // Use this for initialization
        void Start()
        {
            rt = GetComponent<RectTransform>();
        }

        public enum MoveType { Up, Down, Right, Left }
        public void Move(MoveType type)
        {
            var moveVector = new Vector3(0, 0);
            switch (type)
            {
                case MoveType.Up:
                    moveVector = new Vector3(0, rt.sizeDelta.y);
                    break;
                case MoveType.Down:
                    moveVector = new Vector3(0, -rt.sizeDelta.y);
                    break;
                case MoveType.Right:
                    moveVector = new Vector3(rt.sizeDelta.x, 0);
                    break;
                case MoveType.Left:
                    moveVector = new Vector3(-rt.sizeDelta.x, 0);
                    break;
                default:
                    break;
            }


            rt.position = rt.position + moveVector;
        }
        public void MoveUp()
        {
            Move(MoveType.Up);
        }
        public void MoveDown()
        {
            Move(MoveType.Down);
        }
        public void MoveRight()
        {
            Move(MoveType.Right);
        }
        public void MoveLeft()
        {
            Move(MoveType.Left);
        }
    }
}