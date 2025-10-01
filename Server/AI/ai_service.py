from fastapi import FastAPI, HTTPException
from typing import Dict
import re

from Server.Schemas.HuggingfaceModel import HuggingfaceModel
from Server.Schemas.AIModel import BaseAiModel

app = FastAPI()

# AI Models
llmInstance : BaseAiModel = None

# Constants
ROLE = "You are a creative and imaginative story writer."

# Helpers
def extract_thinking_content(text: str) -> str:
    """
    Extracts content within <think>...</think> tags from the given text.
    If no such tags are found, returns an empty string.
    """
    match = re.search(r"(<think\b[^>]*>.*?</think>)", text, re.DOTALL)
    if match:
        return match.group(1).strip()
    return ""

def ensure_curly_braces(s: str) -> str:
    s = s.strip()  # remove accidental leading/trailing spaces/newlines
    
    if not s.startswith("{"):
        s = "{" + s
    if not s.endswith("}"):
        s = s + "}"
    
    return s

def clean_ai_response(response: str) -> str:
    """
    Cleans the AI response by removing any unwanted prefixes or suffixes.
    This can be customized based on the specific patterns observed in the responses.
    """
    # Example: Remove any leading/trailing whitespace and unwanted characters
    print("Extracting thinking content...", )
    thinking_content = extract_thinking_content(response)

    cleaned = response.replace(thinking_content, "").strip()
    cleaned = ensure_curly_braces(cleaned)
    return thinking_content, cleaned.strip()

# API Endpoints
@app.on_event("startup")
async def startup_event():
    # Code here runs once at startup
    print("App is starting up...")
    global llmInstance
    llmInstance = HuggingfaceModel("Qwen/Qwen3-0.6B")
    # e.g., connect to DB, preload models, init resources, etc.
    print("App startup complete.")

@app.post("/enhance_story_prompt", response_model=Dict[str, str])
def enhance_story_prompt(request : Dict[str, str]):
    if 'prompt' not in request:
        raise HTTPException(status_code=400, detail="Missing 'prompt' in request body")

    print("Enhancing story prompt with request:", request)
    instructions = f"{ROLE} Create an engaging and vivid story synopsis based on the user's prompt."
    enhanced = llmInstance.generate_text(request['prompt'], instructions)

    thinking_content, enhanced = clean_ai_response(enhanced)

    print("Enhanced story prompt:", enhanced)


    return {"thinking_content": thinking_content,"enhanced_description": enhanced}


@app.post("/create_character", response_model=Dict[str, str])
def create_character(request : Dict[str, str]):
    if 'story_description' not in request:
        raise HTTPException(status_code=400, detail="Missing 'story_description' in request body")
    if "existing_characters" not in request:
        request["existing_characters"] = "N/A"

    print("Creating character with request:", request)
    instructions = ROLE + ''' Create a character alongside their name, description, gender, and personality so that they fit into the story description provided. Avoid creating characters that already exist in the story. If no characters exist yet, create the first character.
Your response must be in the format:
{"name": "<character_name>", "description": "<character_description>", "gender": "<character_gender>", "personality": "<character_personality>" }

You must strictly follow this format without any additional text or explanation.
'''

    prompt = f"Story Description: {request['story_description']}\nExisting Characters: {request['existing_characters']}"
    character = llmInstance.generate_text(prompt, instructions)

    thinking_content, character = clean_ai_response(character)
    print("Generated character:", character)

    return {"thinking_content": thinking_content, "character": character}

@app.post("/get_character_talk", response_model=Dict[str, str])
def get_character_talk(request : Dict[str, str]):
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

    print("Creating character talk with request:", request)
    instructions = f'''
{ROLE} Based on the user's prompt, character, and story description, create an engaging response that fits the character's personality and the story context. If no characters have spoken yet, start the conversation.
Your response must be for the character specified in the request and no other characters.
Your response must be in the format:
{"character": "<character_name>", "response": "<character_response>"}

You must strictly follow this format without any additional text or explanation.
'''
    prompt = f"Story Description: {request['story_description']}\nConversation History:{request['conversation_history']}\nCharacter: {request['character']}\nPersonality: {request['personality']}\nUser Prompt: {request['prompt']}"
    character_response = llmInstance.generate_text(prompt, instructions)

    thinking_content, character_response = clean_ai_response(character_response)
    print("Generated story:", character_response)
    return {"thinking_content": thinking_content, "character_response": character_response}