use dive_deco::{BuhlmannConfig, BuhlmannModel, CeilingType, DecoModel, DecoStopFormatting, Depth, Gas, Time, BreathingSource};
use std::ffi::CString;
use std::os::raw::c_char;
use serde::{Serialize, Deserialize};
use std::sync::Mutex;
use once_cell::sync::Lazy;

static CONFIG: Lazy<Mutex<Option<BuhlmannConfig>>> = Lazy::new(|| Mutex::new(None));
static DIVE: Lazy<Mutex<Option<BuhlmannModel>>> = Lazy::new(|| Mutex::new(None));
static BREATHING_SOURCES: Lazy<Mutex<Vec<BreathingSource>>> = Lazy::new(|| Mutex::new(Vec::new()));

#[derive(Debug, Serialize, Deserialize)]
struct DecoResult {
    success: bool,
    error: Option<String>,
    decotable: Option<serde_json::Value>,
}

#[derive(Debug, Serialize, Deserialize)]
struct CompleteDiveState {
    cns: Option<f64>,
    otu: Option<f64>,
    supersaturation: Option<serde_json::Value>,
    current_ceiling: Option<serde_json::Value>,
    current_ndl: Option<serde_json::Value>,
    deco_result: Option<DecoResult>,
}

fn serialize_to_c_string<T: Serialize>(obj: &T) -> *const c_char
{
    match serde_json::to_string(obj) {
        Ok(json_str) => {
            match CString::new(json_str) {
                Ok(c_string) => c_string.into_raw() as *const c_char,
                Err(_) => std::ptr::null(),
            }
        }
        Err(_) => std::ptr::null(),
    }
}

pub fn println_c_string(c_str_ptr: *const c_char)
{
    if !c_str_ptr.is_null() {
        let c_str = unsafe { std::ffi::CStr::from_ptr(c_str_ptr) };
        if let Ok(str_slice) = c_str.to_str() {
            println!("{}", str_slice);
        }

        FreeCString(c_str_ptr);
    }
}

#[no_mangle]
pub extern "C" fn FreeCString(c_str_ptr: *const c_char)
{
    if c_str_ptr.is_null() {
        return;
    }

    unsafe {
        // Recreate a CString from the raw pointer so Rust will free it when dropped.
        let _ = CString::from_raw(c_str_ptr as *mut c_char);
    }
}

#[no_mangle]
pub extern "C" fn CreateNewDive (gflow: u8,
                                 gfhigh: u8,
                                 surface_pressure: i32,
                                 deco_ascent_rate: f64,
                                 ceiling_type: u8,
                                 round_ceiling: bool,
                                 recalc_all_tissues_m_values: bool,
                                 water_density: f64,
                                 stop_formatting: u8,
                                 last_stop_depth: f64,
                                 min_pp_o2: f64) -> *const c_char
{
    let ceiling_type = match ceiling_type {
        0 => CeilingType::Actual,
        1 => CeilingType::Adaptive,
        _ => panic!("Invalid ceiling type"),
    };

    let stop_formatting = match stop_formatting {
        0 => DecoStopFormatting::Metric,
        1 => DecoStopFormatting::Imperial,
        2 => DecoStopFormatting::Continuous,
        _ => panic!("Invalid stop formatting"),
    };

    let config = BuhlmannConfig {
        gf: (gflow, gfhigh),
        surface_pressure,
        deco_ascent_rate,
        ceiling_type,
        round_ceiling,
        recalc_all_tissues_m_values,
        water_density,
        stop_formatting,
        last_stop_depth: Depth::from_meters(last_stop_depth),
        min_pp_o2,
    };

    let mut config_guard = CONFIG.lock().unwrap();
    *config_guard = Some(config.clone());
    drop(config_guard);

    let mut dive_guard = DIVE.lock().unwrap();
    *dive_guard = Some(BuhlmannModel::new(config.clone()));
    drop(dive_guard);

    serialize_to_c_string(&config)
}

#[no_mangle]
pub extern "C" fn GetBreathingSourceConfig (index: i32) -> *const c_char
{
    let sources = BREATHING_SOURCES.lock().unwrap();

    if let Some(bs) = sources.get(index as usize) {
        serialize_to_c_string(bs)
    } else {
        std::ptr::null()
    }
}

#[no_mangle]
pub extern "C" fn AddBreathingSourceOpenCircuit(o2: f64, he: f64) -> i32
{
    let mut sources = BREATHING_SOURCES.lock().unwrap();

    let bs = BreathingSource::OpenCircuit(Gas::new(o2, he));
    sources.push(bs);
    (sources.len() - 1) as i32
}

#[no_mangle]
pub extern "C" fn AddBreathingSourceClosedCircuit(setpoint: f64, diluent_o2: f64, diluent_he: f64) -> i32
{
    let mut sources = BREATHING_SOURCES.lock().unwrap();

    let bs = BreathingSource::ClosedCircuit {
        setpoint: setpoint,
        diluent: Gas::new(diluent_o2, diluent_he),
    };
    sources.push(bs);
    (sources.len() - 1) as i32
}

