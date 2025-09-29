from transformers import AutoModelForCausalLM, AutoTokenizer
from .AIModel import BaseAiModel

class HuggingfaceModel(BaseAiModel):
    def __init__(self, model_name: str):
        super().__init__(model_name)
        self.tokenizer = AutoTokenizer.from_pretrained(model_name)
        self.model = AutoModelForCausalLM.from_pretrained(model_name)

    def generate_text(self, prompt: str) -> str:
        print("Tokenizing prompt...")
        inputs = self.tokenizer(prompt, return_tensors="pt")

        print("Generating text...")
        outputs = self.model.generate(**inputs)

        print("Decoding output...")
        return self.tokenizer.decode(outputs[0], skip_special_tokens=True)