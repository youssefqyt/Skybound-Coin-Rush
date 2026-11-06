using System;
using UnityEngine;
using UnityEngine.EventSystems;

namespace Skybound
{
    /// <summary>
    /// Touch-first runner input with forgiving swipe detection, keyboard fallback
    /// and a public API for optional on-screen controls.
    /// </summary>
    public class PlayerInput : MonoBehaviour
    {
        public bool Enabled { get; set; }

        public event Action<int> LaneRequested;
        public event Action JumpRequested;
        public event Action SlideRequested;

        private float swipeThreshold;
        private bool tracking;
        private Vector2 startPos;
        private float startTime;
        private bool swipeConsumed;
        private int activeFinger = -1;
        private float keyboardCooldown;

        private void Awake()
        {
            swipeThreshold = Mathf.Max(38f, Screen.height * 0.032f);
        }

        public void SetSwipeThreshold(float pixels)
        {
            swipeThreshold = Mathf.Max(10f, pixels);
        }

        private void Update()
        {
            if (!Enabled) return;
            keyboardCooldown -= Time.unscaledDeltaTime;

            HandleKeyboard();
            HandleTouch();
            if (!Application.isMobilePlatform) HandleMouse();
        }

        private void HandleKeyboard()
        {
            if (keyboardCooldown > 0f) return;
            if (Input.GetKeyDown(KeyCode.LeftArrow) || Input.GetKeyDown(KeyCode.A))
            {
                EmitLane(-1);
                keyboardCooldown = 0.12f;
            }
            else if (Input.GetKeyDown(KeyCode.RightArrow) || Input.GetKeyDown(KeyCode.D))
            {
                EmitLane(1);
                keyboardCooldown = 0.12f;
            }
            else if (Input.GetKeyDown(KeyCode.UpArrow) || Input.GetKeyDown(KeyCode.W) || Input.GetKeyDown(KeyCode.Space))
            {
                EmitJump();
                keyboardCooldown = 0.08f;
            }
            else if (Input.GetKeyDown(KeyCode.DownArrow) || Input.GetKeyDown(KeyCode.S) ||
                     Input.GetKeyDown(KeyCode.LeftControl) || Input.GetKeyDown(KeyCode.LeftShift))
            {
                EmitSlide();
                keyboardCooldown = 0.12f;
            }
        }

        private void HandleTouch()
        {
            if (Input.touchCount == 0)
            {
                if (tracking && activeFinger >= 0)
                {
                    tracking = false;
                    activeFinger = -1;
                }
                return;
            }

            for (int i = 0; i < Input.touchCount; i++)
            {
                Touch touch = Input.GetTouch(i);
                if (touch.phase == TouchPhase.Began)
                {
                    if (activeFinger != -1) continue;
                    if (EventSystem.current != null && EventSystem.current.IsPointerOverGameObject(touch.fingerId))
                        continue;
                    activeFinger = touch.fingerId;
                    tracking = true;
                    swipeConsumed = false;
                    startPos = touch.position;
                    startTime = Time.unscaledTime;
                }
                else if (tracking && touch.fingerId == activeFinger)
                {
                    if (touch.phase == TouchPhase.Moved || touch.phase == TouchPhase.Stationary)
                    {
                        if (!swipeConsumed)
                        {
                            Vector2 delta = touch.position - startPos;
                            if (delta.magnitude >= swipeThreshold &&
                                Time.unscaledTime - startTime <= 0.8f)
                            {
                                ResolveSwipe(delta);
                                swipeConsumed = true;
                                startPos = touch.position;
                                startTime = Time.unscaledTime;
                            }
                        }
                    }
                    else if (touch.phase == TouchPhase.Ended || touch.phase == TouchPhase.Canceled)
                    {
                        tracking = false;
                        activeFinger = -1;
                    }
                }
            }
        }

        private void HandleMouse()
        {
            if (Input.GetMouseButtonDown(0))
            {
                if (EventSystem.current != null && EventSystem.current.IsPointerOverGameObject())
                    return;
                tracking = true;
                swipeConsumed = false;
                startPos = Input.mousePosition;
                startTime = Time.unscaledTime;
            }
            else if (tracking && Input.GetMouseButton(0))
            {
                if (!swipeConsumed)
                {
                    Vector2 delta = (Vector2)Input.mousePosition - startPos;
                    if (delta.magnitude >= swipeThreshold && Time.unscaledTime - startTime <= 0.9f)
                    {
                        ResolveSwipe(delta);
                        swipeConsumed = true;
                        startPos = Input.mousePosition;
                        startTime = Time.unscaledTime;
                    }
                }
            }
            else if (tracking && Input.GetMouseButtonUp(0))
            {
                tracking = false;
            }
        }

        private void ResolveSwipe(Vector2 delta)
        {
            if (Mathf.Abs(delta.x) >= Mathf.Abs(delta.y) * 0.85f)
            {
                EmitLane(delta.x < 0f ? -1 : 1);
            }
            else
            {
                if (delta.y > 0f) EmitJump();
                else EmitSlide();
            }
        }

        private void EmitLane(int direction)
        {
            if (LaneRequested != null) LaneRequested(direction);
        }

        private void EmitJump()
        {
            if (JumpRequested != null) JumpRequested();
        }

        private void EmitSlide()
        {
            if (SlideRequested != null) SlideRequested();
        }

        public void PerformLane(int direction)
        {
            if (!Enabled) return;
            EmitLane(direction);
        }

        public void PerformJump()
        {
            if (!Enabled) return;
            EmitJump();
        }

        public void PerformSlide()
        {
            if (!Enabled) return;
            EmitSlide();
        }
    }
}
