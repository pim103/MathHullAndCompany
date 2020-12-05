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
        
        private List<Edge> edges;
        private List<Triangle> triangles;
        public bool flipping;

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
            centers = new List<Point>();
            radiuses = new List<float>();
            List<Edge> activeEdges = edges.FindAll(e => e.isActive == false);
            Edge[] edgesToFlipArray = new Edge[activeEdges.Count];
            activeEdges.CopyTo(edgesToFlipArray);
            List<Edge> edgesToFlip = edgesToFlipArray.ToList();
            Edge currentEdge;
            bool firstTriangleInSecondCircle;
            bool secondTriangleInFirstCircle;
            Debug.Log("triangles " + triangles.Count);
            while(edgesToFlip.Count > 0)
            {
                currentEdge = edgesToFlip[0];
                edgesToFlip.Remove(currentEdge);
                firstTriangleInSecondCircle = false;
                secondTriangleInFirstCircle = false;
                List<Triangle> trianglesWithCurrentEdge = triangles.FindAll(triangle => 
                    (currentEdge.p1.GetPosition() == triangle.p1.GetPosition() || currentEdge.p1.GetPosition() == triangle.p2.GetPosition() || currentEdge.p1.GetPosition() == triangle.p3.GetPosition()) && (currentEdge.p2.GetPosition() == triangle.p1.GetPosition() || currentEdge.p2.GetPosition() == triangle.p2.GetPosition() || currentEdge.p2.GetPosition() == triangle.p3.GetPosition()) /*&& 
                                                                                        triangle.p1 != triangle.p2 && triangle.p1 != triangle.p3 && triangle.p2 != triangle.p3 &&
                                                                                        currentEdge.p1 != currentEdge.p2*/);
                Triangle oneTriangle = triangles.Find(triangle => 
                    (currentEdge.p1.GetPosition() == triangle.p1.GetPosition() || currentEdge.p1.GetPosition() == triangle.p2.GetPosition() || currentEdge.p1.GetPosition() == triangle.p3.GetPosition()) && 
                    (currentEdge.p2.GetPosition() == triangle.p1.GetPosition() || currentEdge.p2.GetPosition() == triangle.p2.GetPosition() || currentEdge.p2.GetPosition() == triangle.p3.GetPosition()));
                if (oneTriangle != null)
                {
                    Debug.Log("yes");
                }
                Debug.Log("edges " + trianglesWithCurrentEdge.Count);
                if (trianglesWithCurrentEdge.Count < 2)
                {
                    Debug.Log(edgesToFlip.Count + "wesh");
                }
                else if(trianglesWithCurrentEdge.Count > 2)
                {
                    Debug.Log("IMPOSSIBLE " + trianglesWithCurrentEdge.Count);
                    break;
                }
                else
                {
                    Debug.Log(edgesToFlip.Count + " bon ");
                    //Vector3 firstCenter = new Vector3( (float)Math.Sin(2*Vector3.Angle(trianglesWithCurrentEdge[0].p1.GetPosition() - trianglesWithCurrentEdge[0].p2.GetPosition(), trianglesWithCurrentEdge[0].p1.GetPosition() - trianglesWithCurrentEdge[0].p3.GetPosition())), 
                    Vector3 firstCenter = new Vector3((trianglesWithCurrentEdge[0].p1.GetPosition().x + trianglesWithCurrentEdge[0].p2.GetPosition().x + trianglesWithCurrentEdge[0].p3.GetPosition().x)/3,
                        (trianglesWithCurrentEdge[0].p1.GetPosition().y + trianglesWithCurrentEdge[0].p2.GetPosition().y + trianglesWithCurrentEdge[0].p3.GetPosition().y)/3,
                        (trianglesWithCurrentEdge[0].p1.GetPosition().z + trianglesWithCurrentEdge[0].p2.GetPosition().z + trianglesWithCurrentEdge[0].p3.GetPosition().z)/3);
                    //firstCenter += trianglesWithCurrentEdge[0].p1.GetPosition();
                    float firstRadius = Vector3.Distance(trianglesWithCurrentEdge[0].p1.GetPosition(),firstCenter);
                    
                    Debug.Log(firstCenter + " " + firstRadius);
                    centers.Add(new Point(firstCenter));
                    radiuses.Add(firstRadius);
                    
                    
                    /*Vector3 secondCenter = new Vector3( (float)Math.Sin(2*Vector3.Angle(trianglesWithCurrentEdge[1].p1.GetPosition() - trianglesWithCurrentEdge[1].p2.GetPosition(), trianglesWithCurrentEdge[1].p1.GetPosition() - trianglesWithCurrentEdge[1].p3.GetPosition())), 
                        (float)Math.Sin(2*Vector3.Angle(trianglesWithCurrentEdge[1].p2.GetPosition() - trianglesWithCurrentEdge[1].p1.GetPosition(), trianglesWithCurrentEdge[1].p2.GetPosition() - trianglesWithCurrentEdge[1].p3.GetPosition())),
                        (float)Math.Sin(2*Vector3.Angle(trianglesWithCurrentEdge[1].p3.GetPosition() - trianglesWithCurrentEdge[1].p1.GetPosition(), trianglesWithCurrentEdge[1].p3.GetPosition() - trianglesWithCurrentEdge[1].p2.GetPosition())));*/
                    Vector3 secondCenter =new Vector3((trianglesWithCurrentEdge[1].p1.GetPosition().x + trianglesWithCurrentEdge[1].p2.GetPosition().x + trianglesWithCurrentEdge[1].p3.GetPosition().x)/3,
                        (trianglesWithCurrentEdge[1].p1.GetPosition().y + trianglesWithCurrentEdge[1].p2.GetPosition().y + trianglesWithCurrentEdge[1].p3.GetPosition().y)/3,
                        (trianglesWithCurrentEdge[1].p1.GetPosition().z + trianglesWithCurrentEdge[1].p2.GetPosition().z + trianglesWithCurrentEdge[1].p3.GetPosition().z)/3);;
                    //secondCenter += trianglesWithCurrentEdge[1].p1.GetPosition();
                    float secondRadius = Vector3.Distance(trianglesWithCurrentEdge[1].p1.GetPosition(),secondCenter);
                    
                    Debug.Log(secondCenter + " " + secondRadius);
                    centers.Add(new Point(secondCenter));
                    radiuses.Add(secondRadius);
                    
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
                        Debug.Log("ShouldFlip");
                        Debug.Log(currentEdge.p1.GetPosition() + " " + currentEdge.p2.GetPosition());
                        edges.Remove(currentEdge);
                        triangles.Remove(trianglesWithCurrentEdge[0]);
                        triangles.Remove(trianglesWithCurrentEdge[1]);
                        
                        Point edgeP1 = null;
                        Point edgeP2 = null;
                        if (currentEdge.p1.GetPosition() != trianglesWithCurrentEdge[0].p1.GetPosition() &&
                            currentEdge.p2.GetPosition() != trianglesWithCurrentEdge[0].p1.GetPosition())
                        {
                            edgeP1 = trianglesWithCurrentEdge[0].p1;
                        } else if (currentEdge.p1.GetPosition() != trianglesWithCurrentEdge[0].p2.GetPosition() &&
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
                        } else if (currentEdge.p1.GetPosition() != trianglesWithCurrentEdge[1].p2.GetPosition() &&
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
                        edges.Add(newEdge);
                        //edgesToFlip.Add(newEdge);
                        /*Point trianglePoint = null;
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

                        if (trianglePoint != null)
                        {
                            Triangle newTriangle1 = new Triangle(edgeP1, trianglePoint, edgeP2);
                            triangles.Add(newTriangle1);
                            Debug.Log("yes");
                        }
                        else
                        {
                            Debug.Log("full merde");
                        }

                        trianglePoint = null;
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

                        if (trianglePoint != null)
                        {
                            Triangle newTriangle2 = new Triangle(edgeP2, trianglePoint, edgeP1);
                            triangles.Add(newTriangle2);
                        }
                        else
                        {
                            Debug.Log("full merdeux");
                        }*/
                        
                        Triangle newTriangle = new Triangle(edgeP1, edgeP2, currentEdge.p1);
                        triangles.Add(newTriangle);
                        Triangle newTriangle2 = new Triangle(edgeP2, edgeP1, currentEdge.p2);
                        triangles.Add(newTriangle2);
                        Debug.Log("hasFlipped " + currentEdge.p1.GetPosition() + " "+currentEdge.p2.GetPosition());
                    }
                }
            }
        }
    }
}