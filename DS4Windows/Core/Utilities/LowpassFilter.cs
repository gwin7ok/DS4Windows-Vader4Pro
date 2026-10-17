using System;
using System.Collections.Generic;
using System.Text;
using System.Diagnostics;

namespace Sensorit.Base
{

    public class LowpassFilter
    {
        public LowpassFilter()
        {
            firstTime = true;
        }

        protected bool firstTime;
        protected double hatXPrev;

        public double Last()
        {
            return hatXPrev;
        }

        public double Filter(double x, double alpha)
        {
            double hatX = 0;
            if (firstTime)
            {
                firstTime = false;
                hatX = x;
            }
            else
                hatX = alpha * x + (1 - alpha) * hatXPrev;

            hatXPrev = hatX;

            return hatX;
        }

        public void Reset()
        {
            firstTime = true;
            hatXPrev = 0.0;
        }
    }
}
