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
    public AudioClip theme;
    private int currentTrack = -1;
    public int desiredTrack;
    public float mapHeight = 300;

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
        desiredTrack = Mathf.FloorToInt(transform.position.y / mapHeight * musicClips.Length);
        if (transform.position.x < 175 && musicSource.clip != theme)
        {
            musicSource.clip = theme;
        }
;
        if (currentTrack != desiredTrack && transform.position.x > 175) {
            musicSource.clip = musicClips[desiredTrack];
            currentTrack = desiredTrack;
            }
        
        if (!musicSource.isPlaying) {
                musicSource.Play();
            }
        }
    }

