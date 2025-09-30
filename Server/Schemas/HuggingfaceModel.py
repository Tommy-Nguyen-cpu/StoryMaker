from transformers import AutoModelForCausalLM, AutoTokenizer
from .AIModel import BaseAiModel

class HuggingfaceModel(BaseAiModel):
    def __init__(self, model_name: str):
        super().__init__(model_name)
        self.tokenizer = AutoTokenizer.from_pretrained(model_name)
        self.model = AutoModelForCausalLM.from_pretrained(model_name, dtype="auto", device_map="auto")

    def generate_text(self, prompt: str, instructions: str) -> str:
        messages = [
            {"role": "system", "content": instructions},
            {"role": "user", "content": prompt}
        ]
        text = self.tokenizer.apply_chat_template(
            messages,
            tokenize=False,
            add_generation_prompt=True,
        )

        print("Tokenizing prompt...")
        inputs = self.tokenizer([text], return_tensors="pt").to(self.model.device)

        print("Generating text...")
        outputs = self.model.generate(**inputs, max_new_tokens=262144)

        print("Decoding output...")
        return self.tokenizer.decode(outputs[0][len(inputs.input_ids[0]):].tolist(), skip_special_tokens=True)