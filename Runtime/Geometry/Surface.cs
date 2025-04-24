using System;
using System.Collections.Generic;
using System.Linq;
using Tactile.Core.Utility.MeshPart;
using Tactile.UI.Utility;
using UnityEngine;

namespace Tactile.UI.Geometry
{
    /// <summary>
    /// A surface allows you to create a rounded rectangular prism with specified corner sizes. You can specify both
    /// the radi and the depth of the corners, as well as the resolution of the corners by adjusting the number
    /// of corner vertices.
    /// </summary>
    [RequireComponent(typeof(RectTransform))]
    [RequireComponent(typeof(MeshFilter))]
    [RequireComponent(typeof(MeshRenderer))]
    [ExecuteInEditMode]
    public class Surface : MonoBehaviour
    {
        #region Serialized Variables

        [SerializeField] private CornerRadii cornerRadii;
        [SerializeField] private float surfaceDepth;
        [SerializeField] private float frontFaceDepth;
        [SerializeField] private float backFaceDepth;
        [SerializeField] private bool useNormalizedUV;
        [SerializeField] private int cornerVerticesCount = 3;
        [SerializeField] private float depthOffset = 0.001f;

        #endregion

        #region Properties

        public CornerRadii CornerRadii
        {
            get => cornerRadii;
            set => SetAndMarkDirty(out cornerRadii, value);
        }

        public int CornerVerticesCount
        {
            get => cornerVerticesCount;
            set => SetAndMarkDirty(out cornerVerticesCount, value);
        }

        public float SurfaceDepth
        {
            get => surfaceDepth;
            set => SetAndMarkDirty(out surfaceDepth, value);
        }

        public float FrontFaceDepth
        {
            get => frontFaceDepth;
            set => SetAndMarkDirty(out frontFaceDepth, value);
        }

        public float BackFaceDepth
        {
            get => backFaceDepth;
            set => SetAndMarkDirty(out backFaceDepth, value);
        }

        public bool UseNormalizedUV
        {
            get => useNormalizedUV;
            set => SetAndMarkDirty(out useNormalizedUV, value);
        }

        public float DepthOffset
        {
            get => depthOffset;
            set => SetAndMarkDirty(out depthOffset, value);
        }

        #endregion

        private RectTransform _rectTransform;
        private MeshFilter _meshFilter;
        private MeshRenderer _meshRenderer;
        private Mesh _surfaceMesh;
        private bool _isDirty;

        private static readonly Dictionary<int, MeshPart> CornerCache = new();

        #region Unity Events

        private void Awake()
        {
            _rectTransform = GetComponent<RectTransform>();
            _meshFilter = GetComponent<MeshFilter>();
            _meshRenderer = GetComponent<MeshRenderer>();
        }
        
        private void Start()
        {
            BuildSurface();
        }

        private void OnValidate()
        {
            MarkDirty();
        }

        private void LateUpdate()
        {
            if (Application.isPlaying && _isDirty)
            {
                BuildSurface();
            }
        }

        private void OnRectTransformDimensionsChange()
        {
            MarkDirty();
        }
        
        #if UNITY_EDITOR
        
        private void OnEnable()
        {
            if (!Application.isPlaying)
            {
                Camera.onPreCull += OnCameraPreCull;
            }
        }

        private void OnDisable()
        {
            if (!Application.isPlaying)
            {
                Camera.onPreCull -= OnCameraPreCull;    
            }
        }
        
        private void OnCameraPreCull(Camera drawingCamera)
        {
            if (Application.isPlaying)
            {
                return;
            }

            if (!_rectTransform)
            {
                _rectTransform = GetComponent<RectTransform>();
            }

            if (!_meshRenderer)
            {
                _meshRenderer = GetComponent<MeshRenderer>();
            }
            
            if (!_surfaceMesh)
            {
                BuildSurface();
            }
            
            Graphics.DrawMesh(_surfaceMesh, transform.localToWorldMatrix, _meshRenderer.sharedMaterial, gameObject.layer);
        }
        
        #endif

        #endregion

        private void SetAndMarkDirty<T>(out T var, T value)
        {
            var = value;
            MarkDirty();
        }

