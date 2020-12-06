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

        private Vector3 AbsVector(Vector3 vector)
        {
            vector.x = vector.x < 0 ? vector.x * -1 : vector.x;
            vector.y = vector.y < 0 ? vector.y * -1 : vector.y;
            vector.z = vector.z < 0 ? vector.z * -1 : vector.z;

            return vector;
        }

        private Vector3 PowVector(Vector3 vector, int value)
        {
            vector.x = (float)Math.Pow(vector.x, value);
            vector.y = (float)Math.Pow(vector.y, value);
            vector.z = (float)Math.Pow(vector.z, value);

            return vector;
        }

        public static Vector3 CircleCenter(Vector3 aP0, Vector3 aP1, Vector3 aP2)
        {
            // two circle chords
            var v1 = aP1 - aP0;
            var v2 = aP2 - aP0;

            Vector3 normal = Vector3.Cross(v1, v2);
            if (normal.sqrMagnitude < 0.00001f)
                return Vector3.one * float.NaN;
            normal.Normalize();

            // perpendicular of both chords
            var p1 = Vector3.Cross(v1, normal).normalized;
            var p2 = Vector3.Cross(v2, normal).normalized;
            // distance between the chord midpoints
            var r = (v1 - v2) * 0.5f;
            // center angle between the two perpendiculars
            var c = Vector3.Angle(p1, p2);
            // angle between first perpendicular and chord midpoint vector
            var a = Vector3.Angle(r, p1);
            // law of sine to calculate length of p2
            var d = r.magnitude * Mathf.Sin(a * Mathf.Deg2Rad) / Mathf.Sin(c * Mathf.Deg2Rad);
            if (Vector3.Dot(v1, aP2 - aP1) > 0)
                return aP0 + v2 * 0.5f - p2 * d;
            return aP0 + v2 * 0.5f + p2 * d;
        }
        
        private void EdgeFlipping()
        {
            centers = new List<Point>();
            radiuses = new List<float>();
            List<Edge> activeEdges = edges.FindAll(e => e.isActive == false);
            List<Edge> edgesToFlip = new List<Edge>(activeEdges);
            Edge currentEdge;

            while(edgesToFlip.Count > 0)
            {
                currentEdge = edgesToFlip[0];
                edgesToFlip.Remove(currentEdge);
                var firstTriangleInSecondCircle = false;
                var secondTriangleInFirstCircle = false;
                List<Triangle> trianglesWithCurrentEdge = triangles.FindAll(triangle => 
                    (currentEdge.p1.GetPosition() == triangle.p1.GetPosition() || currentEdge.p1.GetPosition() == triangle.p2.GetPosition() || currentEdge.p1.GetPosition() == triangle.p3.GetPosition()) && 
                    (currentEdge.p2.GetPosition() == triangle.p1.GetPosition() || currentEdge.p2.GetPosition() == triangle.p2.GetPosition() || currentEdge.p2.GetPosition() == triangle.p3.GetPosition()));

                if (trianglesWithCurrentEdge.Count == 2)
                {
                    Vector3 p1Pos = trianglesWithCurrentEdge[0].p1.GetPosition();
                    Vector3 p2Pos = trianglesWithCurrentEdge[0].p2.GetPosition();
                    Vector3 p3Pos = trianglesWithCurrentEdge[0].p3.GetPosition();
                   
                    Vector3 firstCenter = CircleCenter(p1Pos,p2Pos,p3Pos);
                    float firstRadius = Vector3.Distance(trianglesWithCurrentEdge[0].p1.GetPosition(),firstCenter);
                    
                    Debug.Log(firstCenter + " " + firstRadius);
                    centers.Add(new Point(firstCenter));
                    radiuses.Add(firstRadius/2);
                    
                    
                    p1Pos = trianglesWithCurrentEdge[1].p1.GetPosition();
                    p2Pos = trianglesWithCurrentEdge[1].p2.GetPosition(); 
                    p3Pos = trianglesWithCurrentEdge[1].p3.GetPosition();
                    Vector3 secondCenter = CircleCenter(p1Pos,p2Pos,p3Pos);

                    float secondRadius = Vector3.Distance(trianglesWithCurrentEdge[1].p1.GetPosition(),secondCenter);
                    
                    Debug.Log(secondCenter + " " + secondRadius);
                    centers.Add(new Point(secondCenter));
                    radiuses.Add(secondRadius/2);
                    
                    /*firstTriangleInSecondCircle = true;
                    secondTriangleInFirstCircle = true;*/
                    
                    Debug.Log("triangle debug : first triangle " + secondRadius + " " + Vector3.Distance(trianglesWithCurrentEdge[0].p1.GetPosition(),secondCenter) + " " + Vector3.Distance(trianglesWithCurrentEdge[0].p2.GetPosition(),secondCenter) + " " + Vector3.Distance(trianglesWithCurrentEdge[0].p3.GetPosition(),secondCenter));
                    if(Vector3.Distance(trianglesWithCurrentEdge[0].p1.GetPosition(),secondCenter) <= secondRadius && Vector3.Distance(trianglesWithCurrentEdge[0].p2.GetPosition(),secondCenter) <= secondRadius && Vector3.Distance(trianglesWithCurrentEdge[0].p3.GetPosition(),secondCenter) <= secondRadius)
                    {
                        
                        firstTriangleInSecondCircle = true;
                        Debug.Log("triangle debug : firstEntered");
                    }
                    Debug.Log("triangle debug : second triangle " + firstRadius + " " + Vector3.Distance(trianglesWithCurrentEdge[1].p1.GetPosition(),firstCenter) + " " + Vector3.Distance(trianglesWithCurrentEdge[1].p2.GetPosition(),firstCenter) + " " + Vector3.Distance(trianglesWithCurrentEdge[1].p3.GetPosition(),firstCenter));
                    if(Vector3.Distance(trianglesWithCurrentEdge[1].p1.GetPosition(),firstCenter) <= firstRadius && Vector3.Distance(trianglesWithCurrentEdge[1].p2.GetPosition(),firstCenter) <= firstRadius && Vector3.Distance(trianglesWithCurrentEdge[1].p3.GetPosition(),firstCenter) <= firstRadius)
                    {
                        secondTriangleInFirstCircle = true;
                        Debug.Log("triangle debug : secondEntered");
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
                            Debug.Log("edge1_1 " + edgeP1.GetPosition() + " " + currentEdge.p1.GetPosition() + " " + currentEdge.p2.GetPosition());
                        } else if (currentEdge.p1.GetPosition() != trianglesWithCurrentEdge[0].p2.GetPosition() &&
                            currentEdge.p2.GetPosition() != trianglesWithCurrentEdge[0].p2.GetPosition())
                        {
                            edgeP1 = trianglesWithCurrentEdge[0].p2;
                            Debug.Log("edge1_2" + edgeP1.GetPosition() + " " + currentEdge.p1.GetPosition() + " " + currentEdge.p2.GetPosition());
                        }
                        else if (currentEdge.p1.GetPosition() != trianglesWithCurrentEdge[0].p3.GetPosition() &&
                            currentEdge.p2.GetPosition() != trianglesWithCurrentEdge[0].p3.GetPosition())
                        {
                            edgeP1 = trianglesWithCurrentEdge[0].p3;
                            Debug.Log("edge1_3" + edgeP1.GetPosition() + " " + currentEdge.p1.GetPosition() + " " + currentEdge.p2.GetPosition());
                        }
                        if (currentEdge.p1.GetPosition() != trianglesWithCurrentEdge[1].p1.GetPosition() &&
                            currentEdge.p2.GetPosition() != trianglesWithCurrentEdge[1].p1.GetPosition())
                        {
                            edgeP2 = trianglesWithCurrentEdge[1].p1;
                            Debug.Log("edge2_1" + edgeP2.GetPosition() + " " + currentEdge.p1.GetPosition() + " " + currentEdge.p2.GetPosition());
                        } else if (currentEdge.p1.GetPosition() != trianglesWithCurrentEdge[1].p2.GetPosition() &&
                            currentEdge.p2.GetPosition() != trianglesWithCurrentEdge[1].p2.GetPosition())
                        {
                            edgeP2 = trianglesWithCurrentEdge[1].p2;
                            Debug.Log("edge2_2" + edgeP2.GetPosition() + " " + currentEdge.p1.GetPosition() + " " + currentEdge.p2.GetPosition());
                        }
                        else if (currentEdge.p1.GetPosition() != trianglesWithCurrentEdge[1].p3.GetPosition() &&
                            currentEdge.p2.GetPosition() != trianglesWithCurrentEdge[1].p3.GetPosition())
                        {
                            edgeP2 = trianglesWithCurrentEdge[1].p3;
                            Debug.Log("edge2_3" + edgeP2.GetPosition() + " " + currentEdge.p1.GetPosition() + " " + currentEdge.p2.GetPosition());
                        }
//                        edges.Add(newEdge);
                        
                        Edge newEdge = new Edge(edgeP1,edgeP2);
                        AddNewTriangle(newEdge, currentEdge.p1);
                        AddNewTriangle(newEdge, currentEdge.p2);
                    }
                }
            }
        }
    }
}