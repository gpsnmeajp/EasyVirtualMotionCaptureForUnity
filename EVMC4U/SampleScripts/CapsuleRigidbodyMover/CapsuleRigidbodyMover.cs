/*
 * ExternalReceiver
 * https://sabowl.sakura.ne.jp/gpsnmeajp/
 *
 * MIT License
 * 
 * Copyright (c) 2019 gpsnmeajp
 * 
 * Permission is hereby granted, free of charge, to any person obtaining a copy
 * of this software and associated documentation files (the "Software"), to deal
 * in the Software without restriction, including without limitation the rights
 * to use, copy, modify, merge, publish, distribute, sublicense, and/or sell
 * copies of the Software, and to permit persons to whom the Software is
 * furnished to do so, subject to the following conditions:
 * 
 * The above copyright notice and this permission notice shall be included in all
 * copies or substantial portions of the Software.
 * 
 * THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
 * IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
 * FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
 * AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
 * LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
 * OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE
 * SOFTWARE.
 */
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
namespace EVMC4U
{
    public class CapsuleRigidbodyMover : MonoBehaviour
    {
        public Transform MovePos;
        public Transform MoveRot;
        public Transform chest;

        EDirection direction = EDirection.STOP;
        bool click = false;

        enum EDirection {
            STOP,
            FORWARD,
            BACK,
            LEFT,
            RIGHT
        }

        // Start is called before the first frame update
        void Start()
        {
        }

        // Update is called once per frame
        void Update()
        {
            switch (direction) {
                case EDirection.FORWARD: MovePos.position += chest.forward * 10f * Time.deltaTime; break;
                case EDirection.BACK: MovePos.position += chest.forward * -10f * Time.deltaTime; break;
                case EDirection.LEFT: MoveRot.Rotate(new Vector3(0, -100f * Time.deltaTime, 0)); break;
                case EDirection.RIGHT: MoveRot.Rotate(new Vector3(0, 100f * Time.deltaTime, 0)); break;
                default: break;
            }
        }

        public void KeyInputEvent(EVMC4U.KeyInput key)
        {
            if (key.active == 1)
            {
                switch (key.name) {
                    case "W": direction = EDirection.FORWARD; break;
                    case "S": direction = EDirection.BACK; break;
                    case "A": direction = EDirection.LEFT; break;
                    case "D": direction = EDirection.RIGHT; break;
                }
            }else{
                direction = 0;
            }
        }

        public void ControllerInputEvent(EVMC4U.ControllerInput con)
        {
            if (con.name == "PositionTrackpad")
            {
                if (con.IsAxis == 1)
                {
                    var rot = Mathf.Atan2(con.Axis.x, con.Axis.y) * Mathf.Rad2Deg;

                    if (-45 <= rot && rot < 45)
                    {
                        direction = EDirection.FORWARD;
                    }
                    else if (45 <= rot && rot < 135)
                    {
                        direction = EDirection.RIGHT;
                    }
                    else if (-135 <= rot && rot < -45)
                    {
                        direction = EDirection.LEFT;
                    }
                    else
                    {
                        direction = EDirection.BACK;
                    }
                }
            }
            if(con.name == "ClickTrackpad")
            {
                click = con.active == 1;
            }
            if (!click)
            {
                direction = EDirection.STOP;
            }
        }
    }
}