using UnityEngine;

public class FadeTransition : MonoBehaviour
{
    public GameManager gameManager;

    public void AdvanceScreen()
    {
        gameManager.AdvanceAnimation();
    }
}
