using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using Utils;

namespace Triangulation
{
    public class IncrementalTriangulation
    {
        private List<Point> points;
        private List<Point> calculatedPoints;

        public List<Point> centers;
        public List<float> radiuses;
        
        public List<Edge> edges;
        public List<Triangle> triangles;
        public bool flipping;

        private const float EPSILON = 0.00001f;

        public IncrementalTriangulation(List<Point> npoints)
        {
            points = npoints;
            edges = new List<Edge>();
            triangles = new List<Triangle>();
            flipping = false;
        }

        private List<Point> GetOrderedPoints()
        {
            if (points == null || points.Count == 0)
            {
                return null;
            }

            return points.OrderBy(p1 => p1.GetPosition().x).ThenBy(p1 => p1.GetPosition().y).ToList();
        }

        private void CreateFirstTriangle()
        {
            Triangle firstTriangle = new Triangle(calculatedPoints[0], calculatedPoints[1], calculatedPoints[2]);
            edges.AddRange(firstTriangle.GetEdges());
            triangles.Add(firstTriangle);
        }

        private List<Edge> edgesToFlip;

        private void AddNewTriangle(Edge edge, Point p)
        {
            Edge findEdge;

            Triangle newTriangle = new Triangle(edge.p1, edge.p2, p);
            triangles.Add(newTriangle);

            List<Edge> newEdgeInTriangle = newTriangle.GetEdges();

            foreach (Edge eachEdge in newEdgeInTriangle)
            {
                findEdge = eachEdge.FindSameEdges(edges);
                int nbTrianglesContainingEdge = findEdge?.GetNbTriangleContainingEdge(triangles) ?? 0;

                if (nbTrianglesContainingEdge == 0)
                {
                    edges.Add(eachEdge);
                }
                else if (nbTrianglesContainingEdge > 1)
                {
                    findEdge.isActive = false;
                }

                if (edgesToFlip != null)
                {
                    findEdge = eachEdge.FindSameEdges(edgesToFlip);
                    nbTrianglesContainingEdge = findEdge?.GetNbTriangleContainingEdge(triangles) ?? 0;
                    if (nbTrianglesContainingEdge == 0)
                    {
                        edgesToFlip.Add(eachEdge);
                    }
                }
            }
        }

        private void ComputeIncrementalTriangulation()
        {
            calculatedPoints = GetOrderedPoints();
            
            int index = 0;
            foreach (Point p in calculatedPoints)
            {
                p.SetGameObjectName("Objet : " + index);
                index++;
            }

            CreateFirstTriangle();

            int idx = 3;
            int safeLoopIteration = 0;

            while (idx < calculatedPoints.Count && (++safeLoopIteration) < 10000)
            {
                Point currentPointChecked = calculatedPoints[idx];
                
                List<Edge> activeEdges = edges.FindAll(e => e.isActive);

                foreach (Edge eachActiveEdge in activeEdges)
                {
                    Vector3 normal = eachActiveEdge.GetNormal();
                    Vector3 positionTested = currentPointChecked.GetPosition() - eachActiveEdge.p1.GetPosition();

                    if (Vector3.Dot(normal, positionTested) > 0)
                    {
                        AddNewTriangle(eachActiveEdge, currentPointChecked);

                        eachActiveEdge.isActive = false;
                    }
                }

                ++idx;
            }
        }

        public List<Edge> ComputeAndGetEdges()
        {
            ComputeIncrementalTriangulation();
            if (flipping)
            {
                EdgeFlipping();
            }
            return edges;
        }