#[no_mangle]
pub extern "C" fn RecordDiveSegment(depth: f64, time: f64, gas_index: i32)
{
    let gassources = BREATHING_SOURCES.lock().unwrap();
    if let Some(gassource) = gassources.get(gas_index as usize) {
        let mut dive_guard = DIVE.lock().unwrap();
        if let Some(dive) = dive_guard.as_mut() {
            dive.record(Depth::from_meters(depth), Time::from_seconds(time), gassource);
        }
    }
}

#[no_mangle]
pub extern "C" fn RecordTravelWithRate(depth: f64, rate: f64, gas_index: i32)
{
    let gassources = BREATHING_SOURCES.lock().unwrap();
    if let Some(gassource) = gassources.get(gas_index as usize) {
        let mut dive_guard = DIVE.lock().unwrap();
        if let Some(dive) = dive_guard.as_mut() {
            dive.record_travel_with_rate(Depth::from_meters(depth), rate, gassource);
        }
    }
}

#[no_mangle]
pub extern "C" fn RecordTravel(depth: f64, time: f64, gas_index: i32)
{
    let gassources = BREATHING_SOURCES.lock().unwrap();
    if let Some(gassource) = gassources.get(gas_index as usize) {
        let mut dive_guard = DIVE.lock().unwrap();
        if let Some(dive) = dive_guard.as_mut() {
            dive.record_travel(Depth::from_meters(depth), Time::from_minutes(time), gassource);
        }
    }
}

#[no_mangle]
pub extern "C" fn CalculateDeco() -> *const c_char
{
    let dive_guard = DIVE.lock().unwrap();
    if let Some(dive) = dive_guard.as_ref() {
        let gassources = BREATHING_SOURCES.lock().unwrap();
        match dive.deco(gassources.clone()) {
            Ok(deco_runtime) => {
                let result = DecoResult {
                    success: true,
                    error: None,
                    decotable: serde_json::to_value(&deco_runtime).ok(),
                };
                serialize_to_c_string(&result)
            }
            Err(e) => {
                let result = DecoResult {
                    success: false,
                    error: Some(e.to_string()),
                    decotable: None,
                };
                serialize_to_c_string(&result)
            }
        }
    } else {
        std::ptr::null()
    }
}

#[no_mangle]
pub extern "C" fn GetCurrentNDL() -> *const c_char
{
    let dive_guard = DIVE.lock().unwrap();
    if let Some(dive) = dive_guard.as_ref() {
        let ndl = dive.ndl();
        serialize_to_c_string(&ndl)
    } else {
        std::ptr::null()
    }
}

#[no_mangle]
pub extern "C" fn GetCurrentCeiling() -> *const c_char
{
    let dive_guard = DIVE.lock().unwrap();
    if let Some(dive) = dive_guard.as_ref() {
        let ceiling = dive.ceiling();
        serialize_to_c_string(&ceiling)
    } else {
        std::ptr::null()
    }
}

#[no_mangle]
pub extern "C" fn GetSupersaturation() -> *const c_char
{
    let dive_guard = DIVE.lock().unwrap();
    if let Some(dive) = dive_guard.as_ref() {
        let supersaturation = dive.supersaturation();
        serialize_to_c_string(&supersaturation)
    } else {
        std::ptr::null()
    }
}

#[no_mangle]
pub extern "C" fn GetOtu() -> *const c_char
{
    let dive_guard = DIVE.lock().unwrap();
    if let Some(dive) = dive_guard.as_ref() {
        let otu = dive.otu();
        serialize_to_c_string(&otu)
    } else {
        std::ptr::null()
    }
}

#[no_mangle]
pub extern "C" fn GetCns() -> *const c_char
{
    let dive_guard = DIVE.lock().unwrap();
    if let Some(dive) = dive_guard.as_ref() {
        let cns = dive.cns();
        serialize_to_c_string(&cns)
    } else {
        std::ptr::null()
    }
}

#[no_mangle]
pub extern "C" fn GetCompleteDiveState() -> *const c_char
{
    let dive_guard = DIVE.lock().unwrap();
    if let Some(dive) = dive_guard.as_ref() {
        let gassources = BREATHING_SOURCES.lock().unwrap();
        
        let cns = dive.cns();
        let otu = dive.otu();
        let supersaturation = dive.supersaturation();
        let current_ceiling = dive.ceiling();
        let current_ndl = dive.ndl();
        
        let deco_result = match dive.deco(gassources.clone()) {
            Ok(deco_runtime) => {
                DecoResult {
                    success: true,
                    error: None,
                    decotable: serde_json::to_value(&deco_runtime).ok(),
                }
            }
            Err(e) => {
                DecoResult {
                    success: false,
                    error: Some(e.to_string()),
                    decotable: None,
                }
            }
        };
        
        let state = CompleteDiveState {
            cns: Some(cns),
            otu: Some(otu),
            supersaturation: serde_json::to_value(&supersaturation).ok(),
            current_ceiling: serde_json::to_value(&current_ceiling).ok(),
            current_ndl: serde_json::to_value(&current_ndl).ok(),
            deco_result: Some(deco_result),
        };
        
        serialize_to_c_string(&state)
    } else {
        std::ptr::null()
    }
}