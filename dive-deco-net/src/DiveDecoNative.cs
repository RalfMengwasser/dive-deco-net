using System;
using System.Runtime.InteropServices;
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;
using Newtonsoft.Json.Linq;
using System.Collections.Generic;

namespace DiveDecoNet
{
    internal static class NativeMethods
    {
        private const string DllName = "dive_deco_bridge";

        [DllImport(DllName, CallingConvention = CallingConvention.Cdecl)]
        public static extern IntPtr CreateNewDive(byte gflow, byte gfhigh, int surface_pressure, double deco_ascent_rate, byte ceiling_type, bool round_ceiling, bool recalc_all_tissues_m_values, double water_density, byte stop_formatting, double last_stop_depth, double min_pp_o2);

        [DllImport(DllName, CallingConvention = CallingConvention.Cdecl)]
        public static extern void FreeCString(IntPtr c_str_ptr);

        [DllImport(DllName, CallingConvention = CallingConvention.Cdecl)]
        public static extern IntPtr GetBreathingSourceConfig(int index);

        [DllImport(DllName, CallingConvention = CallingConvention.Cdecl)]
        public static extern int AddBreathingSourceOpenCircuit(double o2, double he);

        [DllImport(DllName, CallingConvention = CallingConvention.Cdecl)]
        public static extern int AddBreathingSourceClosedCircuit(double setpoint, double diluent_o2, double diluent_he);

        [DllImport(DllName, CallingConvention = CallingConvention.Cdecl)]
        public static extern void RecordDiveSegment(double depth, double timeinSeconds, int gas_index);

        [DllImport(DllName, CallingConvention = CallingConvention.Cdecl)]
        public static extern IntPtr CalculateDeco();

        [DllImport(DllName, CallingConvention = CallingConvention.Cdecl)]
        public static extern IntPtr GetCurrentNDL();

        [DllImport(DllName, CallingConvention = CallingConvention.Cdecl)]
        public static extern IntPtr GetCurrentCeiling();

        [DllImport(DllName, CallingConvention = CallingConvention.Cdecl)]
        public static extern void RecordTravelWithRate(double depth, double rate, int gas_index);

        [DllImport(DllName, CallingConvention = CallingConvention.Cdecl)]
        public static extern void RecordTravel(double depth, double timeInMinutes, int gas_index);

        [DllImport(DllName, CallingConvention = CallingConvention.Cdecl)]
        public static extern IntPtr GetSupersaturation();

        [DllImport(DllName, CallingConvention = CallingConvention.Cdecl)]
        public static extern IntPtr GetOtu();

        [DllImport(DllName, CallingConvention = CallingConvention.Cdecl)]
        public static extern IntPtr GetCns();

        [DllImport(DllName, CallingConvention = CallingConvention.Cdecl)]
        public static extern IntPtr GetCompleteDiveState();
    }

    public static class DiveDeco
    {
        private static string? PtrToStringAndFree(IntPtr p)
        {
            if (p == IntPtr.Zero) return null;
            try
            {
                return Marshal.PtrToStringAnsi(p);
            }
            finally
            {
                NativeMethods.FreeCString(p);
            }
        }

        private static T? DeserializeAndFree<T>(IntPtr p)
        {
            var s = PtrToStringAndFree(p);
            if (s is null) return default;

            var settings = new JsonSerializerSettings
            {
                NullValueHandling = NullValueHandling.Ignore,
            };

            if (typeof(T) == typeof(JToken))
            {
                return (T?)(object?)JToken.Parse(s);
            }

            try
            {
                return JsonConvert.DeserializeObject<T>(s, settings);
            }
            catch
            {
                return default;
            }
        }

        public static DiveConfig? CreateNewDive(byte gflow, byte gfhigh, int surfacePressure, double decoAscentRateMetersPerMinute, CeilingType ceilingType, bool roundCeiling, bool recalcAllTissues, double waterDensity, StopFormatting stopFormatting, double lastStopDepthMeters, double minPpO2)
            => DeserializeAndFree<DiveConfig>(NativeMethods.CreateNewDive(gflow, gfhigh, surfacePressure, decoAscentRateMetersPerMinute, (byte)ceilingType, roundCeiling, recalcAllTissues, waterDensity, (byte)stopFormatting, lastStopDepthMeters, minPpO2));

        public static BreathingSource? GetBreathingSourceConfig(int index) => DeserializeAndFree<BreathingSource>(NativeMethods.GetBreathingSourceConfig(index));

        public static DecoResult? CalculateDeco() => DeserializeAndFree<DecoResult>(NativeMethods.CalculateDeco());

        public static NdlResult? GetCurrentNDL() => DeserializeAndFree<NdlResult>(NativeMethods.GetCurrentNDL());

        public static DepthMeter? GetCurrentCeiling() => DeserializeAndFree<DepthMeter>(NativeMethods.GetCurrentCeiling());

        public static Supersaturation? GetSupersaturation() => DeserializeAndFree<Supersaturation>(NativeMethods.GetSupersaturation());

        public static double? GetOtu() => DeserializeAndFree<double>(NativeMethods.GetOtu());

        public static double? GetCns() => DeserializeAndFree<double>(NativeMethods.GetCns());

        public static CompleteDiveState? GetCompleteDiveState() => DeserializeAndFree<CompleteDiveState>(NativeMethods.GetCompleteDiveState());

        public static int AddBreathingSourceOpenCircuit(double o2, double he) => NativeMethods.AddBreathingSourceOpenCircuit(o2, he);