        private void MarkDirty()
        {
            if (Application.isPlaying)
            {
                _isDirty = true;
            }
            else
            {
                BuildSurface();
            }
        }

        #region Geometry

        private void BuildSurface()
        {
            var frontSurface = BuildFace(frontFaceDepth, true);
            var backSurface = BuildFace(backFaceDepth, false);
            var surface = MeshPart.Combine(frontSurface, backSurface);
            BuildSurfaceSides(surface);
            BuildSurfaceUV(surface);
            OffsetSurface(surface);

            _surfaceMesh = surface.CreateMesh();
            _surfaceMesh.name = "Surface Mesh";

            if (Application.isPlaying)
            {
                _meshFilter.mesh = _surfaceMesh;
            }
        }

        private void OffsetSurface(MeshPart surface)
        {
            surface.Translate(CalculateSurfaceOffset());
        }

        private void BuildSurfaceUV(MeshPart surface)
        {
            var uvScale = UseNormalizedUV
                ? new Vector2(1f / _rectTransform.rect.size.x, 1f / _rectTransform.rect.size.y)
                : Vector2.one;
            var uvOffset = 0.5f * (UseNormalizedUV ? Vector2.one : Vector2.zero);
            surface.UV.AddRange(surface.Positions.Select(vert => uvOffset + Vector2.Scale(uvScale, vert)));
        }

        private void BuildSurfaceSides(MeshPart surface)
        {
            StitchCornerEdges(surface,
                (4, 0, Direction.Z),
                (1, 5, Direction.Z),
                (2, 6, Direction.Z),
                (7, 3, Direction.Z));

            surface.AddQuads(
                GetMultiCornerQuad((2, 6, 0, 4), Direction.X),
                GetMultiCornerQuad((5, 7, 1, 3), Direction.X),
                GetMultiCornerQuad((4, 5, 0, 1), Direction.Y),
                GetMultiCornerQuad((2, 3, 6, 7), Direction.Y)
            );
        }

        private MeshPart BuildFace(float faceDepth, bool isFrontFace)
        {
            var corner = GetCorner(CornerVerticesCount);
            var topLeft = corner.Copy();
            var topRight = corner.Copy();
            var bottomLeft = corner.Copy();
            var bottomRight = corner.Copy();

            var rectSize = _rectTransform.rect.size;
            var maxSize = Mathf.Min(rectSize.x, rectSize.y) / 2f;
            var topLeftLength = Mathf.Min(maxSize, cornerRadii.topLeft);
            var topRightLength = Mathf.Min(maxSize, cornerRadii.topRight);
            var bottomLeftLength = Mathf.Min(maxSize, cornerRadii.bottomLeft);
            var bottomRightLength = Mathf.Min(maxSize, cornerRadii.bottomRight);
            faceDepth = Mathf.Min(surfaceDepth / 2f, faceDepth);
            var faceDepthScale = isFrontFace ? -faceDepth : faceDepth;

            // Create corners
            topLeft.Scale(new Vector3(-topLeftLength, topLeftLength, faceDepthScale));
            topLeft.Translate(CalculateCornerOffset(Corner.TopLeft, topLeftLength, isFrontFace, faceDepth));

            topRight.Scale(new Vector3(topRightLength, topRightLength, faceDepthScale));
            topRight.Translate(CalculateCornerOffset(Corner.TopRight, topRightLength, isFrontFace, faceDepth));
            topRight.FlipTriangleFaces();

            bottomLeft.Scale(new Vector3(-bottomLeftLength, -bottomLeftLength, faceDepthScale));
            bottomLeft.Translate(CalculateCornerOffset(Corner.BottomLeft, bottomLeftLength, isFrontFace, faceDepth));
            bottomLeft.FlipTriangleFaces();

            bottomRight.Scale(new Vector3(bottomRightLength, -bottomRightLength, faceDepthScale));
            bottomRight.Translate(CalculateCornerOffset(Corner.BottomRight, bottomRightLength, isFrontFace, faceDepth));

            // Add front face
            var face = MeshPart.Combine(topLeft, topRight, bottomLeft, bottomRight);
            face.AddQuad(GetMultiCornerQuad((0, 1, 2, 3), Direction.Z));

            StitchCornerEdges(
                face,
                (1, 0, Direction.X),
                (2, 3, Direction.X),
                (2, 0, Direction.Y),
                (1, 3, Direction.Y)
            );

            if (!isFrontFace)
            {
                face.FlipTriangleFaces();
            }

            return face;
        }

