from fastapi import FastAPI
from typing import Dict

from Server.Schemas.HuggingfaceModel import HuggingfaceModel
from Server.Schemas.AIModel import BaseAiModel

app = FastAPI()

# AI Models
llmInstance : BaseAiModel = None

@app.on_event("startup")
async def startup_event():
    # Code here runs once at startup
    print("App is starting up...")
    global llmInstance
    llmInstance = HuggingfaceModel("Qwen/Qwen3-4B-Instruct-2507")
    # e.g., connect to DB, preload models, init resources, etc.

@app.post("/create_story", response_model=Dict[str, str])
def create_story(request : Dict[str, str]):

    print("Creating story with request:", request)
    story = llmInstance.generate_text(request['prompt'])
    return {"story": story}