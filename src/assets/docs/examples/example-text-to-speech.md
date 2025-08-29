# 📢 Text To Speech Example

This example demonstrates how to use the TextToSpeech feature of MaIN.NET. In this case the example shows how to setup a simple speech generation using Kokoro model with preset voices.

## 🚀 Quick Start

To run the example you need the Kokoro TTS model downloaded. The model must be in <b>ONNX</b> format. 
<br>It can be acquired here: <a href="https://github.com/taylorchu/kokoro-onnx/releases/download/v0.2.0/kokoro.onnx">GET KOKORO</a>. <br>Voices can be downloaded from original Kokoro repository on HuggingFace <a href="https://huggingface.co/onnx-community/Kokoro-82M-v1.0-ONNX/tree/main/voices">GET VOICES</a>.
<br>Model needs to be placed in set Models directory. Voices can be stored wherever but their path <b>MUST</b> be set in the example code.

⚠️⚠️⚠️ <b>IMPORTANT</b> ⚠️⚠️⚠️
<br>TTS feature is in an ongoing development. Some changes will be refactored in near future.
<br>Some approaches will be changed completely to match desired MaIN approach. (<i>VoiceService</i> is great example)

### 📝 Code Example
```csharp
public class ChatWithTextToSpeechExample : IExample
{
    private const string VoicePath = "<your-path-to-voices>";
    
    public async Task Start()
    {
        Console.WriteLine("ChatWithTextToSpeech is running! Put on your headphones and press any key.");
        Console.ReadKey();
        
        VoiceService.SetVoicesPath(VoicePath);
        var voice = VoiceService.GetVoice("af_heart")
            .MixWith(VoiceService.GetVoice("bf_emma"));
        
        await AIHub.Chat().WithModel("gemma2:2b")
            .WithMessage("Generate a 4 sentence poem.")
            .Speak(new TextToSpeechParams("kokoro:82m", voice, true))
            .CompleteAsync(interactive: true);

        Console.WriteLine("Done!");
        Console.ReadKey();
    }
}
```

## 🔹 How It Works

1. **Set Voices Path** → required for a time being. Sets manually directory where voice files are stored
2. **Voice Service** → static utility class. Works as a temporary bridge make certain features possible. It is mainly used for `GetVoice()` voice loading and `MixWith()` extension method that allows for voice mixing*
3. **Speak** → core of the TTS functionality. Vocalizes each message returned by model. In this case a 4 sentence poem. Requires `TextToSpeechParams` parameters which are essentially all <i>"moving parts"</i> of TTS. It consists of 3 parameters: 
- `model` - model name. Similar to how `WithModel()` parameter
- `voice` - `Voice` class loaded in previous step
- `playback` - a boolean that specifies whether generated audio should be played back to via system audio driver. This parameter is optional and defaults to `false`.
<br>Generated TTS audio (apart from the optional playback) will be stored in `Message` class, in `Speech` byte array property.

## 📋 Prerequisites

- Kokoro model and voices downloaded
- Any audio device present
- MaIN.NET framework properly configured

*This feature as well as parts of TTS code were heavily inspired by Lyrcaxis project called <a href="https://github.com/Lyrcaxis/KokoroSharp">KokoroSharp</a>. Please check their work and give them a ⭐