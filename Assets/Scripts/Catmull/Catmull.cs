using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Collections.Generic;
using UnityEngine;
namespace Catmull {
    public struct Vertex {
        public int posIndex;
        public Vector3 position;
    }

    public enum VertexContent {
        none    = 0x00,
        pos     = 0x01,
        full    = 0x7F,
    }

    public static class VertexContentMethods {
        public static bool HasPosition(this VertexContent c) { return (c & VertexContent.pos)     != 0; }
    }

    public struct Submesh {
        public int[][] faces; // [face index] => [face vertex indices]
    }

    public struct Face {
        public int submesh;
        public int index; // face index
    }
    
    public struct Edge {
        public Face face;
        public int index;
        public static Edge Invalid = new Edge { index = -1 };
        public bool IsValid() { return index != -1; }
    }

    public enum EdgeType {
        boundary = -1,
        back     = -2,
        solid    = 0, 
        seam0    = 1, 
        seam1    = 2,
    }

    public static class CatmullClark {
        private static readonly int keyBitsSubmeshes    = 6;
        
        private static System.UInt32 GetFacePointKey(int si, int fi) {
            System.UInt32 key = 0;
                                      key ^= (System.UInt32)fi;
            key <<= keyBitsSubmeshes; key ^= (System.UInt32)si;
            key <<= 1;                key |= 1;
            return key;
        }

        private static System.UInt32 GetEdgePointKey(int si, int fi, int ei) {
            System.UInt32 key = 0;
            key ^= (System.UInt32)fi;
            key <<= keyBitsSubmeshes; 
            key ^= (System.UInt32)si;
            key <<= 2;                
            key ^= (System.UInt32)ei;
            key <<= 1;                
            key |= 0;
            return key;
        }

        private static System.UInt32 GetFacePointKey(Face f) {
            return GetFacePointKey(f.submesh, f.index);
        }

        private static System.UInt32 GetEdgePointKey(Edge e) {
            return GetEdgePointKey(e.face.submesh, e.face.index, e.index);
        }

        private class MeshVertices {
            public MeshX mesh;
            public Dictionary<System.UInt32, int> vertexIndices = new Dictionary<System.UInt32, int>();

            public struct VertexKeyPair {
                public Vertex vertex;
                public System.UInt32 key;
            }

            public void AddVertices(Vertex position, params VertexKeyPair[] vertexPairs) {
                int pi = mesh.AddPosition(position);

                foreach (VertexKeyPair p in vertexPairs) {
                    Vertex v = p.vertex;
                    v.posIndex = pi;
                    int vi = mesh.AddVertex(v);
                    vertexIndices[p.key] = vi;
                }
            }

            public Vertex GetVertex(System.UInt32 key, VertexContent mask = VertexContent.full) {
                return mesh.GetVertex(vertexIndices[key], mask);
            }
        }

        private static MeshVertices.VertexKeyPair Pair(Vertex v, System.UInt32 k) {
            return new MeshVertices.VertexKeyPair { vertex = v, key = k };
        }

        private static VertexContent maskPosition = VertexContent.pos;

