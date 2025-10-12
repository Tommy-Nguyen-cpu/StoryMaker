using UnityEngine;

public static class CharacterFactory
{
    public static GameObject SetUpPrimitiveCharacter(GameObject instantiatedGameObj, Character charInfo, string voice)
    {
        var movementScript = instantiatedGameObj.GetComponent<AiCharacterController>();
        movementScript.CharacterInfo = charInfo;
        movementScript.CharacterVoice = voice;

        // Get the Renderer component
        Renderer renderer = instantiatedGameObj.GetComponent<Renderer>();
        renderer.material.color = new Color(Random.Range(0.0f, 1.0f), Random.Range(0.0f, 1.0f), Random.Range(0.0f, 1.0f));

        return instantiatedGameObj;
    }
}
