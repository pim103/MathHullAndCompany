using System.Collections.Generic;
using UnityEngine;

namespace Catmull
{
    public class MeshX
    {
        public IList<Vector3> positions;
        public IList<int> posIndices;
        public VertexContent content = VertexContent.none;
        public string name;
        public Submesh[] submeshes;
        public int vertexCount { get { return posIndices == null ? -1 : posIndices.Count; } }

        public bool helpersInited { get { return positionVertices != null; } }
        public  int[][] positionVertices;
        public  Edge[][] vertexEdges;
        private Edge[][][] neighbors;

        public MeshX() {}

        public void StartBuilding()
        {
            if (content.HasPosition()) positions = new List<Vector3>();
            posIndices = new List<int>();
        }

        public int AddPosition(Vertex v) {
            positions.Add(v.position);
            return positions.Count - 1;
        }

        public int AddVertex(Vertex v, bool addPosition = false) {
            int pi = addPosition ? AddPosition(v) : v.posIndex;
            posIndices.Add(pi);
            return posIndices.Count - 1;
        }

        // positions & vertices
        public void SetPosition(int pi, Vertex v) {
            positions[pi] = v.position;
        }

        public Vertex GetPosition(int pi) {
            Vertex v = new Vertex();
            v.position = positions[pi];
            return v;
        }

        public Vertex GetVertex(int vi, VertexContent mask = VertexContent.full) {
            VertexContent c = content & mask;
            int pi = posIndices[vi];
            Vertex v = new Vertex();
            v.posIndex = pi;
            if (c.HasPosition()) v.position = positions[pi];
            return v;
        }

        public Vertex[] GetVertices(int[] indices) {
            Vertex[] vs = new Vertex[indices.Length];
            for (int i = 0; i < indices.Length; ++i) {
                vs[i] = GetVertex(indices[i]);
            }
            return vs;
        }

        private void SetNeighbor(Edge e, Edge n) {
            neighbors[e.face.submesh][e.face.index][e.index] = n;
        }
        public Edge GetNeighbor(Edge e) {
            return neighbors[e.face.submesh][e.face.index][e.index];
        }

        public void InitHelpers() {
            vertexEdges = new Edge[vertexCount][];
            for (int v = 0; v < vertexCount; ++v) {
                var es = new List<Edge>();
                foreach (Face face in IterAllFaces()) {
                    int e = GetIndexInFace(GetFaceVertexIndices(face), v);
                    if (e != -1) {
                        es.Add(new Edge { face = face, index = e });
                    }
                }
                vertexEdges[v] = es.ToArray();
            }
            positionVertices = new int[positions.Count][];
            for (int p = 0; p < positions.Count; ++p) {
                var vs = new List<int>();
                for (int v = 0; v < vertexCount; ++v) {
                    if (posIndices[v] == p) {
                        vs.Add(v);
                    }
                }
                positionVertices[p] = vs.ToArray();
            }
            neighbors = new Edge[submeshes.Length][][];
            for (int s = 0; s < submeshes.Length; ++s) {
                int[][] faces = submeshes[s].faces;
                neighbors[s] = new Edge[faces.Length][];
                for (int f = 0; f < faces.Length; ++f) {
                    int[] es = faces[f];
                    neighbors[s][f] = new Edge[es.Length];
                    for (int e = 0; e < es.Length; ++e) {
                        neighbors[s][f][e] = Edge.Invalid;
                    }
                }
            }
            foreach (Edge e in IterAllEdges()) {
                if (GetNeighbor(e).IsValid()) continue;
                Edge n = FindNeighbor(e);
                if (!n.IsValid()) continue;
                SetNeighbor(e, n);
                SetNeighbor(n, e);
            }
        }
        private struct QuadVariant {
            public int[] v0;
            public int[] v1;
            public int[] vs;
        }
        private static QuadVariant[] GetQuadVariants() {
            var qs = new QuadVariant[9];
            int[] tv = new[] { 0, 1, 2 };
            foreach (int i in tv) {
                foreach (int j in tv) {
                    qs[3*i+j] = new QuadVariant {
                        v0 = new[] { (i+1)%3, 3+(j+2)%3 },
                        v1 = new[] { (i+2)%3, 3+(j+1)%3 },
                        vs = new[] { (i+2)%3, (i+0)%3, (i+1)%3, 3+(j+0)%3 },
                    };
                }
            }
            return qs;
        }
        private static QuadVariant[] quadVariants = null;
        private static int[][] MakeQuadFaces(int[] ts) {
            if (quadVariants == null) {
                quadVariants = GetQuadVariants();
            }
            var qs = new int[ts.Length / 6][];
            for (int i = 0; i < ts.Length; i += 6) {
                bool quadFound = false;
                foreach (QuadVariant q in quadVariants) {
                    if (ts[i + q.v0[0]] == ts[i + q.v0[1]] &&
                        ts[i + q.v1[0]] == ts[i + q.v1[1]])
                    {
                        qs[i / 6] = new[] {
                            ts[i + q.vs[0]],
                            ts[i + q.vs[1]],
                            ts[i + q.vs[2]],
                            ts[i + q.vs[3]],
                        };
                        quadFound = true;
                        break;
                    }
                }
                if (!quadFound) {
                    return null;
                }
            }
            return qs;
        }
        private static int[][] MakeTriangleFaces(int[] ts) {
            var fs = new int[ts.Length / 3][];
            for (int i = 0; i < ts.Length; i += 3) {
                fs[i / 3] = new[] {
                    ts[i + 0],
                    ts[i + 1],
                    ts[i + 2],
                };
            }
            return fs;
        }
        private static int[][] TrianglesToFaces(int[] triangles) {
            return 
                MakeQuadFaces(triangles) ?? 
                MakeTriangleFaces(triangles);
        }
        public static int[] FacesToTriangles(int[][] faces) {
            var ts = new List<int>();
            foreach (int[] f in faces) {
                for (int i = 2; i < f.Length; ++i) {
                    ts.AddRange(new[] { f[0], f[i-1], f[i] });
                }
            }
            return ts.ToArray();
        }
        private static VertexContent GetMeshVertexContent() {
            var c = VertexContent.pos;
            return c;
        }