        private Vector3 CalculateSurfaceOffset()
        {
            var anchorOffset = _rectTransform.rect.size / 2f -
                               Vector2.Scale(_rectTransform.pivot, _rectTransform.rect.size);
            var offset = new Vector3(anchorOffset.x, anchorOffset.y, depthOffset);

            return offset;
        }

        private Vector3 CalculateCornerOffset(Corner corner, float cornerSize, bool isFrontFace, float depth)
        {
            var cornerReflection = corner switch
            {
                Corner.TopLeft => new Vector2(-1, 1),
                Corner.TopRight => new Vector2(1, 1),
                Corner.BottomLeft => new Vector2(-1, -1),
                Corner.BottomRight => new Vector2(1, -1),
                _ => throw new ArgumentOutOfRangeException(nameof(corner), corner, null)
            };

            var cornerVec = Vector2.Scale(_rectTransform.rect.size / 2f - cornerSize * Vector2.one, cornerReflection);
            var offsetDepth = isFrontFace ? depth : surfaceDepth - depth;
            var offsetVec = new Vector3(cornerVec.x, cornerVec.y, offsetDepth);

            return offsetVec;
        }

        private void StitchCornerEdges(MeshPart part,
            params (int fromCornerIndex, int toCornerIndex, Direction planeNormal)[] stitches)
        {
            foreach (var stitch in stitches)
            {
                StitchCornerEdges(part, stitch.fromCornerIndex, stitch.toCornerIndex, stitch.planeNormal);
            }
        }

        private void StitchCornerEdges(MeshPart part, int fromCornerIndex, int toCornerIndex, Direction planeNormal)
        {
            var (firstEdge, secondEdge) = planeNormal switch
            {
                Direction.X => (Direction.Z, Direction.Y),
                Direction.Y => (Direction.X, Direction.Z),
                Direction.Z => (Direction.Y, Direction.X),
                _ => throw new ArgumentOutOfRangeException(nameof(planeNormal), planeNormal, null)
            };

            var fromFirstCornerStart = GetCornerFeatureIndex(fromCornerIndex, CornerFeature.CornerStart, firstEdge);
            var toFirstCornerStart = GetCornerFeatureIndex(toCornerIndex, CornerFeature.CornerStart, firstEdge);
            var fromSecondCornerStart = GetCornerFeatureIndex(fromCornerIndex, CornerFeature.CornerStart, secondEdge);
            var toSecondCornerStart = GetCornerFeatureIndex(toCornerIndex, CornerFeature.CornerStart, secondEdge);

            // Stitch unique edges
            for (var i = 0; i < cornerVerticesCount - 1; i++)
            {
                var tl1 = fromFirstCornerStart + i;
                var tr1 = toFirstCornerStart + i;
                var bl1 = fromFirstCornerStart + i + 1;
                var br1 = toFirstCornerStart + i + 1;

                var tl2 = toSecondCornerStart + i * cornerVerticesCount;
                var tr2 = fromSecondCornerStart + i * cornerVerticesCount;
                var bl2 = toSecondCornerStart + (i + 1) * cornerVerticesCount;
                var br2 = fromSecondCornerStart + (i + 1) * cornerVerticesCount;

                part.AddQuads(
                    (tl1, tr1, bl1, br1),
                    (tl2, tr2, bl2, br2));
            }

            // Stitch seams
            var lastUniqueEdgeStart1 = fromFirstCornerStart + cornerVerticesCount - 1;
            var lastUniqueEdgeEnd1 = toFirstCornerStart + cornerVerticesCount - 1;
            var lastUniqueEdgeStart2 = fromSecondCornerStart + cornerVerticesCount * (cornerVerticesCount - 1);
            var lastUniqueEdgeEnd2 = toSecondCornerStart + cornerVerticesCount * (cornerVerticesCount - 1);
            var seamStart = GetCornerFeatureIndex(fromCornerIndex, CornerFeature.StitchEdgeStart, secondEdge);
            var seamEnd = GetCornerFeatureIndex(toCornerIndex, CornerFeature.StitchEdgeStart, secondEdge);

            part.AddQuads(
                (lastUniqueEdgeStart1, lastUniqueEdgeEnd1, seamStart, seamEnd),
                (seamStart, seamEnd, lastUniqueEdgeStart2, lastUniqueEdgeEnd2));
        }

