public static class Constants
{
    public const string EnhanceDescApi = "/enhance_story_prompt";
    public const string GenerateCharacterResponseApi = "/get_character_talk";
    public const string CreateCharacterApi = "/create_character";
    public const string GetAvailableVoicesApi = "/get_available_voices";
    public const string ttsApi = "/tts";

    public const int MAX_RETRY = 5;
    public const float Timeout = 10f;
    public const int MaxConvLength = 20;

    // TODO: Will add more actions later on, but "Move to" is the only action that requires a character.
    public const string MoveToAction = "Move to ";

    public enum AnimationTrigger
    {
        IsWalking, IsTalking, IsDancing
    }
}
