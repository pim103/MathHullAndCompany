using System;
using System.Collections.Generic;
using UnityEngine;

namespace Utils
{
    public class Edge3
    {
        public Vertex a, b;
        public List<Triangle3> faces;
        public Vertex ept;

        public Edge3(Vertex a, Vertex b)
        {
            this.a = a;
            this.b = b;
            this.faces = new List<Triangle3>();
        }

        public void AddTriangle(Triangle3 f)
        {
            faces.Add(f);
        }

        public bool Has(Vertex v)
        {
            return v == a || v == b;
        }

        public Vertex GetOtherVertex(Vertex v)
        {
            if (a != v) return a;
            return b;
        }
    }
    public class Edge
    {
        public Point p1;
        public Point p2;
        public bool isActive = true;

        public Edge(Point p1, Point p2)
        {
            this.p1 = p1;
            this.p2 = p2;
        }

        public Vector3 GetNormal()
        {
            Vector3 normalised = (p2.GetPosition() - p1.GetPosition()).normalized;

            Vector3 normal = Vector3.zero;
            normal.x = normalised.y;
            normal.y = -normalised.x;

            return normal;
        }

        public List<Edge> GetAdjacentEdges(List<Edge> edges)
        {
            return edges.FindAll(edge => edge.p1.GetPosition() == p1.GetPosition() && edge.p2.GetPosition() != p2.GetPosition() || 
                                         edge.p1.GetPosition() != p1.GetPosition() && edge.p2.GetPosition() == p2.GetPosition() || 
                                         edge.p1.GetPosition() != p2.GetPosition() && edge.p2.GetPosition() == p1.GetPosition() || 
                                         edge.p1.GetPosition() == p2.GetPosition() && edge.p2.GetPosition() != p1.GetPosition());
        }
        
        public Edge FindSameEdges(List<Edge> edges)
        {
            Edge findEdge = edges.Find(
                eachEdge => 
                    (eachEdge.p1.GetPosition() == p1.GetPosition() || eachEdge.p2.GetPosition() == p1.GetPosition()) && 
                    (eachEdge.p1.GetPosition() == p2.GetPosition() || eachEdge.p2.GetPosition() == p2.GetPosition()));

            return findEdge;
        }

        public int GetNbTriangleContainingEdge(List<Triangle> triangles)
        {
            return GetTriangleContainingEdge(triangles).Count;
        }
        
        public List<Triangle> GetTriangleContainingEdge(List<Triangle> triangles)
        {
            List<Triangle> trianglesContainingEdge = triangles.FindAll(triangle => 
                (p1.GetPosition() == triangle.p1.GetPosition() || p1.GetPosition() == triangle.p2.GetPosition() || p1.GetPosition() == triangle.p3.GetPosition()) && 
                (p2.GetPosition() == triangle.p1.GetPosition() || p2.GetPosition() == triangle.p2.GetPosition() || p2.GetPosition() == triangle.p3.GetPosition()));

            return trianglesContainingEdge;
        }

        public Vector3 GetCenter()
        {
            Vector3 center = Vector3.zero;
            Vector3 pos1 = p1.GetPosition();
            Vector3 pos2 = p2.GetPosition();

            center.x = (pos1.x + pos2.x) / 2;
            center.y = (pos1.y + pos2.y) / 2;
            center.z = (pos1.z + pos2.z) / 2;

            return center;
        }
    }
}