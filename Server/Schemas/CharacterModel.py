from pydantic import BaseModel

class CharacterModel(BaseModel):
    name: str
    description: str
    gender: str
    personality: str

# {"character": "<character_name>", "response": "<character_response>"}
class CharacterTalk(BaseModel):
    character: str
    response: str