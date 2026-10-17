using System;
using System.Diagnostics;
using DS4WinWPF.DS4Control;

namespace DS4Windows
{
    // Phase7-Step7 W4: 旧 Mapping の入れ子の型をトップレベルへ移した（Q5＝A。中身は変えていない）。
    public class DeltaSettingsProcessor
    {
        double previousPointerX = 0.0;
        //double accelHelperX = 0.0;
        //double accelTravelX = 0.0;
        //Stopwatch deltaEasingTimeX = new Stopwatch();

        double previousPointerY = 0.0;
        //double accelHelperY = 0.0;
        //double accelTravelY = 0.0;
        //Stopwatch deltaEasingTimeY = new Stopwatch();

        double previousPointerRadial = 0.0;
        double accelCurrentMultiRadial = 0.0;
        double accelEasingMultiRadial = 0.0;
        double accelTravelRadial = 0.0;
        Stopwatch deltaEasingTimeRadial = new Stopwatch();
        double totalTravelRadial = 0.0;

        public bool useDeltaAccel = false;
        public double AccelOutXNorm = 0.0;
        public double AccelOutYNorm = 0.0;

        public void Process(int device, double axisDirX, double axisDirY,
            double axisRawDirX, double axisRawDirY,
            double maxXDir, double maxYDir,
            DeltaAccelSettings mouseDeltaSettings)
        {
            //DeltaAccelSettings mouseDeltaSettings = stickSettings.outputSettings.controlSettings.deltaAccelSettings;
            // Calculate delta acceleration slope and offset.
            bool testDeltaAccel = useDeltaAccel = mouseDeltaSettings.enabled;
            double testAccelMulti = mouseDeltaSettings.multiplier;
            double testAccelMaxTravel = mouseDeltaSettings.maxTravel;
            double testAccelMinTravel = mouseDeltaSettings.minTravel;
            double testAccelEasingDuration = mouseDeltaSettings.easingDuration;
            double minfactor = Math.Max(1.0, mouseDeltaSettings.minfactor); // default 1.0
            double minTravelStop = Math.Max(0.1, testAccelMinTravel);

            double accelSlope = (testAccelMulti - minfactor) / (testAccelMaxTravel - testAccelMinTravel);
            double accelOffset = minfactor - (accelSlope * testAccelMinTravel);

            double outXNorm = (axisDirX) / maxXDir, outYNorm = (axisDirY) / maxYDir;
            AccelOutXNorm = outXNorm; AccelOutYNorm = outYNorm;
            double rawXNorm = (axisRawDirX) / maxXDir, rawYNorm = (axisRawDirY) / maxYDir;
            double absX = Math.Abs(outXNorm);
            double absY = Math.Abs(outYNorm);

            double hyp = Math.Sqrt((rawXNorm * rawXNorm) + (rawYNorm * rawYNorm));

            if (testDeltaAccel)
            {
                //Trace.WriteLine("DELTA CHECK");
                //double tempCheckTravel = !inDuration ? testAccelMinTravel : testAccelMinTravel;
                if (hyp > 0.0 &&
                    Math.Abs(hyp - previousPointerRadial) >= testAccelMinTravel &&
                    (hyp - previousPointerRadial >= 0.0))
                {
                    double tempTravel = Math.Abs(hyp - previousPointerRadial);
                    double tempDist = tempTravel;

                    if (totalTravelRadial == 0.0)
                    {
                        totalTravelRadial = tempTravel;
                        accelEasingMultiRadial = (accelSlope * tempDist + accelOffset);
                    }
                    else
                    {
                        totalTravelRadial += tempDist;
                        double tempEasingDist = totalTravelRadial;
                        //tempDist = tempEasingDist;
                        //tempTravel = tempDist;
                        accelEasingMultiRadial = (accelSlope * tempEasingDist + accelOffset);
                    }

                    accelCurrentMultiRadial = (accelSlope * tempDist + accelOffset);
                    outXNorm = outXNorm * accelCurrentMultiRadial;
                    outYNorm = outYNorm * accelCurrentMultiRadial;
                    accelTravelRadial = tempTravel;

                    deltaEasingTimeRadial.Restart();
                    //currentTime = Stopwatch.GetTimestamp();
                    //previousTime = currentTime;

                    previousPointerRadial = hyp;
                    previousPointerX = rawXNorm;
                    previousPointerY = rawYNorm;

                    //Trace.WriteLine($"WTF {hyp} {accelTravelRadial} {accelCurrentMultiRadial} {accelEasingMultiRadial}");
                }
                else if (hyp > 0.0 && accelCurrentMultiRadial > 0.0 &&
                    Math.Abs(previousPointerRadial - hyp) < minTravelStop &&
                    !(
                    (previousPointerX >= 0.0) != (rawXNorm >= 0.0) &&
                    (previousPointerY >= 0.0) != (rawYNorm >= 0.0))
                    )
                {
                    //Trace.WriteLine("STAY ZONE");
                    //inDuration = true;

                    double timeElapsed = deltaEasingTimeRadial.ElapsedMilliseconds;
                    //currentTime = Stopwatch.GetTimestamp();
                    //double timeElapsed = (currentTime - previousTime) * (1.0 / Stopwatch.Frequency) * 1000.0;
                    double elapsedDiff = 1.0;
                    double tempAccel = accelCurrentMultiRadial;
                    double tempTravel = accelTravelRadial;

                    if (hyp - previousPointerRadial <= 0.0)
                    {
                        double tempmix2 = Math.Abs(hyp - previousPointerRadial);
                        tempmix2 = Math.Min(tempmix2, minTravelStop);
                        double tempmixslope = (testAccelMinTravel - tempTravel) / minTravelStop;
                        double tempshitintercept = tempTravel;
                        double finalmanham = (tempmixslope * tempmix2 + tempshitintercept);

                        tempTravel = finalmanham;
                        tempAccel = (accelSlope * (tempTravel) + accelOffset);
                    }

                    double elapsedDuration = testAccelEasingDuration * (accelEasingMultiRadial / testAccelMulti);
                    //Trace.WriteLine($"TIME ELAPSED: {timeElapsed} {tempAccel} {elapsedDuration}");
                    if (elapsedDuration > 0.0 && (timeElapsed * 0.001) < elapsedDuration)
                    {
                        elapsedDiff = ((timeElapsed * 0.001) / elapsedDuration);
                        elapsedDiff = (1.0 - tempAccel) * (elapsedDiff * elapsedDiff * elapsedDiff) + tempAccel;
                        outXNorm = elapsedDiff * outXNorm;
                        outYNorm = elapsedDiff * outYNorm;

                        //Trace.WriteLine($"CONITNUING {elapsedDiff}");
                    }
                    else
                    {
                        // Easing time has ended. Reset values.
                        previousPointerRadial = hyp;
                        accelCurrentMultiRadial = 0.0;
                        accelTravelRadial = 0.0;
                        deltaEasingTimeRadial.Reset();
                        accelEasingMultiRadial = 0.0;
                        totalTravelRadial = 0.0;
                        //previousTime = currentTime;
                        previousPointerX = rawXNorm;
                        previousPointerY = rawYNorm;
                        //inDuration = false;

                        //Trace.WriteLine($"DURATION ENDED");
                    }
                }
                else
                {
                    //Trace.WriteLine("NEW RESET");
                    previousPointerRadial = hyp;
                    accelCurrentMultiRadial = 0.0;
                    accelTravelRadial = 0.0;
                    accelEasingMultiRadial = 0.0;
                    totalTravelRadial = 0.0;
                    deltaEasingTimeRadial.Reset();
                    //currentTime = Stopwatch.GetTimestamp();
                    //previousTime = currentTime;
                    previousPointerX = rawXNorm;
                    previousPointerY = rawYNorm;
                    //inDuration = false;
                }
            }
            else
            {
                previousPointerRadial = hyp;
                previousPointerX = rawXNorm;
                previousPointerY = rawYNorm;
                accelCurrentMultiRadial = 0.0;
                accelTravelRadial = 0.0;
                accelEasingMultiRadial = 0.0;
                totalTravelRadial = 0.0;
                //inDuration = false;
                //currentTime = Stopwatch.GetTimestamp();
                //previousTime = currentTime;
                //if (deltaEasingTimeRadial.IsRunning)
                {
                    deltaEasingTimeRadial.Reset();
                }
            }

            AccelOutXNorm = outXNorm; AccelOutYNorm = outYNorm;
            //Trace.WriteLine($"X: {AccelOutXNorm} | Y: {AccelOutYNorm}");
        }

        public void Reset()
        {
            useDeltaAccel = false;
            AccelOutXNorm = AccelOutYNorm = 0.0;

            previousPointerRadial = 0.0;
            previousPointerX = 0.0;
            previousPointerY = 0.0;
            accelCurrentMultiRadial = 0.0;
            accelTravelRadial = 0.0;
            accelEasingMultiRadial = 0.0;
            totalTravelRadial = 0.0;
            //inDuration = false;
            //currentTime = Stopwatch.GetTimestamp();
            //previousTime = currentTime;
            deltaEasingTimeRadial.Reset();
        }
    }
}
