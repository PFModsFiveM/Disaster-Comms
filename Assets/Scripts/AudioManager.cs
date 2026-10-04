using UnityEngine;

public class AudioManager : MonoBehaviour
{
    [Header("UI")]
    public AudioClip uiClick;

    [Header("Radio")]
    public AudioClip radioStatic;
    public AudioClip newCommunication;

    [Header("Answers")]
    public AudioClip correctAnswer;
    public AudioClip incorrectAnswer;

    [Header("Reactor")]
    [Tooltip("Loops while the reactor is critical.")]
    public AudioClip warningAlarm;
    public AudioClip explosion;

    [Header("Volumes")]
    [Range(0f, 1f)] public float oneShotVolume = 1f;
    [Range(0f, 1f)] public float alarmVolume = 0.6f;

    AudioSource oneShotSource;
    AudioSource alarmSource;

    bool alarmOn;

    void Awake()
    {
        oneShotSource = gameObject.AddComponent<AudioSource>();
        oneShotSource.playOnAwake = false;

        alarmSource = gameObject.AddComponent<AudioSource>();
        alarmSource.playOnAwake = false;
        alarmSource.loop = true;
    }

    void OnDisable()
    {
        SetAlarm(false);
    }

    public void PlayClick() => PlayOneShot(uiClick);
    public void PlayRadioStatic() => PlayOneShot(radioStatic);
    public void PlayNewCommunication() => PlayOneShot(newCommunication);
    public void PlayCorrect() => PlayOneShot(correctAnswer);
    public void PlayIncorrect() => PlayOneShot(incorrectAnswer);
    public void PlayExplosion() => PlayOneShot(explosion);

    public void SetAlarm(bool on)
    {
        if (on == alarmOn) return;
        alarmOn = on;

        if (alarmSource == null) return;

        if (on)
        {
            if (warningAlarm == null) return;
            alarmSource.clip = warningAlarm;
            alarmSource.volume = alarmVolume;
            alarmSource.Play();
        }
        else
        {
            alarmSource.Stop();
        }
    }

    public void StopAll()
    {
        SetAlarm(false);
        if (oneShotSource != null) oneShotSource.Stop();
    }

    void PlayOneShot(AudioClip clip)
    {
        if (clip == null || oneShotSource == null) return;
        oneShotSource.PlayOneShot(clip, oneShotVolume);
    }
}
