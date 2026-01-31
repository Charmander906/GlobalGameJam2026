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
    public GameObject player;
    public int numberOfMusicClips;
    private AudioClip[] musicClips;
    public float mapHeight;
    private int currentTrack = -1;
    public int desiredTrack;
    private float playerStartingY;

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        playerStartingY = player.transform.position.y;
        musicClips = new AudioClip[numberOfMusicClips];
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
        desiredTrack = Mathf.FloorToInt((player.transform.position.y - playerStartingY) * numberOfMusicClips / mapHeight); 
        if (desiredTrack < 0) desiredTrack = 0;
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
