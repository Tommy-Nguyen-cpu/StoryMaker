from fastapi import FastAPI
from typing import Dict

app = FastAPI()

@app.post("/create_story", response_model=Dict[str, str])
def create_story(request : Dict[str, str]):
    
    return {"status": "Library deleted successfully"}