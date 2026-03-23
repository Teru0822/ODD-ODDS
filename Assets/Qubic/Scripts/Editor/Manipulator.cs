using System;
using UnityEditor;
using UnityEngine;

namespace QubicNS
{
    public class Manipulator
    {
        public Vector3 Position { get; private set; }
        public Vector3 PressStartPosition { get; private set; }
        public Vector3 ReleasePosition { get; private set; }

        public bool IsDragging => isDragging;
        public bool IsHovered => isHovered;

        public Handles.CapFunction CapFunction { get; set; } = Handles.SphereHandleCap;
        public Color Color { get; set; } = Color.green;

        private bool isDragging;
        private bool isHovered;
        private int handleID;
        private Vector3 dragOffset;
        private Vector3 startPos;

        public bool restrictXZplane;
        public bool restrictY;
        public float Size = 0.2f;
        public float offsetY = 0;
        public float HandleSize;
        public Action OnDragStart;

        bool lastHovered;

        public void SetPosition(Vector3 pos)
        {
            startPos = pos;
            Position = pos;
            PressStartPosition = pos;
            ReleasePosition = pos;
            HandleSize = HandleUtility.GetHandleSize(Position);
        }

        public void Update()
        {
            Event evt = Event.current;
            handleID = GUIUtility.GetControlID(FocusType.Passive);
            float size = HandleUtility.GetHandleSize(Position) * Size;

            bool hoverStateChanged = (lastHovered != isHovered);
            lastHovered = isHovered;

            if (hoverStateChanged)
            {
                SceneView.RepaintAll();
            }

            if (isDragging)
                GUIUtility.hotControl = handleID;

            switch (evt.type)
            {
                case EventType.Layout:
                    if (CapFunction == Handles.ArrowHandleCap)
                        HandleUtility.AddControl(handleID, HandleUtility.DistanceToCircle(Position + Vector3.up * size * 0.8f, size / 5));
                    else
                        HandleUtility.AddControl(handleID, HandleUtility.DistanceToCircle(Position, size));
                    isHovered = HandleUtility.nearestControl == handleID;
                    break;

                case EventType.MouseDown:
                    if (HandleUtility.nearestControl == handleID && evt.button == 0)
                    {
                        GUIUtility.hotControl = handleID;
                        PressStartPosition = Position;
                        dragOffset = Position - GetMouseWorldPosition(evt.mousePosition);
                        isDragging = true;
                        OnDragStart?.Invoke();
                        evt.Use();
                    }
                    break;

                case EventType.MouseDrag:
                    if (GUIUtility.hotControl == handleID)
                    {
                        Vector3 newPos = GetMouseWorldPosition(evt.mousePosition) + dragOffset;

                        if (restrictXZplane)
                            newPos.y = startPos.y;

                        if (restrictY)
                        {
                            newPos.x = startPos.x;
                            newPos.z = startPos.z;
                        }

                        Position = newPos;
                        evt.Use();
                    }
                    break;

                case EventType.MouseUp:
                    if (GUIUtility.hotControl == handleID)
                    {
                        GUIUtility.hotControl = 0;
                        isDragging = false;
                        ReleasePosition = Position;
                        evt.Use();
                    }
                    break;
            }

            Handles.color = isHovered ? Color.red : Color;
            CapFunction(handleID, Position, Quaternion.Euler(-90, 0, 0), size, EventType.Repaint);
        }

        private Vector3 GetMouseWorldPosition(Vector2 guiPosition)
        {
            Ray ray = HandleUtility.GUIPointToWorldRay(guiPosition);
            Plane plane = new Plane(ray.direction, startPos);

            if (restrictXZplane)
                plane = new Plane(Vector3.up, startPos);
            else if (restrictY)
                plane = new Plane(ray.direction.XZ(), startPos); // you can adjust this

            if (plane.Raycast(ray, out float dist))
                return ray.GetPoint(dist);

            return startPos;
        }
    }
}