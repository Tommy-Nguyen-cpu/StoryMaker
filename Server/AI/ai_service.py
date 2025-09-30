from fastapi import FastAPI, HTTPException
from typing import Dict

from Server.Schemas.HuggingfaceModel import HuggingfaceModel
from Server.Schemas.AIModel import BaseAiModel

app = FastAPI()

# AI Models
llmInstance : BaseAiModel = None

# Constants
ROLE = "You are a creative and imaginative story writer."

@app.on_event("startup")
async def startup_event():
    # Code here runs once at startup
    print("App is starting up...")
    global llmInstance
    llmInstance = HuggingfaceModel("Qwen/Qwen3-4B-Instruct-2507")
    # e.g., connect to DB, preload models, init resources, etc.
    print("App startup complete.")

@app.post("/enhance_story_prompt", response_model=Dict[str, str])
def enhance_story_prompt(request : Dict[str, str]):
    if 'prompt' not in request:
        raise HTTPException(status_code=400, detail="Missing 'prompt' in request body")

    print("Enhancing story prompt with request:", request)
    instructions = f"{ROLE} Create an engaging and vivid story description based on the user's prompt."
    enhanced = llmInstance.generate_text(request['prompt'], instructions)
    print("Enhanced story prompt:", enhanced)
    return {"enhanced_description": enhanced}


@app.post("/create_character", response_model=Dict[str, str])
def create_character(request : Dict[str, str]):
    if 'story_description' not in request:
        raise HTTPException(status_code=400, detail="Missing 'story_description' in request body")

    print("Creating character with request:", request)
    instructions = ROLE + ''' Create a character alongside their name, description, gender, and personality so that they fit into the story description provided.
Your response must be in the format:
{"name": "<character_name>", "description": "<character_description>", "gender": "<character_gender>", "personality": "<character_personality>" }

You must strictly follow this format without any additional text or explanation.
'''

    prompt = f"Story Description: {request['story_description']}"
    character = llmInstance.generate_text(prompt, instructions)
    print("Generated character:", character)
    return {"character": character}

@app.post("/character_response", response_model=Dict[str, str])
def character_response(request : Dict[str, str]):
    if 'prompt' not in request:
        raise HTTPException(status_code=400, detail="Missing 'prompt' in request body")
    if 'character' not in request:
        raise HTTPException(status_code=400, detail="Missing 'character' in request body")
    if 'story_description' not in request:
        raise HTTPException(status_code=400, detail="Missing 'story_description' in request body")
    if "personality" not in request:
        request["personality"] = "N/A"
    if "conversation_history" not in request:
        request["conversation_history"] = "N/A"

    print("Creating story with request:", request)
    instructions = f'''
{ROLE} Based on the user's prompt, character, and story description, create an engaging response that fits the character's personality and the story context. If no characters have spoken yet, start the conversation.
Your response must be for the character specified in the request and no other characters.
Your response must be in the format:
{"character": "<character_name>", "response": "<character_response>"}

You must strictly follow this format without any additional text or explanation.
'''
    prompt = f"Story Description: {request['story_description']}\nConversation History:{request['conversation_history']}\nCharacter: {request['character']}\nPersonality: {request['personality']}\nUser Prompt: {request['prompt']}"
    enhanced = llmInstance.generate_text(prompt, instructions)
    print("Generated story:", enhanced)
    return {"enhanced_description": enhanced}