using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using Utils;

namespace Kobbelt
{
    public class KobbeltScript
    {
        private List<Edge> oldEdges;
        private List<Edge> edges;
        private List<Triangle> triangles;
        private List<Triangle> computeTriangles;

        private List<Point> newVertices;
        
        private Mesh sourceMesh;

        public KobbeltScript(List<Triangle> trianglesInScene)
        {
            newVertices = new List<Point>();
            edges = new List<Edge>();
            oldEdges = new List<Edge>();

            triangles = trianglesInScene;
            computeTriangles = new List<Triangle>();
        }
        
        public KobbeltScript(Mesh mesh)
        {
            newVertices = new List<Point>();
            edges = new List<Edge>();
            oldEdges = new List<Edge>();

            triangles = new List<Triangle>();
            computeTriangles = new List<Triangle>();
            sourceMesh = mesh;
            
            for (int i = 0; i < sourceMesh.triangles.Length; i += 3)
            {
                Point p1 = AddPointToVertice(new Point(sourceMesh.vertices[sourceMesh.triangles[i]]));
                Point p2 = AddPointToVertice(new Point(sourceMesh.vertices[sourceMesh.triangles[i + 1]]));
                Point p3 = AddPointToVertice(new Point(sourceMesh.vertices[sourceMesh.triangles[i + 2]]));
            
                triangles.Add(new Triangle(p1, p2, p3));
            }
        }

        private Point AddPointToVertice(Point pointToAdd)
        {
            Point existingPoint = newVertices.Find(p => p.GetPosition() == pointToAdd.GetPosition());
            
            if (existingPoint == null)
            {
                existingPoint = pointToAdd;
                newVertices.Add(pointToAdd);
            }

            return existingPoint;
        }

        private void GetEdgesInTriangles(List<Triangle> triangles)
        {
            foreach (Triangle triangle in triangles)
            {
                foreach (Edge e in triangle.GetEdges())
                {
                    if (e.FindSameEdges(oldEdges) == null)
                    {
                        oldEdges.Add(e);
                    }
                }
            }
        }

        private void SubdivideTriangle(Triangle triangle)
        {
            Point barycenter = triangle.interCenter;
            List<Point> pointsInTriangle = triangle.GetPoints();

            // if (newVertices.Find(p => p.GetPosition() == barycenter.GetPosition()) == null)
            // {
            //     newVertices.Add(barycenter);
            // }

            foreach (Point p in pointsInTriangle)
            {
                Edge e = new Edge(barycenter, p);

                if (e.FindSameEdges(edges) == null)
                {
                    edges.Add(e);
                }
                
            }
        }

        private float CalculAlpha(int n)
        {
            float alpha = 9 * (4 - 2 * (float)Math.Cos((2 * Math.PI) / n));

            return 1 / alpha;
        }

        private Vector3 SumOfVectorAdjacentAtPoint(List<Edge> adjacent, Point origin)
        {
            Vector3 sum = Vector3.zero;
            
            foreach (Edge e in adjacent)
            {
                if (e.p1.GetPosition() != origin.GetPosition())
                {
                    sum += e.p1.GetPosition();
                }
                else
                {
                    sum += e.p2.GetPosition();
                }
            }

            return sum;
        }
        
        private void MoveVertices(List<Point> points)
        {
            foreach (Point p in points.ToArray())
            {
                Vector3 initialPosition = p.GetPosition();

                List<Edge> edgesContainingPoint = p.FindEdgeWithPoint(edges);

                int nbEdges = edgesContainingPoint.Count;
                float alpha = CalculAlpha(nbEdges);

                Vector3 vectorMove = (1 - alpha) * initialPosition +
                                     (alpha / nbEdges) * SumOfVectorAdjacentAtPoint(edgesContainingPoint, p);

                p.SetPosition(vectorMove.x, vectorMove.y, vectorMove.z);
                p.someProp = true;
            }
        }

        public void ComputeKobbelt()
        {
            GetEdgesInTriangles(triangles);
            
            // Step 1 - Subdivide triangle
            foreach (Triangle triangle in triangles)
            {
                SubdivideTriangle(triangle);
            }


            // Step 2 - Perturbed vertices
            MoveVertices(newVertices);

            // Step 2 - Flipping edges
            foreach (Edge currentEdge in oldEdges)
            {
                List<Triangle> trianglesContainingEdge = currentEdge.GetTriangleContainingEdge(triangles);

                // If 2 triangles contains edge, create edge with center of triangles
                if (trianglesContainingEdge.Count > 1)
                {
                    edges.Add(new Edge(trianglesContainingEdge[0].interCenter, trianglesContainingEdge[1].interCenter));
                }
                // Else, create edge from center to infinite
                else if (trianglesContainingEdge.Count == 1)
                {
                    Vector3 centerEdge = currentEdge.GetCenter();

                    Vector3 direction = trianglesContainingEdge[0].interCenter.GetPosition() - centerEdge;
                    
                    if (Vector3.Dot(direction, currentEdge.GetNormal()) < 0)
                    {
                        direction *= -1;
                    }
                    
                    Point exageratedPoint = new Point(direction.normalized * 1000);
                    edges.Add(new Edge(trianglesContainingEdge[0].interCenter, exageratedPoint));
                }
            }

            CreateTrianglesFromEdges();
        }

        private void CreateTrianglesFromEdges()
        {
            Point center = new Point(Vector3.zero);
            
            foreach (Edge e in edges)
            {
                List<Edge> adjacents = e.GetAdjacentEdges(edges);

                foreach (Edge adjacent in adjacents)
                {
                    Edge lastEdge = null;
                    
                    if (adjacent.p1.GetPosition() == e.p1.GetPosition() || adjacent.p1.GetPosition() == e.p2.GetPosition())
                    {
                        Vector3 otherPointSearched = adjacent.p1.GetPosition() == e.p1.GetPosition() ? e.p2.GetPosition() : e.p1.GetPosition();

                        lastEdge = edges.Find(edge =>
                            (edge.p1.GetPosition() == otherPointSearched || edge.p2.GetPosition() == otherPointSearched) && 
                            (edge.p1.GetPosition() == adjacent.p2.GetPosition() || edge.p2.GetPosition() == adjacent.p2.GetPosition()));
                    } else if (adjacent.p2.GetPosition() == e.p1.GetPosition() || adjacent.p2.GetPosition() == e.p2.GetPosition())
                    {
                        Vector3 otherPointSearched = adjacent.p2.GetPosition() == e.p1.GetPosition() ? e.p2.GetPosition() : e.p1.GetPosition();

                        lastEdge = edges.Find(edge =>
                            (edge.p1.GetPosition() == otherPointSearched || edge.p2.GetPosition() == otherPointSearched) && 
                            (edge.p1.GetPosition() == adjacent.p1.GetPosition() || edge.p2.GetPosition() == adjacent.p1.GetPosition()));
                    }

                    if (lastEdge != null)
                    {
                        Point lastPoint = lastEdge.p1.GetPosition() == e.p1.GetPosition() || lastEdge.p1.GetPosition() == e.p2.GetPosition() ? lastEdge.p2 : lastEdge.p1;
                        Triangle t = new Triangle(e.p1, e.p2, lastPoint, center);

                        if (t.FindSameTriangle(computeTriangles).Count == 0)
                        {
                            computeTriangles.Add(t);
                        }
                    }
                }
            }
        }

        public List<Edge> GetEdgesComputed()
        {
            return edges;
        }
        
        public List<Triangle> GetTrianglesComputed()
        {
            return computeTriangles;
        }
    }
}