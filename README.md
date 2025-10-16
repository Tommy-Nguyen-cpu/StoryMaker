# StoryMaker
<img width="1536" height="1024" alt="StoryMaker Thumbnail" src="https://github.com/user-attachments/assets/532f9e15-c487-45a4-b7b6-cf6dc4f09b8c" />

## Description
StoryMaker is an AI-powered app that creates full stories or episodes from a user prompt. Inspired by CodeBullet’s video on generating Rick & Morty episodes with AI, this was a fun side project that grew into something surprisingly entertaining.

The backend is built in Python, handling all AI interactions and connecting with language models from HuggingFace. The Unity frontend brings everything to life—animating characters, switching scenes, and managing the visuals.

It was a blast to build, and I’ve gotten some hilarious results from it! I’ll share a link to the YouTube soon, so I hope you look forward to it!!

## Running
In order to run the application, you must first create a python virtual environment for the server via:
```
python -m venv serviceEnv
```
Then activate the environment, and pip install the requirements specified in requirements.txt:
```
pip install -r requirements.txt
```

Lastly, start the server by running the following command:
```
uvicorn Server.AI.ai_service:app --port 8000
```

Once the server has started, we can start running our Unity app. Open the Unity application in the UI folder.
