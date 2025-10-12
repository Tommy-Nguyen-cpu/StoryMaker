<img width="1536" height="1024" alt="StoryMaker Thumbnail" src="https://github.com/user-attachments/assets/532f9e15-c487-45a4-b7b6-cf6dc4f09b8c" />


# StoryMaker
Was inspired by CodeBullet's video where an AI directed a Rick and Morty episode. CodeBullet previously failed at creating a continuous conversation via multiple agents. This likely stems from a lack of context. For fun, I decided to tackle it myself and see how far I can go. Got a simple looking up going, and the loop works!

# Running
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