        private (int topLeft, int topRight, int bottomLeft, int bottomRight) GetMultiCornerQuad(
            (int topLeft, int topRight, int bottomLeft, int bottomRight) cornerIndices, Direction direction)
        {
            return (
                GetCornerFeatureIndex(cornerIndices.topLeft, CornerFeature.CornerStart, direction),
                GetCornerFeatureIndex(cornerIndices.topRight, CornerFeature.CornerStart, direction),
                GetCornerFeatureIndex(cornerIndices.bottomLeft, CornerFeature.CornerStart, direction),
                GetCornerFeatureIndex(cornerIndices.bottomRight, CornerFeature.CornerStart, direction));
        }

        private int GetCornerFeatureIndex(int cornerIndex, CornerFeature feature, Direction direction)
        {
            var vertices = CalculateCornerVerticesCount(cornerVerticesCount);

            return (cornerIndex * vertices.TotalVertices) + (feature, direction) switch
            {
                (CornerFeature.CornerStart, Direction.X) => 0,
                (CornerFeature.CornerStart, Direction.Y) => vertices.VerticesPerFace,
                (CornerFeature.CornerStart, Direction.Z) => 2 * vertices.VerticesPerFace,
                (CornerFeature.StitchEdgeStart, Direction.X) => vertices.TotalFaceVertices,
                (CornerFeature.StitchEdgeStart, Direction.Y) => vertices.TotalFaceVertices +
                                                                vertices.VerticesPerStichEdge,
                (CornerFeature.StitchEdgeStart, Direction.Z) => vertices.TotalFaceVertices +
                                                                2 * vertices.VerticesPerStichEdge,
                _ => throw new ArgumentOutOfRangeException()
            };
        }

        private static (int VerticesPerFace, int TotalFaceVertices, int VerticesPerStichEdge, int TotalVertices)
            CalculateCornerVerticesCount(int cornerSubdivisions)
        {
            var verticesPerFace = cornerSubdivisions * cornerSubdivisions;
            var totalFaceVertices = 3 * verticesPerFace;
            var verticesPerStitchEdge = cornerSubdivisions;
            var totalVertices = totalFaceVertices + 3 * verticesPerStitchEdge + 1;
            return (verticesPerFace, totalFaceVertices, verticesPerStitchEdge, totalVertices);
        }

        private static MeshPart GetCorner(int cornerSubdivisions)
        {
            if (CornerCache.TryGetValue(cornerSubdivisions, out var cachedCorner))
            {
                return cachedCorner;
            }

            var builtCorner = BuildCorner(cornerSubdivisions);
            CornerCache[cornerSubdivisions] = builtCorner;

            return builtCorner;
        }

