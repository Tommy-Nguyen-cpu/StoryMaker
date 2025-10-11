from fastapi import FastAPI, HTTPException
from fastapi.responses import StreamingResponse
from typing import Dict
import re
import json
import io
import wave
import numpy as np

from Server.Schemas.HuggingfaceModel import HuggingfaceModel
from Server.Schemas.TTSModel import KittenTTSModel
from Server.Schemas.AIModel import BaseAiModel
from Server.Responses.CharacterResponses import CreateCharacterResponse, CharacterTalkResponse
from Server.Responses.TTSResponses import GetAvailableVoicesResponse

app = FastAPI()

# AI Models
llmInstance : BaseAiModel = None
ttsInstance : BaseAiModel = None

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
    print("Loading llm model...")
    global llmInstance
    llmInstance = HuggingfaceModel("Qwen/Qwen3-0.6B")

    print("Loading tts model...")
    global ttsInstance
    ttsInstance = KittenTTSModel("KittenML/kitten-tts-nano-0.2")

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


@app.post("/create_character", response_model=CreateCharacterResponse)
def create_character(request : Dict[str, str]):
    if 'story_description' not in request:
        raise HTTPException(status_code=400, detail="Missing 'story_description' in request body")
    if "existing_characters" not in request:
        request["existing_characters"] = "N/A"

    print("Creating character with request:", request)
    instructions = ROLE + ''' Create a character alongside their name, description, gender (must be male OR female), and personality so that they fit into the story description provided. Avoid creating characters specified in the "Existing Characters" section. If no characters exist yet, create the first character.
Your response must be in the format:
{"name": "<character_name>", "description": "<character_description>", "gender": "<character_gender>", "personality": "<character_personality>" }

You must strictly follow this format without any additional text or explanation.
'''

    prompt = f"Story Description: {request['story_description']}\nExisting Characters: {request['existing_characters']}"
    character = llmInstance.generate_text(prompt, instructions)

    thinking_content, character = clean_ai_response(character)
    print("Generated character:", character)

    return {"thinking_content": thinking_content, "character": json.loads(character)}

@app.get("/get_character_talk", response_model=CharacterTalkResponse)
def get_character_talk(request : Dict[str, str]):
    if 'additional_notes' not in request:
        request["additional_notes"] = "N/A"
    if 'character' not in request:
        raise HTTPException(status_code=400, detail="Missing 'character' in request body")
    if 'story_description' not in request:
        raise HTTPException(status_code=400, detail="Missing 'story_description' in request body")
    if "personality" not in request:
        request["personality"] = "N/A"
    if "available_actions" not in request:
        request["available_actions"] = "N/A"
    if "conversation_history" not in request:
        request["conversation_history"] = "N/A"
        request["last_line"] = "N/A"
    else:
        conv_history = request["conversation_history"].split(",")[-1]
        request["last_line"] = conv_history

    print("Creating character talk with request:", request)
    instructions = ROLE + ''' Based on the user's notes, character, and story description, write a natural spoken line of dialogue representing what the character says aloud that fits the character`s personality, the story description, and the conversation context. If no characters have spoken yet, start the conversation.
Your response must be for the character specified in the request and no other characters.
Use the conversation context specified in the context section for what has already been said.
Provide unique responses that continue the conversation based on the context. Do not repeat or stray off-topic.
If the character takes an action, include it in the "action" field. If no action is taken, leave the "action" field empty. Only include actions available in the "Available Actions" section.
Your response must be in the format:
{"character": "<character_name>", "response": "<character_response>", "action": "<character_action>" }

You must strictly follow this format without any additional text or explanation.

For example:
if "Available Actions" is "turn left, turn right, walk straight, smile brightly",
{"character": "Alice", "response": "I can't believe we made it this far!", "action": "smiles brightly"}
'''
    prompt = f"Story Description: {request['story_description']}\n\nContext:{request['conversation_history']}\n\nPrevious line to reply to: {request["last_line"]}\n\nCharacter: {request['character']}\n\nPersonality: {request['personality']}\n\nAvailable Actions: {request['available_actions']}\n\nUser Additional Notes: {request['additional_notes']}"
    character_response = llmInstance.generate_text(prompt, instructions)

    thinking_content, character_response = clean_ai_response(character_response)
    print("Generated story:", character_response)
    return {"thinking_content": thinking_content, "character_response": json.loads(character_response)}

@app.get("/get_available_voices", response_model=GetAvailableVoicesResponse)
def get_available_voices():
    print("Fetching available voices...")
    voices = ttsInstance.get_available_voices()
    male = [v for v in voices if v.endswith('-m')]
    female = [v for v in voices if v.endswith('-f')]

    return GetAvailableVoicesResponse(male_voices=male, female_voices=female)

@app.get("/tts")
async def tts_endpoint(text: str, voice: str = "expr-voice-2-f"):
    audio = ttsInstance.generate_audio(text, voice=voice)

    # Convert float32 audio in [-1, +1] to int16 PCM
    # Clip just in case
    arr = np.asarray(audio, dtype=np.float32)
    arr = np.clip(arr, -1.0, 1.0)
    # Scale to int16 range
    pcm = (arr * 32767.0).astype(np.int16)

    buf = io.BytesIO()
    with wave.open(buf, mode="wb") as wf:
        wf.setnchannels(1)                # mono
        wf.setsampwidth(2)               # 2 bytes = 16 bits
        wf.setframerate(24000)           # **important**: match 24000 Hz
        wf.writeframes(pcm.tobytes())
    buf.seek(0)

    return StreamingResponse(buf, media_type="audio/wav")

