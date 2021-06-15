using System.Linq;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;
using Utils;

namespace LoopSubdiv
{

    public class Model
    {
        List<Vertex> vertices;
        List<Edge3> edges;
        public List<Triangle3> triangles;

        public Model()
        {
            vertices = new List<Vertex>();
            edges = new List<Edge3>();
            triangles = new List<Triangle3>();
        }

        public Model(Mesh source)
        {
            vertices = new List<Vertex>();
            edges = new List<Edge3>();
            this.triangles = new List<Triangle3>();
            
            Vector3[] points = source.vertices;
            for (int i = 0, n = points.Length; i < n; i++)
            {
                //Debug.Log(source.vertices[i]);
                Vertex v = new Vertex(points[i], i);
                vertices.Add(v);
            }

            int[] triangles = source.triangles;
            for (int i = 0, n = triangles.Length; i < n; i += 3)
            {
                int i0 = triangles[i], i1 = triangles[i + 1], i2 = triangles[i + 2];
                Vertex v0 = vertices[i0], v1 = vertices[i1], v2 = vertices[i2];

                Edge3 e0 = GetEdge(edges, v0, v1);
                Edge3 e1 = GetEdge(edges, v1, v2);
                Edge3 e2 = GetEdge(edges, v2, v0);
                Triangle3 f = new Triangle3(v0, v1, v2, e0, e1, e2);

                this.triangles.Add(f);
                v0.AddTriangle(f); v1.AddTriangle(f); v2.AddTriangle(f);
                e0.AddTriangle(f); e1.AddTriangle(f); e2.AddTriangle(f);
            }
        }

        Edge3 GetEdge(List<Edge3> edges, Vertex v0, Vertex v1)
        {
            Edge3 match = v0.edges.Find(e => {
                return e.Has(v1);
            });
            if (match != null) return match;

            Edge3 ne = new Edge3(v0, v1);
            v0.AddEdge(ne);
            v1.AddEdge(ne);
            edges.Add(ne);
            return ne;
        }

        public void AddTriangle(Vertex v0, Vertex v1, Vertex v2)
        {
            if (!vertices.Contains(v0)) vertices.Add(v0);
            if (!vertices.Contains(v1)) vertices.Add(v1);
            if (!vertices.Contains(v2)) vertices.Add(v2);

            Edge3 e0 = GetEdge(v0, v1);
            Edge3 e1 = GetEdge(v1, v2);
            Edge3 e2 = GetEdge(v2, v0);
            Triangle3 f = new Triangle3(v0, v1, v2, e0, e1, e2);

            this.triangles.Add(f);
            v0.AddTriangle(f); v1.AddTriangle(f); v2.AddTriangle(f);
            e0.AddTriangle(f); e1.AddTriangle(f); e2.AddTriangle(f);
        }

        Edge3 GetEdge(Vertex v0, Vertex v1)
        {
            Edge3 match = v0.edges.Find(e =>
            {
                return (e.a == v1 || e.b == v1);
            });
            if (match != null) return match;

            Edge3 ne = new Edge3(v0, v1);
            edges.Add(ne);
            v0.AddEdge(ne);
            v1.AddEdge(ne);
            return ne;
        }

        public Mesh Build(bool weld = false)
        {
            Mesh mesh = new Mesh();
            int[] triangles = new int[this.triangles.Count * 3];


            Vector3[] vertices = new Vector3[this.triangles.Count * 3];
            for (int i = 0, n = this.triangles.Count; i < n; i++)
            {
                Triangle3 f = this.triangles[i];
                int i0 = i * 3, i1 = i * 3 + 1, i2 = i * 3 + 2;
                vertices[i0] = f.v0.p;
                vertices[i1] = f.v1.p;
                vertices[i2] = f.v2.p;
                triangles[i0] = i0;
                triangles[i1] = i1;
                triangles[i2] = i2;
            }
            mesh.vertices = vertices;
            

            mesh.indexFormat = mesh.vertexCount < 65535 ? IndexFormat.UInt16 : IndexFormat.UInt32;
            mesh.triangles = triangles;

            mesh.RecalculateNormals();
            mesh.RecalculateBounds();

            return mesh;
        }

    }

}