        /// <summary>
        /// Builds a corner of the surface.
        /// </summary>
        /// <param name="cornerSubdivisions">The number of times to subdivide the corner</param>
        /// <returns>A tuple of the corner's vertices and triangles</returns>
        private static MeshPart BuildCorner(int cornerSubdivisions)
        {
            var cornerPart = new MeshPart();

            var (numFaceVertices, totalFaceVertices, numEdgeVertices, totalVertices) =
                CalculateCornerVerticesCount(cornerSubdivisions);

            // Create the corner vertices that are unique to each of the three corner "faces"
            var oneOverVertices = 1f / cornerSubdivisions;

            cornerPart.Positions.Capacity = totalVertices;
            cornerPart.Positions.AddRange(Enumerable.Repeat(Vector3.zero, totalVertices));

            var cornerVertices = cornerPart.Positions;

            // Create the vertices that are unique to each "face" of the corner.
            for (var y = 0; y < cornerSubdivisions; y++)
            {
                for (var x = 0; x < cornerSubdivisions; x++)
                {
                    var i = y * cornerSubdivisions + x;

                    // We create a curved corner by placing vertices along the faces of a cube, then normalizing
                    // these vectors so that they conform to the surface of a sphere.
                    var edgeVertX = new Vector3(1f, y * oneOverVertices, x * oneOverVertices).normalized;
                    var edgeVertY = new Vector3(x * oneOverVertices, 1f, y * oneOverVertices).normalized;
                    var edgeVertZ = new Vector3(y * oneOverVertices, x * oneOverVertices, 1f).normalized;

                    cornerVertices[i] = edgeVertX;
                    cornerVertices[i + numFaceVertices] = edgeVertY;
                    cornerVertices[i + 2 * numFaceVertices] = edgeVertZ;
                }
            }

            // Create the edges that will "stitch" the different corner faces together into a larger corner.
            for (var i = 0; i < cornerSubdivisions; i++)
            {
                var edgeVertXY = new Vector3(1f, 1f, i * oneOverVertices).normalized;
                var edgeVertYZ = new Vector3(i * oneOverVertices, 1f, 1f).normalized;
                var edgeVertXZ = new Vector3(1f, i * oneOverVertices, 1f).normalized;

                cornerVertices[i + totalFaceVertices] = edgeVertXY;
                cornerVertices[i + totalFaceVertices + numEdgeVertices] = edgeVertYZ;
                cornerVertices[i + totalFaceVertices + 2 * numEdgeVertices] = edgeVertXZ;
            }

            // Add center vert
            cornerVertices[^1] = Vector3.one.normalized;

            // Create triangles
            var triangles = cornerPart.Triangles;
            triangles.Capacity = 6 * cornerSubdivisions;
            var gridQuads = cornerSubdivisions - 1;
            var centerVert = totalVertices - 1;

            for (var z = 0; z < 3; z++)
            {
                for (var y = 0; y < gridQuads + 1; y++)
                {
                    for (var x = 0; x < gridQuads + 1; x++)
                    {
                        var offset = z * (int)Mathf.Pow(gridQuads + 1, 2);
                        var i = offset + y * (gridQuads + 1) + x;
                        var i2 = offset + (gridQuads - y) * (gridQuads + 1) + (gridQuads - x);
                        var edgeVertX = totalFaceVertices +
                                        ((z + 2) * cornerSubdivisions + y) % (cornerSubdivisions * 3);
                        var edgeVertY = totalFaceVertices + (z * cornerSubdivisions + x) % (cornerSubdivisions * 3);

                        // Center Vert
                        if (x == gridQuads && y == gridQuads)
                        {
                            cornerPart.AddTriangles(
                                (i, edgeVertY, edgeVertX),
                                (i, edgeVertY, edgeVertX),
                                (edgeVertY, centerVert, edgeVertX));
                        }
                        // X Edge
                        else if (x == gridQuads)
                        {
                            cornerPart.AddTriangles(
                                (i, i + cornerSubdivisions, edgeVertX),
                                (edgeVertX + 1, edgeVertX, i + cornerSubdivisions));
                        }
                        // Y Edge
                        else if (y == gridQuads)
                        {
                            cornerPart.AddTriangles(
                                (i, edgeVertY, i + 1),
                                (edgeVertY + 1, i + 1, edgeVertY));
                        }
                        else
                        {
                            cornerPart.AddTriangles(
                                (i, i + gridQuads + 1, i + 1),
                                (i2, i2 - (gridQuads + 1), i2 - 1));
                        }
                    }
                }
            }

            return cornerPart;
        }

        #endregion

        #region Enums

        private enum Corner
        {
            TopLeft,
            TopRight,
            BottomLeft,
            BottomRight
        }

        private enum Direction
        {
            X,
            Y,
            Z
        }

        private enum CornerFeature
        {
            CornerStart,
            StitchEdgeStart
        }

        #endregion
    }
}