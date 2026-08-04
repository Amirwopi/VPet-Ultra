# AI Models

VPet-Ultra از مدل‌های GGUF (GGUF = GGML Universal Format) برای local inference استفاده می‌کند.

## Supported Models

| Model | Size (Q4_K_M) | RAM Needed | Vision | Recommended For |
|-------|-------------|------------|--------|-----------------|
| **Qwen2.5-3B-Instruct** | ~2.0 GB | 4 GB | ❌ | ✅ Best overall (CPU) |
| Qwen2.5-7B-Instruct | ~4.5 GB | 8 GB | ❌ | High quality (GPU) |
| Qwen2.5-14B-Instruct | ~8.5 GB | 16 GB | ❌ | Best quality (GPU) |
| Llama-3.2-3B-Instruct | ~2.0 GB | 4 GB | ❌ | Alternative (CPU) |
| Llama-3.2-1B-Instruct | ~0.8 GB | 2 GB | ❌ | Low-end hardware |
| SmolVLM-500M | ~0.5 GB | 2 GB | ✅ | Vision (low-end) |
| LLaVA-1.5-7B | ~4.5 GB | 8 GB | ✅ | Vision (GPU) |
| BGE-small-en-v1.5 | ~0.1 GB | 0.5 GB | ❌ | Embeddings only |
| Qwen2.5-VL-3B | ~2.0 GB | 4 GB | ✅ | Vision (CPU) |

## Download

### Qwen2.5-3B (Recommended)

```bash
# HuggingFace
wget https://huggingface.co/Qwen/Qwen2.5-3B-Instruct-GGUF/resolve/main/qwen2.5-3b-instruct-q4_k_m.gguf

# Place in models/ folder
mv qwen2.5-3b-instruct-q4_k_m.gguf models/
```

### Llama-3.2-3B

```bash
wget https://huggingface.co/meta-llama/Llama-3.2-3B-Instruct-GGUF/resolve/main/llama-3.2-3b-instruct-q4_k_m.gguf
mv llama-3.2-3b-instruct-q4_k_m.gguf models/
```

## Quantization

| Quant | Size | Quality | Speed |
|-------|------|---------|-------|
| Q4_K_M | ~50% | ✅ Good | ⚡ Fast |
| Q5_K_M | ~60% | ✅ Better | Normal |
| Q8_0 | ~80% | ✅ Best | 🐌 Slow |
| F16 | 100% | ✅ Full | 🐌 Slowest |

**توصیه:** `Q4_K_M` — بهترین تعادل بین کیفیت و سرعت.

## GPU vs CPU

| Mode | Setting | Speed | RAM |
|------|---------|-------|-----|
| CPU-only | `GpuLayerCount: 0` | ~5-15 tok/s | Model size + 2GB |
| GPU (CUDA) | `GpuLayerCount: 20+` | ~30-100 tok/s | VRAM: model size |

### GPU Requirements
- NVIDIA GPU با CUDA support
- CUDA toolkit 11.8 یا 12.x
- LLamaSharp CUDA backend DLLها کنار exe

## Model Directory Structure

```
models/
├── qwen2.5-3b-instruct-q4_k_m.gguf    # main model
├── ggml-base.bin                       # whisper STT model (optional)
├── en_US-amy-medium.onnx               # Piper TTS model (optional)
└── bge-small-en-v1.5.gguf              # embedding model (optional)
```

## Switching Models

1. مدل جدید را در `models/` قرار دهید
2. `appsettings.json` را ویرایش کنید: `"Model": "new-model-name"`
3. VPet را restart کنید

## Online Alternative

اگر سخت‌افزار کافی ندارید، از Ollama استفاده کنید:

```bash
# Install Ollama
curl -fsSL https://ollama.com/install.sh | sh

# Pull model
ollama pull qwen2.5:3b

# Configure appsettings.json
# "Provider": "online", "Endpoint": "http://localhost:11434"
```