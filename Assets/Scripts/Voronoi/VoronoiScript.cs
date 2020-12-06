using System.Collections.Generic;
using UnityEngine;
using Utils;

namespace Voronoi
{
    public class VoronoiScript
    {
        private List<Edge> edges;
        private List<Triangle> triangles;

        private List<Edge> voronoiEdges;

        public VoronoiScript(List<Triangle> triangles, List<Edge> edges)
        {
            this.edges = edges;
            this.triangles = triangles;
        }

        public void AddEdges(Point p1, Point p2)
        {
            Edge newEdge = new Edge(p1, p2);

            if (newEdge.FindSameEdges(voronoiEdges) == null)
            {
                voronoiEdges.Add(newEdge);
            }
        }

        public List<Edge> ComputeAndGetEdges()
        {
            voronoiEdges = new List<Edge>();

            foreach (Edge currentEdge in edges)
            {
                if (currentEdge.isActive)
                {
                    Triangle triangle = currentEdge.GetTriangleContainingEdge(triangles)[0];
                    Vector3 centerEdge = currentEdge.GetCenter();

                    Vector3 direction = triangle.center.GetPosition() - centerEdge;

                    if (Vector3.Dot(direction, currentEdge.GetNormal()) < 0)
                    {
                        direction *= -1;
                    }
                    
                    Point exageratedPoint = new Point(direction.normalized * 1000);
                    AddEdges(triangle.center, exageratedPoint);
                }
                else
                {
                    List<Triangle> trianglesContainingEdge = currentEdge.GetTriangleContainingEdge(triangles);

                    if (trianglesContainingEdge.Count > 1)
                    {
                        AddEdges(trianglesContainingEdge[0].center, trianglesContainingEdge[1].center);
                    }
                }
            }

            return voronoiEdges;
        }
    }
}