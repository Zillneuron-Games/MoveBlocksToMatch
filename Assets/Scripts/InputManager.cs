using System;
using UnityEngine;

public class InputManager : MonoBehaviour
{
    public static InputManager Instance;

    float TouchFirstPosX, TouchFirstPosY;
    float TouchSecondPosX, TouchSecondPosY;
    float TouchDistance;

    public bool b_active;

    public event EventHandler<InputEventArgs> eventInput;

    public void Initialize(SoundController _sound_controller)
    {
        b_active = true;
        TouchDistance = 60;

        foreach (Button3D _button in buttons3D)
        {
            _button.buttonClick += Button3DClick;
            _button.eventInput += InputListener;
            _button.Initialize(_sound_controller);
            _button.Enable();
        }
    }

    // Update is called once per frame
    void Update()
    {
        InputListener();
    }

    public void Enable()
    {
        b_active = true;
    }
    public void Disable()
    {
        b_active = false;
    }

    private void ThrowInputEvent(InputKey _input_state)
    {
        EventHandler<InputEventArgs> tempInput = eventInput;

        if (tempInput != null)
            tempInput(this, new InputEventArgs(_input_state));
    }

    private void InputListener()
    {
        if (Input.touchCount > 0)
        {
            if (Input.GetTouch(0).phase == TouchPhase.Began)
            {
                TouchFirstPosX = Input.GetTouch(0).position.x;
                TouchFirstPosY = Input.GetTouch(0).position.y;
            }
            else if (Input.GetTouch(0).phase == TouchPhase.Moved)
            {
                TouchSecondPosX = Input.GetTouch(0).position.x;
                TouchSecondPosY = Input.GetTouch(0).position.y;

                if (Mathf.Abs(TouchFirstPosX - TouchSecondPosX) > TouchDistance || Mathf.Abs(TouchFirstPosY - TouchSecondPosY) > TouchDistance)
                {
                    if (Mathf.Abs(TouchFirstPosX - TouchSecondPosX) > Mathf.Abs(TouchFirstPosY - TouchSecondPosY))
                    {
                        if (TouchFirstPosX - TouchSecondPosX < 0)
                        {
                            ThrowInputEvent(InputKey.RIGHT);
                            return;
                        }
                        else if (TouchFirstPosX - TouchSecondPosX > 0)
                        {
                            ThrowInputEvent(InputKey.LEFT);
                            return;
                        }

                    }
                    else if (Mathf.Abs(TouchFirstPosX - TouchSecondPosX) < Mathf.Abs(TouchFirstPosY - TouchSecondPosY))
                    {
                        if (TouchFirstPosY - TouchSecondPosY < 0)
                        {
                            ThrowInputEvent(InputKey.UP);
                            return;
                        }
                        else if (TouchFirstPosY - TouchSecondPosY > 0)
                        {
                            ThrowInputEvent(InputKey.DOWN);
                            return;
                        }
                    }
                }
            }
        }
        else
        {

        }


        if (Input.GetKeyDown(KeyCode.W) || Input.GetKeyDown(KeyCode.UpArrow))
        {
            ThrowInputEvent(InputKey.UP);
            return;
        }
        else if (Input.GetKeyDown(KeyCode.S) || Input.GetKeyDown(KeyCode.DownArrow))
        {
            ThrowInputEvent(InputKey.DOWN);
            return;
        }
        else if (Input.GetKeyDown(KeyCode.A) || Input.GetKeyDown(KeyCode.LeftArrow))
        {
            ThrowInputEvent(InputKey.LEFT);
            return;
        }
        else if (Input.GetKeyDown(KeyCode.D) || Input.GetKeyDown(KeyCode.RightArrow))
        {
            ThrowInputEvent(InputKey.RIGHT);
            return;
        }
        else if (Input.GetKeyDown(KeyCode.Escape))
        {
            ThrowInputEvent(InputKey.ESCAPE);
            return;
        }
        else if (Input.GetKeyDown(KeyCode.N))
        {
            ThrowInputEvent(InputKey.NEXT);
            return;
        }
        else if (Input.GetKeyDown(KeyCode.M))
        {
            ThrowInputEvent(InputKey.MENU);
            return;
        }
        else if (Input.GetKeyDown(KeyCode.P))
        {
            ThrowInputEvent(InputKey.PAUSE);
            return;
        }
        else if (Input.GetKeyDown(KeyCode.G))
        {
            ThrowInputEvent(InputKey.PLAY);
            return;
        }
    }

    private void InputListener(object sender, InputEventArgs args)
    {
        if (!b_active)
            return;

        ThrowInputEvent(args.KEY);
    }

    private void Button3DClick(object sender, EventArgs args)
    {
        if (!b_active)
            return;

        Button3D _sender_button = sender as Button3D;

        if (_sender_button != null)
        {
            _sender_button.StartMoveAction();
        }
    }

    private void OnDestroy()
    {
        foreach (Button3D _button in buttons3D)
        {
            _button.buttonClick -= Button3DClick;
            _button.eventInput -= InputListener;
        }
    }
}