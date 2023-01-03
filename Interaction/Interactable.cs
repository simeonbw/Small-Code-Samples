using Sirenix.OdinInspector;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;

public class Interactable : MonoBehaviour, IInteractable
{
    [SerializeField] private string _interactMessage;
    public string InteractMessage { get => _interactMessage; }

    [SerializeField] private bool _enabled = true;
    public bool Enabled { get => _enabled; set => _enabled = value; }

    public UnityEvent OnInteract = new UnityEvent();

    public void SetMessage(string newMessage)
    {
        _interactMessage = newMessage;
    }

    public void Interact()
    {
        OnInteract?.Invoke();
    }
}
