using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using Utils;
using MathUtils = Utils.MathUtils;

namespace GrahamScan
{
    public class GrahamScanScript
    {
        private List<Point> points;
        private List<Point> calculatedPoints;

        public GrahamScanScript(List<Point> npoints)
        {
            points = npoints;
        }

        private Point FindBarycentre()
        {
            float x = 0;
            float y = 0;
            
            foreach (Point p in points)
            {
                x += p.GetPosition().x;
                y += p.GetPosition().y;
            }

            x /= points.Count;
            y /= points.Count;
            return new Point(Vector3.zero + Vector3.up * y + Vector3.right * x);
        }

        private List<Point> OrderList(Point barycentre)
        {
            List<Point> sortPoints = new List<Point>();

            sortPoints = points.OrderBy(calculatedPoint =>
            {
                Vector3 vectorTest = calculatedPoint.GetPosition() - barycentre.GetPosition();
                return MathUtils.AngleClockwise(Vector3.right, vectorTest);
            }).ThenBy(calculatedPoint => Vector3.Distance(barycentre.GetPosition(), calculatedPoint.GetPosition())).ToList();

            return sortPoints;
        }

        private void ComputeGraham()
        {
            Point bary = FindBarycentre();

            calculatedPoints = OrderList(bary);

            Point sInit = calculatedPoints[0];
            Point pivot = sInit;

            int index = 0;
            bool goForward;
            
            do
            {
                Vector3 sourceAngle = calculatedPoints[index != 0 ? index - 1 : calculatedPoints.Count - 1].GetPosition() - pivot.GetPosition();

                Vector3 targetAngle = pivot.GetPosition() - calculatedPoints[(index + 1 == calculatedPoints.Count ? 0 : index + 1) % calculatedPoints.Count].GetPosition();

                float calculatedAngle = MathUtils.AngleClockwise(sourceAngle, targetAngle);
                if (calculatedAngle < 0)
                {
                    calculatedAngle = 2 * (float)Math.PI - calculatedAngle;
                }

                if (calculatedAngle < Math.PI)
                {
                    index = index + 1 >= calculatedPoints.Count ? 0 : index + 1;

                    pivot = calculatedPoints[index];
                    goForward = true;
                }
                else
                {
                    index = index != 0 ? index - 1 : calculatedPoints.Count - 1;
                    sInit = calculatedPoints[index];
                    calculatedPoints.Remove(pivot);
                    pivot = sInit;

                    goForward = false;
                }
            } while (pivot != sInit || !goForward);
        }

        public List<Point> ComputeAndDisplayGraham()
        {
            ComputeGraham();
            
            int index = 0;
            foreach (Point p in calculatedPoints)
            {
                p.SetGameObjectName("Objet : " + index);
                index++;
            }

            return calculatedPoints;
        }
    }
}