        private void EdgeFlipping()
        {
            centers = new List<Point>();
            radiuses = new List<float>();
            edgesToFlip = edges.FindAll(e => e.isActive == false);
            Edge currentEdge;

            while(edgesToFlip.Count > 0)
            {
                currentEdge = edgesToFlip[0];
                edgesToFlip.Remove(currentEdge);

                var firstTriangleInSecondCircle = false;
                var secondTriangleInFirstCircle = false;

                List<Triangle> trianglesWithCurrentEdge = triangles.FindAll(triangle => 
                    (currentEdge.p1.GetPosition() == triangle.p1.GetPosition() || 
                     currentEdge.p1.GetPosition() == triangle.p2.GetPosition() || 
                     currentEdge.p1.GetPosition() == triangle.p3.GetPosition()) && 
                    (currentEdge.p2.GetPosition() == triangle.p1.GetPosition() || 
                     currentEdge.p2.GetPosition() == triangle.p2.GetPosition() || 
                     currentEdge.p2.GetPosition() == triangle.p3.GetPosition()));

                if (trianglesWithCurrentEdge.Count == 2)
                {
                    Vector3 firstCenter = trianglesWithCurrentEdge[0].center.GetPosition();
                    float firstRadius = Vector3.Distance(trianglesWithCurrentEdge[0].p1.GetPosition(),firstCenter);

                    Vector3 secondCenter = trianglesWithCurrentEdge[1].center.GetPosition();
                    float secondRadius = Vector3.Distance(trianglesWithCurrentEdge[1].p1.GetPosition(),secondCenter);

//                    Debug.Log("triangle debug : first triangle point : " +
//                              trianglesWithCurrentEdge[0].p1.GetPosition().x + " " +
//                              trianglesWithCurrentEdge[0].p1.GetPosition().y + " " +
//                              trianglesWithCurrentEdge[0].p2.GetPosition().x + " " +
//                              trianglesWithCurrentEdge[0].p2.GetPosition().y + " " +
//                              trianglesWithCurrentEdge[0].p3.GetPosition().x + " " +
//                              trianglesWithCurrentEdge[0].p3.GetPosition().y + " " +
//                              " radiusOtherTriangle : " + secondRadius + " dist : " + 
//                              Vector3.Distance(trianglesWithCurrentEdge[0].p1.GetPosition(),secondCenter) + " " + 
//                              Vector3.Distance(trianglesWithCurrentEdge[0].p2.GetPosition(),secondCenter) + " " + 
//                              Vector3.Distance(trianglesWithCurrentEdge[0].p3.GetPosition(),secondCenter));
//
//                    Debug.Log("triangle debug : second triangle point : " +
//                              trianglesWithCurrentEdge[1].p1.GetPosition().x + " " +
//                              trianglesWithCurrentEdge[1].p1.GetPosition().y + " " +
//                              trianglesWithCurrentEdge[1].p2.GetPosition().x + " " +
//                              trianglesWithCurrentEdge[1].p2.GetPosition().y + " " +
//                              trianglesWithCurrentEdge[1].p3.GetPosition().x + " " +
//                              trianglesWithCurrentEdge[1].p3.GetPosition().y + " " +
//                              " radiusOtherTriangle : " + firstRadius + " " + 
//                              Vector3.Distance(trianglesWithCurrentEdge[1].p1.GetPosition(),firstCenter) + " " + 
//                              Vector3.Distance(trianglesWithCurrentEdge[1].p2.GetPosition(),firstCenter) + " " + 
//                              Vector3.Distance(trianglesWithCurrentEdge[1].p3.GetPosition(),firstCenter));

                    if(Vector3.Distance(trianglesWithCurrentEdge[0].p1.GetPosition(),secondCenter) <= secondRadius + EPSILON &&
                       Vector3.Distance(trianglesWithCurrentEdge[0].p2.GetPosition(),secondCenter) <= secondRadius + EPSILON &&
                       Vector3.Distance(trianglesWithCurrentEdge[0].p3.GetPosition(),secondCenter) <= secondRadius + EPSILON)
                    {
                        firstTriangleInSecondCircle = true;
                    }

                    if(Vector3.Distance(trianglesWithCurrentEdge[1].p1.GetPosition(),firstCenter) <= firstRadius + EPSILON &&
                       Vector3.Distance(trianglesWithCurrentEdge[1].p2.GetPosition(),firstCenter) <= firstRadius + EPSILON &&
                       Vector3.Distance(trianglesWithCurrentEdge[1].p3.GetPosition(),firstCenter) <= firstRadius + EPSILON)
                    {
                        secondTriangleInFirstCircle = true;
                    }

                    if (firstTriangleInSecondCircle || secondTriangleInFirstCircle)
                    {
                        edges.Remove(currentEdge.FindSameEdges(edges));
                        triangles.Remove(trianglesWithCurrentEdge[0]);
                        triangles.Remove(trianglesWithCurrentEdge[1]);

                        Point edgeP1 = null;
                        Point edgeP2 = null;

                        if (currentEdge.p1.GetPosition() != trianglesWithCurrentEdge[0].p1.GetPosition() &&
                            currentEdge.p2.GetPosition() != trianglesWithCurrentEdge[0].p1.GetPosition())
                        {
                            edgeP1 = trianglesWithCurrentEdge[0].p1;
                        }
                        else if (currentEdge.p1.GetPosition() != trianglesWithCurrentEdge[0].p2.GetPosition() &&
                            currentEdge.p2.GetPosition() != trianglesWithCurrentEdge[0].p2.GetPosition())
                        {
                            edgeP1 = trianglesWithCurrentEdge[0].p2;
                        }
                        else if (currentEdge.p1.GetPosition() != trianglesWithCurrentEdge[0].p3.GetPosition() &&
                            currentEdge.p2.GetPosition() != trianglesWithCurrentEdge[0].p3.GetPosition())
                        {
                            edgeP1 = trianglesWithCurrentEdge[0].p3;
                        }

                        if (currentEdge.p1.GetPosition() != trianglesWithCurrentEdge[1].p1.GetPosition() &&
                            currentEdge.p2.GetPosition() != trianglesWithCurrentEdge[1].p1.GetPosition())
                        {
                            edgeP2 = trianglesWithCurrentEdge[1].p1;
                        }
                        else if (currentEdge.p1.GetPosition() != trianglesWithCurrentEdge[1].p2.GetPosition() &&
                            currentEdge.p2.GetPosition() != trianglesWithCurrentEdge[1].p2.GetPosition())
                        {
                            edgeP2 = trianglesWithCurrentEdge[1].p2;
                        }
                        else if (currentEdge.p1.GetPosition() != trianglesWithCurrentEdge[1].p3.GetPosition() &&
                            currentEdge.p2.GetPosition() != trianglesWithCurrentEdge[1].p3.GetPosition())
                        {
                            edgeP2 = trianglesWithCurrentEdge[1].p3;
                        }

                        Edge newEdge = new Edge(edgeP1,edgeP2);
                        AddNewTriangle(newEdge, currentEdge.p1);
                        AddNewTriangle(newEdge, currentEdge.p2);
                    }
                }
            }

            triangles.ForEach(tri =>
            {
                centers.Add(tri.center);
                radiuses.Add(Vector3.Distance(tri.p1.GetPosition(), tri.center.GetPosition()) / 2);
            });
        }
    }
}