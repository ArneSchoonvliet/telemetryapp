using rF2SMMonitor.rFactor2Data;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace TelemetryApp
{
    public class Telemetry
    {
        public Telemetry()
        {

        }
        public Telemetry(ref rF2VehicleTelemetry telemetry)
        {
            RubberTemperatures = telemetry.mWheels.Select(x => new RubberTemperature(x)).ToList();
            CarcassTemperatures = telemetry.mWheels.Select(x => new CarcassTemperature(x)).ToList();
        }
        public IEnumerable<RubberTemperature> RubberTemperatures { get; }
        public IEnumerable<CarcassTemperature> CarcassTemperatures { get; }

        
    }

    public class RubberTemperature 
    {
        public RubberTemperature()
        {

        }
        public RubberTemperature(rF2Wheel wheel)
        {
            if(wheel.mTireInnerLayerTemperature != null)
            {
                Left = wheel.mTireInnerLayerTemperature[0];
                Right = wheel.mTireInnerLayerTemperature[1];
                Middle = wheel.mTireInnerLayerTemperature[2];
            }
        }
        public double Left { get; set; }
        public double Right { get; set; }
        public double Middle { get; set; }
        //public WheelOrientation Wheel { get; set; }

    }

    public class CarcassTemperature
    {
        public CarcassTemperature()
        {

        }
        public CarcassTemperature(rF2Wheel wheel)
        {
            Value = wheel.mTireCarcassTemperature;
        }
        public double Value { get; set; }
        //public WheelOrientation Wheel { get; set; }
    }

    public enum WheelOrientation
    {
        FrontLeft,
        FrontRight,
        RearLeft,
        RearRight
    }
}