        public static MeshX Subdivide(MeshX mesh) 
        {
            if (!mesh.helpersInited) mesh.InitHelpers();

            MeshX newMesh = new MeshX {
                name = mesh.name + "/s",
                content = mesh.content,
            };
            newMesh.StartBuilding();

            MeshVertices newVertices = new MeshVertices { mesh = newMesh };
            Dictionary<System.UInt32, Vertex> edgeMidPositions = new Dictionary<System.UInt32, Vertex>();
            
            for (int pi = 0; pi < mesh.positions.Count; ++pi) {
                newMesh.AddPosition(default(Vertex));
            }
            
            for (int vi = 0; vi < mesh.vertexCount; ++vi) {
                Vertex v = mesh.GetVertex(vi);
                newMesh.AddVertex(v);
            }
            
            //face points
            foreach (Face f in mesh.IterAllFaces()) {
                Vertex[] vs = mesh.GetVertices(mesh.GetFaceVertexIndices(f));
                Vertex v = mesh.Average(vs);
                System.UInt32 keyF = GetFacePointKey(f);
                newVertices.AddVertices(v, Pair(v, keyF));
            }
            
            //edge points
            foreach (Edge e in mesh.IterAllEdges()) {
                Edge n = mesh.GetNeighbor(e);
                Vertex midE = mesh.Average(
                    mesh.GetVertices(mesh.GetEdgeVertexIndices(e))
                );

                System.UInt32 keyE = GetEdgePointKey(e);
                edgeMidPositions[keyE] = midE;
                
                Vertex v = mesh.Average(
                    new[] {
                        midE,
                        newVertices.GetVertex(GetFacePointKey(e.face)),
                        newVertices.GetVertex(GetFacePointKey(n.face)),
                    },
                    weights: new[] { 2f, 1f, 1f }
                );
                newVertices.AddVertices(v, Pair(v, keyE));
            }
            
            // move control-points
            for (int pi = 0; pi < mesh.positions.Count; ++pi) {
                var edges = new List<Edge>();
                var front = new List<Edge>();

                foreach (int vi in mesh.positionVertices[pi]) {
                    foreach (Edge e in mesh.vertexEdges[vi]) {
                        edges.Add(e);

                        foreach (Edge edge in new[] { e, mesh.GetNextInFace(e, -1) }) {
                            EdgeType type = mesh.GetEdgeType(edge);
                            if (type != EdgeType.back) {
                                front.Add(edge);
                            }
                        }
                    }
                }

                Vertex controlPoint;
                Vertex[] ms = new Vertex[front.Count];

                for (int e = 0; e < front.Count; ++e) {
                    ms[e] = edgeMidPositions[GetEdgePointKey(front[e])];
                }

                Vertex edgeMidAverage = mesh.Average(ms, maskPosition);
                Vertex[] fs = new Vertex[edges.Count];

                for (int e = 0; e < edges.Count; ++e) {
                    fs[e] = newVertices.GetVertex(GetFacePointKey(edges[e].face), maskPosition);
                }

                Vertex faceAverage = mesh.Average(fs, maskPosition);
                controlPoint = mesh.Average(
                    new[] {
                        faceAverage,
                        edgeMidAverage,
                        mesh.GetPosition(pi)
                    },
                    maskPosition,
                    new[] { 1f, 2f, edges.Count - 3f }
                );
                newMesh.SetPosition(pi, controlPoint);
            }
            
            //face creation
            newMesh.submeshes = new Submesh[mesh.submeshes.Length];

            for (int si = 0; si < mesh.submeshes.Length; ++si) {
                int[][] faces = mesh.submeshes[si].faces;
                int faceCount = 0;

                foreach (int[] face in faces)
                {
                    faceCount += face.Length;
                }

                newMesh.submeshes[si].faces = new int[faceCount][];
                int faceIndex = 0;

                for (int fi = 0; fi < faces.Length; ++fi) {
                    int[] fis = faces[fi];
                    int edgeCount = fis.Length;
                    Face f = new Face { submesh = si, index = fi };
                    int ci = newVertices.vertexIndices[GetFacePointKey(f)];
                    int[] eis = new int[edgeCount];

                    for (int i = 0; i < edgeCount; ++i) {
                        Edge e = new Edge { face = f, index = i };
                        eis[i] = newVertices.vertexIndices[GetEdgePointKey(e)];
                    }
                    
                    for (int i = 0; i < edgeCount; ++i) {
                        int[] q = new int[4];
                        int s = edgeCount == 4 ? i : 0;
                        q[(0 + s) % 4] = fis[i];
                        q[(1 + s) % 4] = eis[i];
                        q[(2 + s) % 4] = ci;
                        q[(3 + s) % 4] = eis[(i - 1 + edgeCount) % edgeCount];
                        newMesh.submeshes[si].faces[faceIndex++] = q;
                    }
                }
            }

            return newMesh;
        }

        public static void StartSubdivision(GameObject obj, int iterations) {
            MeshFilter mf = obj.GetComponent<MeshFilter>();
            Mesh mesh = mf.sharedMesh;
            MeshX mX = new MeshX(mesh);

            for (int i = 0; i < iterations; ++i) {
                mX = Subdivide(mX);
            }

            mesh = mX.ConvertToMesh();
            mf.sharedMesh = mesh;
        }
    }
}

