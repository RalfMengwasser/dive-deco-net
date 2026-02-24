using System;
using Newtonsoft.Json;
using DiveDecoNet;

class Program
{
    static void Main(string[] args)
    {
        // Create a new dive with some defaults (example values)
        DiveConfig? cfg = DiveDeco.CreateNewDive(50, 85, 1013, 10.0, CeilingType.Adaptive, false, true, 1020.0, 0, 3.0, 0.18);
        Console.WriteLine("CreateNewDive -> " + (cfg != null ? JsonConvert.SerializeObject(cfg) : "(null)"));

        // Add breathing gas and record a short segment
        int gasIndex = DiveDeco.AddBreathingSourceOpenCircuit(0.21, 0.0);
        
        Console.WriteLine($"----- Down to 20 m with 10 m/min on air -----");
        DiveDeco.RecordTravelWithRate(20.0, 10.0, gasIndex);
        
        Console.WriteLine($"----- 20 m for 30 mins on air -----");
        DiveDeco.RecordDiveSegment(20.0, TimeSpan.FromMinutes(30), gasIndex);

        CompleteDiveState? completeDiveState = DiveDeco.GetCompleteDiveState();
        Console.WriteLine("GetCompleteDiveState -> " + (completeDiveState != null ? JsonConvert.SerializeObject(completeDiveState) : "(null)"));
      
        Console.WriteLine($"----- Up to 15 m within 3 minutes on air -----");
        DiveDeco.RecordTravel(15.0, TimeSpan.FromMinutes(3), gasIndex);
        
        Console.WriteLine($"----- 15 m for 30 mins on air -----");
        DiveDeco.RecordDiveSegment(15.0, TimeSpan.FromMinutes(30), gasIndex);
        
        completeDiveState = DiveDeco.GetCompleteDiveState();
        Console.WriteLine("GetCompleteDiveState -> " + (completeDiveState != null ? JsonConvert.SerializeObject(completeDiveState) : "(null)"));
        
        Console.WriteLine($"----- Up to 1 m within 3 minutes on air -----");
        DiveDeco.RecordTravel(1.0, TimeSpan.FromMinutes(3), gasIndex);
        
        completeDiveState = DiveDeco.GetCompleteDiveState();
        Console.WriteLine("GetCompleteDiveState -> " + (completeDiveState != null ? JsonConvert.SerializeObject(completeDiveState) : "(null)"));  
    }
}
