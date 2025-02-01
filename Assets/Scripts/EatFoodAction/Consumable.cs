using UnityEngine;

public class Consumable : MonoBehaviour
{
    [SerializeField] GameObject[] portions;
    [SerializeField] int index = 0;

    
    /*
    찾아보니 Expression-bodied property
    public bool IsFinished
    {
        get { return index == portions.Length; }
    }
    ReadOnly 느낌
    */
    public bool IsFinished => index == portions.Length;

    AudioSource _audioSource;
    
    void Start()
    {
        _audioSource = GetComponent<AudioSource>();
        _audioSource.playOnAwake = false;
        SetVisuals();
    }
    
    //인스펙터 안에서 값들 변경하면 실행. 
    void OnValidate()
    {
        SetVisuals();
    }

    [ContextMenu("Consume")]
    public void Consume()
    {
        if (!IsFinished)
        {
            index++;
            SetVisuals();
            _audioSource.Play();
        }
    }

    void SetVisuals()
    {
        for (int i = 0; i < portions.Length; i++)
        {
            portions[i].SetActive(i == index);
        }
    }
}
