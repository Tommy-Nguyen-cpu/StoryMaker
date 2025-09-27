from fastapi import FastAPI
from typing import Dict

from Server.Schemas.HuggingfaceModel import HuggingfaceModel

app = FastAPI()

@app.post("/create_story", response_model=Dict[str, str])
def create_story(request : Dict[str, str]):

    print("Creating story with request:", request)
    HuggingfaceModelInstance = HuggingfaceModel("Qwen/Qwen3-4B-Instruct-2507")
    story = HuggingfaceModelInstance.generate_text(request['prompt'])
    return {"story": story}