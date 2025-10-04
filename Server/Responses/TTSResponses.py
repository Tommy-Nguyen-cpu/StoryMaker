from pydantic import BaseModel

class GetAvailableVoicesResponse(BaseModel):
    male_voices: list[str]
    female_voices: list[str]