using UnityEngine;
using System.Collections.Generic;

namespace QubicNS
{
    public class MeshQuadrantSplitter
    {
        public Bounds[] sideBounds; // Stores Bounds for each side
        public int[] quadrantVertexCounts; // Stores the number of vertices in each quadrant
        public string msg;
        public Bounds totalBounds;
        public float SideWidth = 0.03f;


        [ContextMenu("Split Mesh into Quadrants")]
        public bool SplitMeshIntoQuadrants(GameObject obj)
        {
            msg = "";

            // Get all meshes inside the GameObject and its children
            MeshFilter[] meshFilters = obj.GetComponentsInChildren<MeshFilter>();
            if (meshFilters.Length == 0)
            {
                msg = "No meshes found in the GameObject or its children.";
                return false;
            }

            // Collect all vertices from all meshes
            List<Vector3> allVertices = new List<Vector3>();
            foreach (var meshFilter in meshFilters)
            {
                var mr = meshFilter.GetComponent<MeshRenderer>();
                if (mr != null && !mr.enabled)
                    continue;
                Mesh mesh = meshFilter.sharedMesh;
                if (mesh == null) continue;

#if UNITY_EDITOR
                // Check if the mesh is readable
                if (!mesh.isReadable)
                {
                    if (!mesh.MakeMeshReadable())
                    {
                        msg = $"Mesh '{mesh.name}' is not readable. Skipping.";
                        continue;
                    }
                }
#endif

                // Transform vertices to world coordinates
                Vector3[] vertices = mesh.vertices;
                for (int i = 0; i < vertices.Length; i++)
                {
                    vertices[i] = meshFilter.transform.TransformPoint(vertices[i]);
                }
                allVertices.AddRange(vertices);
            }

            // Check if any vertices were collected
            if (allVertices.Count == 0)
            {
                msg = "No readable vertices found.";
                return false;
            }

            // Calculate the total Bounds for all vertices
            totalBounds = new Bounds(allVertices[0], Vector3.zero);
            foreach (var vertex in allVertices)
            {
                totalBounds.Encapsulate(vertex);
            }

            // Get the center of the Bounds
            Vector3 center = totalBounds.center;
            // decrease size of total bounds
            var leftBound = GetSideBounds(totalBounds, Side.Left, SideWidth);
            var frontBounds = GetSideBounds(totalBounds, Side.Front, SideWidth);
            var rightBound = GetSideBounds(totalBounds, Side.Right, SideWidth);
            var backBounds = GetSideBounds(totalBounds, Side.Back, SideWidth);

            // Split vertices into 4 quadrants in the XZ plane
            List<Vector3>[] quadrants = new List<Vector3>[4];
            for (int i = 0; i < 4; i++)
            {
                quadrants[i] = new List<Vector3>();
            }

            // Initialize vertex counts for each quadrant
            quadrantVertexCounts = new int[4];

            // Distribute vertices into quadrants based on their position relative to the center
            foreach (var vertex in allVertices)
            {
                if (rightBound.Contains(vertex))
                    quadrants[(int)Side.Right].Add(vertex); 
                if (leftBound.Contains(vertex))
                    quadrants[(int)Side.Left].Add(vertex); 
                if (backBounds.Contains(vertex))
                    quadrants[(int)Side.Back].Add(vertex); 
                if (frontBounds.Contains(vertex))
                    quadrants[(int)Side.Front].Add(vertex); 

                if (vertex.x >= center.x && vertex.z >= center.z)// First quadrant (X+, Z+)
                    quadrantVertexCounts[0]++;
                else if (vertex.x < center.x && vertex.z >= center.z)// Second quadrant (X-, Z+)
                    quadrantVertexCounts[1]++;
                else if (vertex.x < center.x && vertex.z < center.z)// Third quadrant (X-, Z-)
                    quadrantVertexCounts[2]++;
                else// Fourth quadrant (X+, Z-)
                    quadrantVertexCounts[3]++;
            }

            // Calculate Bounds for each quadrant
            sideBounds = new Bounds[4];
            for (int i = 0; i < 4; i++)
            {
                if (quadrants[i].Count > 0)
                {
                    sideBounds[i] = new Bounds(quadrants[i][0], Vector3.zero);
                    foreach (var vertex in quadrants[i])
                        sideBounds[i].Encapsulate(vertex);

                    //Debug.Log($"Side {(Side)i} Bounds: {sideBounds[i]}");
                }

                //Debug.Log($"Quadrant {i + 1} Vertex Count: {quadrantVertexCounts[i]}");
            }

            return true;
        }

        public enum Side
        {
            Left,
            Right,
            Front,
            Back
        }

        public static Bounds GetSideBounds(Bounds originalBounds, Side side, float width)
        {
            // Ensure the width is not larger than the original Bounds size
            width = Mathf.Min(width, originalBounds.size.x, originalBounds.size.z);

            // Calculate the center and size of the new Bounds
            Vector3 center = originalBounds.center;
            Vector3 size = originalBounds.size;

            switch (side)
            {
                case Side.Left:
                    center.x = originalBounds.min.x + width / 2; // Move center to the left edge
                    size.x = width; // Set the width of the new Bounds
                    break;

                case Side.Right:
                    center.x = originalBounds.max.x - width / 2; // Move center to the right edge
                    size.x = width; // Set the width of the new Bounds
                    break;

                case Side.Front:
                    center.z = originalBounds.max.z - width / 2; // Move center to the front edge
                    size.z = width; // Set the width of the new Bounds
                    break;

                case Side.Back:
                    center.z = originalBounds.min.z + width / 2; // Move center to the back edge
                    size.z = width; // Set the width of the new Bounds
                    break;
            }

            // Create and return the new Bounds
            return new Bounds(center, size);
        }

        public void OnDrawGizmos()
        {
            if (sideBounds == null) return;

            // Draw Bounds for each quadrant with different colors
            Gizmos.color = Color.red;
            DrawBounds(sideBounds[0]); // First quadrant (X+, Z+)

            Gizmos.color = Color.green;
            DrawBounds(sideBounds[1]); // Second quadrant (X-, Z+)

            Gizmos.color = Color.blue;
            DrawBounds(sideBounds[2]); // Third quadrant (X-, Z-)

            Gizmos.color = Color.yellow;
            DrawBounds(sideBounds[3]); // Fourth quadrant (X+, Z-)
        }

        private void DrawBounds(Bounds bounds)
        {
            if (bounds.size == Vector3.zero) return;

            // Draw the Bounds using Gizmos
            Gizmos.DrawWireCube(bounds.center, bounds.size);
        }
    }
}
