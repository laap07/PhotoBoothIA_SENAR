using UnityEngine;

public class BlinkPhoto : MonoBehaviour
{
    public Animation anim;
    public AudioSource asc;

    public void Ativar()
    {
        anim.Play();
        asc.Play();
    }
    public void AutoDisable()
    {
        this.gameObject.SetActive(false);
    }
}
