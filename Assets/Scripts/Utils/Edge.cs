using System;
using UnityEngine;

namespace Utils
{
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
    }
}