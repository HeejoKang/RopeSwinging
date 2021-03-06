﻿using System.Collections.Generic;
using UnityEngine;
using Wokarol.Utils;

namespace Wokarol
{
    public class PhysicsLine
    {
        public delegate void AnchorsChanged(Anchor currentAnchor);

        public float EdgeOffset { get; set; } = 0.05f;
        public LayerMask GroundMask { get; set; }
        public bool DrawDebug { get; set; }

        /// <summary>
        /// Called when new anchor is added
        /// </summary>
        public event AnchorsChanged AnchorAdded;
        /// <summary>
        /// Called when current anchor is removed
        /// </summary>
        public event AnchorsChanged AnchorRemoved;

        Vector2 previousPosition;

        // Anchor memory
        Stack<Anchor> previousAnchors = new Stack<Anchor>();
        Anchor currentAnchor;

        public PhysicsLine(Vector2 startPosition, Vector2 lineOrigin, float edgeOffset, LayerMask groundMask)
        {
            // Sets variables
            EdgeOffset = edgeOffset;
            GroundMask = groundMask;

            // Sets previous position and first anchor
            previousPosition = startPosition;
            currentAnchor = new Anchor(lineOrigin, Vector2.zero, true);
        }

        public void Tick(Vector2 lineEndPosition)
        {
            RaycastHit2D hit;

            // Loops until line of sight to player is restored
            do {
                // Breaks if end position is inside a collider
                if (Physics2D.OverlapCircle(lineEndPosition, 0.001f, GroundMask) != null) break;

                Vector2 originPos = currentAnchor.Position;

                // Raycasts from last anchor to current player position
                hit = Physics2D.Raycast(originPos, (lineEndPosition - originPos).normalized, Vector2.Distance(originPos, lineEndPosition), GroundMask);

                // Checks if hitted something and ands anchor in that case
                if (hit.transform != null) {
                    // Gets previous and current direction
                    Vector2 previousDirection = previousPosition - originPos;
                    Vector2 direction = lineEndPosition - originPos;

                    // Ckecks if hit was clockwise or counter clockwise
                    float angleBetweenRays = Vector2.SignedAngle(previousDirection, direction);
                    bool clockwise = angleBetweenRays < 0;

                    // Gets all corners for given collider and find correct one
                    var corners = ColliderUtils.FindCorners(originPos, hit.collider);
                    var cornerIndex = VectorUtils.FindFirstPointInCircularOrder(previousDirection, originPos, corners, clockwise);
                    var corner = corners[cornerIndex];
                    Vector2 normal = ColliderUtils.FindNormal(corners[cornerIndex], cornerIndex, hit.collider);

                    // Ads offset
                    corner += normal * EdgeOffset;

                    // Add new anchor to memory
                    previousAnchors.Push(currentAnchor);
                    currentAnchor = new Anchor(corner, normal, clockwise);
                    AnchorAdded?.Invoke(currentAnchor);
                }
            } while (hit.transform != null);

            // Checks if last anchor should be deleted
            if (previousAnchors.Count > 0) {
                Anchor previousAnchor = previousAnchors.Peek();
                Vector2 lastToCurrentAnchorPos = currentAnchor.Position - previousAnchor.Position;
                float angle = Vector2.SignedAngle(lastToCurrentAnchorPos, lineEndPosition - currentAnchor.Position);
                if (currentAnchor.Clockwise == angle > 0) {
                    // Removes anchor from memory
                    currentAnchor = previousAnchors.Pop();
                    AnchorRemoved?.Invoke(currentAnchor);
                }
            }


            if (DrawDebug) {
                // Debug Drawing
                var anchorArray = previousAnchors.ToArray();
                for (int i = 0; i < previousAnchors.Count - 1; i++) {
                    Debug.DrawLine(anchorArray[i].Position, anchorArray[i + 1].Position, Color.red);
                }
                if (previousAnchors.Count > 0) {
                    Debug.DrawLine(previousAnchors.Peek().Position, currentAnchor.Position, Color.green);
                }
                Debug.DrawLine(currentAnchor.Position, lineEndPosition, Color.cyan);
                // ----------- 
            }

            // Updates previous position
            previousPosition = lineEndPosition;
        }

        /// <summary>
        /// Stores info about anchor, position and direction
        /// </summary>
        public struct Anchor
        {
            public readonly Vector2 Position;
            public readonly Vector2 Normal;
            public readonly bool Clockwise;

            public Anchor(Vector2 position, Vector2 normal, bool clockwise)
            {
                Position = position;
                Normal = normal;
                Clockwise = clockwise;
            }
        }
    }
}