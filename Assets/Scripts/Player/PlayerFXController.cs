using EchoMage.Core;
using UnityEngine;

public class PlayerFXController : MonoBehaviour
{
    [SerializeField] private AfterImageController afterImageController;
    [SerializeField] private GameObject visual;

    private void Awake()
    {
        afterImageController = GameManager.Instance.AfterImageController;
        if (afterImageController != null)
        {
            afterImageController.SetRoot(visual);
            afterImageController.SetOrigin(this.transform);
            afterImageController.enabled = true;
        }
    }

    public void SwitchMode(bool State)
    {
        if (State == true)
        {
            afterImageController.SwitchModeAlways();
        }
        else
        {
            afterImageController.SwitchModeCommand();
        }
    }
}