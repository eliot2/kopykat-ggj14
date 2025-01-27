using UnityEngine;
using System.Collections;
using System.Collections.Generic;

namespace KopyKat
{
    public class MusicPlayer : MonoBehaviour
    {

        public List<AudioClip> Playlist;
        private AudioSource audioSource;
        public bool ShouldLoop = true;
        private IEnumerator currentTrack;

        // Use this for initialization
        void Start()
        {
            //start playing music, and keep it going
            if (Playlist != null)
            {
                currentTrack = Playlist.GetEnumerator();
                currentTrack.MoveNext();
                audioSource = GetComponent<AudioSource>();
                if (audioSource != null)
                {
                    audioSource.loop = ShouldLoop;
                    audioSource.clip = getCurrentTrack();
                    audioSource.Play();
                }
                else
                {
                    Debug.Log("MusicPlayer: couldn't find audio source!");
                    this.enabled = false;
                }
            }
        }

        // Update is called once per frame
        void Update()
        {
            //if the clip's not playing, we must have finished the song
            //go to the next song and play if we're not looping
            if (audioSource != null)
            {
                if (!audioSource.isPlaying && !ShouldLoop)
                {
                    getNextTrack();
                    audioSource.clip = getCurrentTrack();
                    audioSource.Play();
                }
                else if (!audioSource.isPlaying && ShouldLoop)
                {
                    audioSource.Play();
                }
            }
        }

        private AudioClip getCurrentTrack()
        {
            return (AudioClip)currentTrack.Current;
        }

        private void getNextTrack()
        {
            //try to go to the next track.
            //if we've moved past the end of the playlist...
            if (!currentTrack.MoveNext())
            {
                //go back to the start of the playlist
                currentTrack = Playlist.GetEnumerator();
                currentTrack.MoveNext();
            }
        }
    }
}