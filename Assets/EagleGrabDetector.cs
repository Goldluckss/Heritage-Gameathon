using UnityEngine;

public class EagleGrabDetector : MonoBehaviour
{
    public CharacterSwitcher characterSwitcher;

    void OnCollisionEnter(Collision collision)
    {
        if (characterSwitcher != null)
        {
            characterSwitcher.OnEagleCollision(collision);
        }
    }
}