        public MeshX(Mesh mesh) {
            content = GetMeshVertexContent();
            var poses = new List<Vector3>();
            posIndices = new int[mesh.vertexCount];
            for (int i = 0; i < mesh.vertexCount; ++i) {
                Vector3 pos = mesh.vertices[i];
                int posIndex = poses.IndexOf(pos);
                if (posIndex == -1) {
                    poses.Add(pos);
                    posIndex = poses.Count - 1;
                }
                posIndices[i] = posIndex;
            }
            positions = poses.ToArray();

            int submeshCount = mesh.subMeshCount;
            submeshes = new Submesh[submeshCount];
            for (int s = 0; s < submeshCount; ++s) {
                int[][] faces = TrianglesToFaces(mesh.GetTriangles(s));
                submeshes[s] = new Submesh { faces = faces };
            }
            name = mesh.name;
        }

        public Mesh ConvertToMesh() {
            Mesh m = new Mesh { name = this.name };
            var vs = new List<Vector3>();

            foreach (int i in posIndices) {
                vs.Add(positions[i]);
            }

            m.SetVertices(vs);
            m.subMeshCount = submeshes.Length;
 
            for (int s = 0; s < submeshes.Length; ++s) {
                m.SetTriangles(FacesToTriangles(submeshes[s].faces), s);
            }
 
            m.RecalculateNormals();
            
            return m;
        }

        public static int GetIndexInFace(int[] face, int v) {
            return System.Array.IndexOf<int>(face, v);
        }

        public static int GetNextInFace(int[] face, int v, int shift = 1) {
            int i = GetIndexInFace(face, v);
            int l = face.Length;
            return face[(i + shift + l) % l];
        }

        public Edge GetNextInFace(Edge edge, int shift = 1) {
            int[] face = GetFaceVertexIndices(edge.face);
            int l = face.Length;
            return new Edge {
                face = edge.face,
                index = (edge.index + shift + l) % l
            };
        }

        public IEnumerable<Face> IterAllFaces() {
            for (int s = 0; s < submeshes.Length; ++s) {
                int[][] faces = submeshes[s].faces;
                for (int f = 0; f < faces.Length; ++f) {
                    yield return new Face { submesh = s, index = f };
                }
            }
        }

        public IEnumerable<Edge> IterAllEdges() {
            foreach (Face face in IterAllFaces()) {
                int[] vs = GetFaceVertexIndices(face);
                for (int i = 0; i < vs.Length; ++i) {
                    yield return new Edge {
                        face = face,
                        index = i
                    };
                }
            }
        }

        public int[] GetFaceVertexIndices(Face f) {
            return submeshes[f.submesh].faces[f.index];
        }

        public int[] GetEdgeVertexIndices(Edge e) {
            int[] vs = GetFaceVertexIndices(e.face);
            int v0 = vs[e.index];
            int v1 = GetNextInFace(vs, v0);
            return new[] { v0, v1 };
        }

        private Edge FindNeighbor(Edge e) {
            int[] vs = GetEdgeVertexIndices(e);
            int p0 = posIndices[vs[0]];
            int p1 = posIndices[vs[1]];
            
            foreach (int V1 in positionVertices[p1]) {
                foreach (Edge E in vertexEdges[V1]) {
                    int V0 = GetEdgeVertexIndices(E)[1];
                    int P0 = posIndices[V0];
                    if (P0 == p0) {
                        return E;
                    }
                }
            }
            
            return Edge.Invalid;
        }

        public EdgeType GetEdgeType(Edge e) {
            Edge n = GetNeighbor(e);
            if (!n.IsValid()) return EdgeType.boundary;
            int[] E = GetEdgeVertexIndices(e);
            int[] N = GetEdgeVertexIndices(n);
            if (E[0] > N[0]) return EdgeType.back;
            EdgeType type = EdgeType.solid;
            if (E[0] != N[1]) type ^= EdgeType.seam0;
            if (E[1] != N[0]) type ^= EdgeType.seam1;
            return type;
        }

        public Vertex Average(Vertex[] vs, VertexContent mask = VertexContent.full, float[] weights = null) {
            VertexContent c = mask & content;
            Vertex r = new Vertex();
            float ws = 0;
            
            for (int i = 0; i < vs.Length; ++i) {
                Vertex v = vs[i];
                float w = weights?[i] ?? 1f;

                if (c.HasPosition())
                {
                    r.position += w * v.position;
                }

                ws += w;
            }

            if (c.HasPosition())
            {
                r.position /= ws;
            }

            r.posIndex = -1;

            return r;
        }
    }
}