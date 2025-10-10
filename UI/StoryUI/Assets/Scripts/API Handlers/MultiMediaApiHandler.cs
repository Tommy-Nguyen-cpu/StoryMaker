using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Audio;
using UnityEngine.Networking;

public class MultiMediaApiHandler
{
    #region Singleton
    private static MultiMediaApiHandler instance = new MultiMediaApiHandler();
    public static MultiMediaApiHandler Instance { get { return instance; } }

    private MultiMediaApiHandler() { }
    #endregion


    public IEnumerator PlayFromUrl(string url, AudioSource audioSource)
    {
        using (UnityWebRequest www = UnityWebRequestMultimedia.GetAudioClip(url, AudioType.WAV))
        {
            // Enable streaming if supported
            DownloadHandlerAudioClip dh = (DownloadHandlerAudioClip)www.downloadHandler;
            dh.streamAudio = true;

            yield return www.SendWebRequest();

            if (www.result != UnityWebRequest.Result.Success)
            {
                Debug.LogError("Audio download error: " + www.error);
            }
            else
            {
                AudioClip clip = DownloadHandlerAudioClip.GetContent(www);
                audioSource.clip = clip;
            }
        }
    }
}
