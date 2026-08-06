use serde::Serialize;
use std::path::PathBuf;
use transcribe_cpp::{
    init_backends_default, init_logging, Backend, Model, ModelOptions, RunOptions, Task,
};

#[derive(Serialize)]
struct TranscribeResult {
    text: String,
    model: String,
    audio_secs: f64,
    load_ms: u64,
    transcribe_ms: u64,
}

fn main() -> anyhow::Result<()> {
    // Parse args: vpet-transcribe <model_path> <wav_path> [language]
    let args: Vec<String> = std::env::args().collect();
    if args.len() < 3 {
        eprintln!("Usage: vpet-transcribe <model_path> <wav_path> [language] [--json]");
        eprintln!("  model_path  Path to GGUF/ggml whisper model file");
        eprintln!("  wav_path    Path to 16kHz mono 16-bit PCM WAV file");
        eprintln!("  language    Language code (e.g. 'fa', 'en', 'auto'). Default: auto");
        eprintln!("  --json      Output JSON result on stdout");
        std::process::exit(2);
    }

    let model_path = PathBuf::from(&args[1]);
    let wav_path = PathBuf::from(&args[2]);
    let language = args.get(3).filter(|s| !s.starts_with("--")).cloned();
    let json_output = args.iter().any(|a| a == "--json");

    if !model_path.exists() {
        eprintln!("error: model file not found: {}", model_path.display());
        std::process::exit(2);
    }
    if !wav_path.exists() {
        eprintln!("error: wav file not found: {}", wav_path.display());
        std::process::exit(2);
    }

    // Validate WAV format (16kHz mono 16-bit PCM)
    let reader = hound::WavReader::open(&wav_path)?;
    let spec = reader.spec();
    if spec.sample_rate != 16_000 || spec.channels != 1 || spec.bits_per_sample != 16 {
        eprintln!(
            "error: expected 16kHz mono 16-bit PCM WAV, got {}Hz / {}ch / {}bit",
            spec.sample_rate, spec.channels, spec.bits_per_sample
        );
        std::process::exit(2);
    }

    // Read samples as f32
    let samples: Vec<f32> = reader
        .into_samples::<i16>()
        .filter_map(|s| s.ok())
        .map(|s| s as f32 / 32768.0)
        .collect();
    let audio_secs = samples.len() as f64 / 16000.0;

    // Initialize transcribe-cpp backends (loads CPU/GPU modules)
    init_logging();
    init_backends_default()?;

    // Load model
    let load_start = std::time::Instant::now();
    let model_options = ModelOptions {
        backend: Backend::Auto,
        gpu_device: 0,
    };
    let model = Model::load_with(&model_path, &model_options)?;
    let bound_backend = model.backend();
    let mut session = model.session()?;
    let load_ms = load_start.elapsed().as_millis() as u64;

    eprintln!(
        "Loaded model '{}' on backend '{}', load_ms={}",
        model_path.display(),
        bound_backend,
        load_ms
    );

    // Transcribe
    let task = Task::Transcribe;
    let run_options = RunOptions {
        task,
        language: language.clone(),
        target_language: None,
        family: None,
        ..Default::default()
    };

    let transcribe_start = std::time::Instant::now();
    let transcript = session.run(&samples, &run_options)?;
    let transcribe_ms = transcribe_start.elapsed().as_millis() as u64;
    let text = transcript.text;

    let result = TranscribeResult {
        text: text.clone(),
        model: model_path.display().to_string(),
        audio_secs,
        load_ms,
        transcribe_ms,
    };

    if json_output {
        let json = serde_json::to_string(&result)?;
        println!("{}", json);
    } else {
        println!("text: {}", text);
        println!(
            "model={} backend={} audio={:.2}s load={}ms transcribe={}ms",
            model_path.display(),
            bound_backend,
            audio_secs,
            load_ms,
            transcribe_ms
        );
    }

    Ok(())
}
