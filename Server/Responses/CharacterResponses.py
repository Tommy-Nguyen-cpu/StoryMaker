
from pydantic import BaseModel
from Server.Schemas.CharacterModel import CharacterModel
from Server.Schemas.CharacterModel import CharacterTalk

class CreateCharacterResponse(BaseModel):
    thinking_content: str
    # The 'character' field MUST be the nested object model
    character: CharacterModel

class CharacterTalkResponse(BaseModel):
    thinking_content: str
    # The 'character_talk' field MUST be the nested object model
    character_response: CharacterTalk