        public static int AddBreathingSourceClosedCircuit(double setpoint, double diluentO2, double diluentHe) => NativeMethods.AddBreathingSourceClosedCircuit(setpoint, diluentO2, diluentHe);

        public static void RecordDiveSegment(double depthMeters, TimeSpan duration, int gasIndex) => NativeMethods.RecordDiveSegment(depthMeters, duration.TotalSeconds, gasIndex);

        public static void RecordTravelWithRate(double depthMeters, double metersPerMinute, int gasIndex) => NativeMethods.RecordTravelWithRate(depthMeters, metersPerMinute, gasIndex);
        
        public static void RecordTravel(double depthMeters, TimeSpan duration, int gasIndex) => NativeMethods.RecordTravel(depthMeters, duration.TotalMinutes, gasIndex);
    }
}

// --- Types for deserialization ---

public enum CeilingType : byte
{
    /// <summary>Actual ceiling based on current tissue loads.</summary>
    Actual = 0,
    
    /// <summary>Adaptive ceiling (uses additional logic).</summary>
    Adaptive = 1
}

public enum StopFormatting : byte
{
    /// <summary>Metric stop formatting.</summary>
    Metric = 0,
    
    /// <summary>Imperial stop formatting.</summary>
    Imperial = 1,
    
    /// <summary>Continuous stop formatting.</summary>
    Continuous = 2
}

[JsonConverter(typeof(StringEnumConverter))]
public enum DecoStageType : byte
{
    /// <summary>Ascent stage.</summary>
    Ascent = 0,

    /// <summary>Deco stop stage.</summary>
    DecoStop = 1,

    /// <summary>Gas switch stage.</summary>
    GasSwitch = 2,
}

public class DiveConfig
{
    [JsonProperty("gf")] public int[] Gf { get; set; } = Array.Empty<int>();
    [JsonProperty("surface_pressure")] public int SurfacePressure { get; set; }
    [JsonProperty("deco_ascent_rate")] public double DecoAscentRate { get; set; }
    [JsonProperty("ceiling_type")] public string? CeilingType { get; set; }
    [JsonProperty("round_ceiling")] public bool RoundCeiling { get; set; }
    [JsonProperty("recalc_all_tissues_m_values")] public bool RecalcAllTissuesMValues { get; set; }
    [JsonProperty("water_density")] public double WaterDensity { get; set; }
    [JsonProperty("stop_formatting")] public StopFormatting StopFormatting { get; set; }
    [JsonProperty("last_stop_depth")] public DepthMeter? LastStopDepth { get; set; }
    [JsonProperty("min_pp_o2")] public double MinPpO2 { get; set; }
}

public class DepthMeter { [JsonProperty("m")] public double Meters { get; set; } }

public class BreathingSource
{
    [JsonProperty("OpenCircuit")] public Gas? OpenCircuit { get; set; }
    [JsonProperty("ClosedCircuit")] public ClosedCircuit? ClosedCircuit { get; set; }
}

public class Gas
{
    [JsonProperty("fraction_he")] public double FractionHe { get; set; }
    [JsonProperty("fraction_n2")] public double FractionN2 { get; set; }
    [JsonProperty("fraction_o2")] public double FractionO2 { get; set; }
}

public class ClosedCircuit
{
    [JsonProperty("setpoint")] public double Setpoint { get; set; }
    [JsonProperty("diluent")] public Gas? Diluent { get; set; }
}

public class DecoResult
{
    [JsonProperty("success")] public bool Success { get; set; }
    [JsonProperty("error")] public string? Error { get; set; }
    [JsonProperty("decotable")] public DecoTable? Decotable { get; set; }
}

public class DecoTable
{
    [JsonProperty("deco_stages")] public DecoStage[]? DecoStages { get; set; }
    [JsonProperty("tts")] public DurationSeconds? Tts { get; set; }
    [JsonProperty("tts_at_5")] public DurationSeconds? TtsAt5 { get; set; }
    [JsonProperty("tts_delta_at_5")] public DurationSeconds? TtsDeltaAt5 { get; set; }
    [JsonProperty("error")] public string? Error { get; set; }
}

public class DecoStage
{
    [JsonProperty("duration")] public DurationSeconds? Duration { get; set; }
    [JsonProperty("end_depth")] public DepthMeter? EndDepth { get; set; }
    [JsonProperty("gas")] public BreathingSource? Gas { get; set; }
    [JsonProperty("stage_type")] public DecoStageType? StageType { get; set; }
    [JsonProperty("start_depth")] public DepthMeter? StartDepth { get; set; }
}

public class DurationSeconds { [JsonProperty("s")] public double Seconds { get; set; } }

public class NdlResult { [JsonProperty("s")] public double Seconds { get; set; } }

public class Supersaturation { [JsonProperty("gf_99")] public double Gf99 { get; set; }
                                [JsonProperty("gf_surf")] public double GfSurf { get; set; } }

public class CompleteDiveState
{
    [JsonProperty("cns")] public double? Cns { get; set; }
    [JsonProperty("otu")] public double? Otu { get; set; }
    [JsonProperty("supersaturation")] public Supersaturation? Supersaturation { get; set; }
    [JsonProperty("current_ceiling")] public DepthMeter? CurrentCeiling { get; set; }
    [JsonProperty("current_ndl")] public NdlResult? CurrentNdl { get; set; }
    [JsonProperty("deco_result")] public DecoResult? DecoResult { get; set; }
}

