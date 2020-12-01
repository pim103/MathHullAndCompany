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

        private List<Edge> edges;
        private List<Triangle> triangles;
        public bool flipping;

        public List<GameObject> edgeObjects;

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
        }

        private Edge FindSameEdges(Edge edge)
        {
            Edge findEdge = edges.Find(
                eachEdge => 
                    (eachEdge.p1 == edge.p1 || eachEdge.p2 == edge.p1) && 
                    (eachEdge.p1 == edge.p2 || eachEdge.p2 == edge.p2));

            return findEdge;
        }

        private void AddNewTriangle(Edge edge, Point p)
        {
            Edge findEdge;

            Triangle newTriangle = new Triangle(edge.p1, edge.p2, p);
            triangles.Add(newTriangle);

            List<Edge> newEdgeInTriangle = newTriangle.GetEdges();

            foreach (Edge eachEdge in newEdgeInTriangle)
            {
                if ((findEdge = FindSameEdges(eachEdge)) == null)
                {
                    edges.Add(eachEdge);
                }
                else
                {
                    findEdge.isActive = false;
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
            Edge[] edgesToFlipArray = new Edge[edges.Count];
            edges.CopyTo(edgesToFlipArray);
            List<Edge> edgesToFlip = edgesToFlipArray.ToList();
            Edge currentEdge;
            bool firstTriangleInSecondCircle;
            bool secondTriangleInFirstCircle;
            while(edgesToFlip.Count > 0)
            {
                currentEdge = edgesToFlip[0];
                edgesToFlip.Remove(currentEdge);
                firstTriangleInSecondCircle = false;
                secondTriangleInFirstCircle = false;
                List<Triangle> trianglesWithCurrentEdge = triangles.FindAll(triangle => (currentEdge.p1 == triangle.p1 || currentEdge.p1 == triangle.p2 || currentEdge.p1 == triangle.p3) && 
                                                                                        (currentEdge.p2 == triangle.p1 || currentEdge.p2 == triangle.p2 || currentEdge.p2 == triangle.p3) && 
                                                                                        triangle.p1 != triangle.p2 && triangle.p1 != triangle.p3 && triangle.p2 != triangle.p3 &&
                                                                                        currentEdge.p1 != currentEdge.p2);
                if (trianglesWithCurrentEdge.Count < 2)
                {
                    Debug.Log(edgesToFlip.Count);
                }
                else if(trianglesWithCurrentEdge.Count > 2)
                {
                    Debug.Log("IMPOSSIBLE " + trianglesWithCurrentEdge.Count);
                }
                else
                {
                    Debug.Log(edgesToFlip.Count + " bon ");
                    Vector3 firstCenter = new Vector3( (float)Math.Sin(2*Vector3.Angle(trianglesWithCurrentEdge[0].p1.GetPosition() - trianglesWithCurrentEdge[0].p2.GetPosition(), trianglesWithCurrentEdge[0].p1.GetPosition() - trianglesWithCurrentEdge[0].p3.GetPosition())), 
                        (float)Math.Sin(2*Vector3.Angle(trianglesWithCurrentEdge[0].p2.GetPosition() - trianglesWithCurrentEdge[0].p1.GetPosition(), trianglesWithCurrentEdge[0].p2.GetPosition() - trianglesWithCurrentEdge[0].p3.GetPosition())),
                        (float)Math.Sin(2*Vector3.Angle(trianglesWithCurrentEdge[0].p3.GetPosition() - trianglesWithCurrentEdge[0].p1.GetPosition(), trianglesWithCurrentEdge[0].p3.GetPosition() - trianglesWithCurrentEdge[0].p2.GetPosition())));
                    float firstRadius = Vector3.Distance(trianglesWithCurrentEdge[0].p1.GetPosition(),trianglesWithCurrentEdge[0].p2.GetPosition())/(float)Math.Sin(Vector3.Angle(trianglesWithCurrentEdge[0].p1.GetPosition() - trianglesWithCurrentEdge[0].p2.GetPosition(), trianglesWithCurrentEdge[0].p1.GetPosition() - trianglesWithCurrentEdge[0].p3.GetPosition()));
                    
                    Vector3 secondCenter = new Vector3( (float)Math.Sin(2*Vector3.Angle(trianglesWithCurrentEdge[1].p1.GetPosition() - trianglesWithCurrentEdge[1].p2.GetPosition(), trianglesWithCurrentEdge[1].p1.GetPosition() - trianglesWithCurrentEdge[1].p3.GetPosition())), 
                        (float)Math.Sin(2*Vector3.Angle(trianglesWithCurrentEdge[1].p2.GetPosition() - trianglesWithCurrentEdge[1].p1.GetPosition(), trianglesWithCurrentEdge[1].p2.GetPosition() - trianglesWithCurrentEdge[1].p3.GetPosition())),
                        (float)Math.Sin(2*Vector3.Angle(trianglesWithCurrentEdge[1].p3.GetPosition() - trianglesWithCurrentEdge[1].p1.GetPosition(), trianglesWithCurrentEdge[1].p3.GetPosition() - trianglesWithCurrentEdge[1].p2.GetPosition())));
                    float secondRadius = Vector3.Distance(trianglesWithCurrentEdge[1].p1.GetPosition(),trianglesWithCurrentEdge[1].p2.GetPosition())/(float)Math.Sin(Vector3.Angle(trianglesWithCurrentEdge[1].p1.GetPosition() - trianglesWithCurrentEdge[1].p2.GetPosition(), trianglesWithCurrentEdge[1].p1.GetPosition() - trianglesWithCurrentEdge[1].p3.GetPosition()));
                    
                    
                    if(Vector3.Distance(trianglesWithCurrentEdge[0].p1.GetPosition(),secondCenter) < secondRadius && Vector3.Distance(trianglesWithCurrentEdge[0].p2.GetPosition(),secondCenter) < secondRadius && Vector3.Distance(trianglesWithCurrentEdge[0].p3.GetPosition(),secondCenter) < secondRadius)
                    {
                        firstTriangleInSecondCircle = true;
                    }
                    if(Vector3.Distance(trianglesWithCurrentEdge[1].p1.GetPosition(),firstCenter) < firstRadius && Vector3.Distance(trianglesWithCurrentEdge[1].p2.GetPosition(),firstCenter) < firstRadius && Vector3.Distance(trianglesWithCurrentEdge[1].p3.GetPosition(),firstCenter) < firstRadius)
                    {
                        secondTriangleInFirstCircle = true;
                    }

                    if (firstTriangleInSecondCircle || secondTriangleInFirstCircle)
                    {
                        edges.Remove(currentEdge);
                        triangles.Remove(trianglesWithCurrentEdge[0]);
                        triangles.Remove(trianglesWithCurrentEdge[1]);
                        
                        Point edgeP1 = null;
                        Point edgeP2 = null;
                        if (currentEdge.p1 != trianglesWithCurrentEdge[0].p1 &&
                            currentEdge.p2 != trianglesWithCurrentEdge[0].p1)
                        {
                            edgeP1 = trianglesWithCurrentEdge[0].p1;
                        } else if (currentEdge.p1 != trianglesWithCurrentEdge[0].p2 &&
                            currentEdge.p2 != trianglesWithCurrentEdge[0].p2)
                        {
                            edgeP1 = trianglesWithCurrentEdge[0].p2;
                        }
                        else if (currentEdge.p1 != trianglesWithCurrentEdge[0].p3 &&
                            currentEdge.p2 != trianglesWithCurrentEdge[0].p3)
                        {
                            edgeP1 = trianglesWithCurrentEdge[0].p3;
                        }
                        if (currentEdge.p1 != trianglesWithCurrentEdge[1].p1 &&
                            currentEdge.p2 != trianglesWithCurrentEdge[1].p1)
                        {
                            edgeP2 = trianglesWithCurrentEdge[1].p1;
                        } else if (currentEdge.p1 != trianglesWithCurrentEdge[1].p2 &&
                            currentEdge.p2 != trianglesWithCurrentEdge[1].p2)
                        {
                            edgeP2 = trianglesWithCurrentEdge[1].p2;
                        }
                        else if (currentEdge.p1 != trianglesWithCurrentEdge[1].p3 &&
                            currentEdge.p2 != trianglesWithCurrentEdge[1].p3)
                        {
                            edgeP2 = trianglesWithCurrentEdge[1].p3;
                        }
                        Edge newEdge = new Edge(edgeP1,edgeP2);
                        edges.Add(newEdge);
                        Point trianglePoint = null;
                        if (edgeP1 != trianglesWithCurrentEdge[0].p1 &&
                            edgeP2 != trianglesWithCurrentEdge[0].p1)
                        {
                            trianglePoint = trianglesWithCurrentEdge[0].p1;
                        } else if (edgeP1 != trianglesWithCurrentEdge[0].p2 &&
                                   edgeP2 != trianglesWithCurrentEdge[0].p2)
                        {
                            trianglePoint = trianglesWithCurrentEdge[0].p2;
                        }
                        else if (edgeP1 != trianglesWithCurrentEdge[0].p3 &&
                                 edgeP1 != trianglesWithCurrentEdge[0].p3)
                        {
                            trianglePoint = trianglesWithCurrentEdge[0].p3;
                        }
                        Triangle newTriangle1 = new Triangle(edgeP1,edgeP2,trianglePoint);
                        triangles.Add(newTriangle1);
                        if (edgeP1 != trianglesWithCurrentEdge[1].p1 &&
                            edgeP2 != trianglesWithCurrentEdge[1].p1)
                        {
                            trianglePoint = trianglesWithCurrentEdge[1].p1;
                        } else if (edgeP1 != trianglesWithCurrentEdge[1].p2 &&
                                   edgeP2 != trianglesWithCurrentEdge[1].p2)
                        {
                            trianglePoint = trianglesWithCurrentEdge[1].p2;
                        }
                        else if (edgeP1 != trianglesWithCurrentEdge[1].p3 &&
                                 edgeP1 != trianglesWithCurrentEdge[1].p3)
                        {
                            trianglePoint = trianglesWithCurrentEdge[1].p3;
                        }
                        Triangle newTriangle2 = new Triangle(edgeP1,edgeP2,trianglePoint);
                        triangles.Add(newTriangle2);
                        //ça fait dla merde, mais y a une logique au moins, hypothèse 1 : mauvais sens de création des triangles, hypothèse 2 : les calculs sont foirés, hypothèse 3: on a fait full merde
                        // et oui y a dla duplication mais c'est juste pour tester au moins, NIQUE PIM
                    }
                }
            }
        }
    }
}