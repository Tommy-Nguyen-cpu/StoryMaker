using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public static class CharacterFactory
{
    public static GameObject CreatePrimitiveCharacter(Color color, Vector3 position, Vector3 scale)
    {
        GameObject cyl = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
        cyl.transform.position = position;
        cyl.transform.localScale = scale;

        Renderer rend = cyl.GetComponent<Renderer>();
        if (rend != null)
        {
            Material mat = new Material(rend.sharedMaterial);
            if (mat.HasProperty("_Color"))
                mat.SetColor("_Color", color);
            else if (mat.HasProperty("_BaseColor"))
                mat.SetColor("_BaseColor", color);
            rend.material = mat;
        }

        // Add necessary components to AI asset.
        cyl.AddComponent<AiCharacterController>();
        cyl.AddComponent<AudioSource>();

        return cyl;
    }
}
