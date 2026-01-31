using System.Collections;
using UnityEngine;

public class AudioManager : MonoBehaviour
{
    public AudioSource musicSource;
    public AudioSource SFXSource;

    public AudioClip music1;
    public AudioClip music2;
    public AudioClip music3;
    public AudioClip music4;
    public AudioClip music5;
    public AudioClip music6;
    private AudioClip[] musicClips = new AudioClip[6];

    private int currentTrack = -1;
    public int desiredTrack;

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        musicClips[0] = music1;
        musicClips[1] = music2;
        musicClips[2] = music3;
        musicClips[3] = music4;
        musicClips[4] = music5;
        musicClips[5] = music6;
    }

    // Update is called once per frame
    void Update()
    {
        if(currentTrack != desiredTrack) {
              musicSource.clip = musicClips[desiredTrack];
              currentTrack = desiredTrack;
        }
        
        if (!musicSource.isPlaying)
        {
            musicSource.Play();
        }
    }
}
