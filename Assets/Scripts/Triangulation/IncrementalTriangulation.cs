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
        
        public IncrementalTriangulation(List<Point> npoints)
        {
            points = npoints;
            edges = new List<Edge>();
            triangles = new List<Triangle>();
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

            return edges;
        }
    }
}