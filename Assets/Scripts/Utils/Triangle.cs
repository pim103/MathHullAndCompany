using System.Collections.Generic;
using UnityEngine;

namespace Utils
{
    public class Triangle
    {
        public Point p1;
        public Point p2;
        public Point p3;

        public Triangle(Point p1, Point p2, Point p3)
        {
            Vector3 firstVector = p2.GetPosition() - p1.GetPosition();
            Vector3 secondVector = p3.GetPosition() - p1.GetPosition();
            
            float det = firstVector.x * secondVector.y - firstVector.y * secondVector.x;

            Point temp;
            if (det < 0)
            {
                temp = p2;
                p2 = p3;
                p3 = temp;
            }

            this.p1 = p1;
            this.p2 = p2;
            this.p3 = p3;
        }

        public List<Edge> GetEdges()
        {
            List<Edge> edges = new List<Edge>
            {
                new Edge(p1, p2), 
                new Edge(p2, p3), 
                new Edge(p3, p1)
            };

            return edges;
        }
    }
}