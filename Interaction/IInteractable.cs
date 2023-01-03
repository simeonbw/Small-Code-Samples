using UnityEngine;

public interface IInteractable
{
    public string InteractMessage { get; }
    public bool Enabled { get; }
    public void Interact();
}
