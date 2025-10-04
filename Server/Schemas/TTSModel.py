from Server.Schemas.AIModel import BaseAiModel
from kittentts import KittenTTS

class KittenTTSModel(BaseAiModel):
    def __init__(self, model_name: str):
        super().__init__(model_name)
        self.tts_model = KittenTTS(model_name)

    def generate_text(self, prompt: str, instructions: str) -> str:
        # This model does not generate text, so we return an empty string or a placeholder
        raise NotImplementedError("This model is for TTS and does not support text generation.")
    
    def generate_audio(self, text: str, voice: str = 'expr-voice-2-f'):
        print("Generating audio...")
        audio = self.tts_model.generate(text, voice=voice)
        return audio
    
    def get_available_voices(self):
        return self.tts_model.available_voices
