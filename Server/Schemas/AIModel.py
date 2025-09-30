class BaseAiModel():
    def __init__(self, model_name: str):
        self.model_name = model_name
    def generate_text(self, prompt: str, instructions: str) -> str:
        raise NotImplementedError("This method should be overridden by subclasses.")