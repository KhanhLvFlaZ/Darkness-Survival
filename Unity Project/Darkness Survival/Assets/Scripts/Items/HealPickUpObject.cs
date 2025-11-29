using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class HealPickUpObject : MonoBehaviour, IPickUpObject
{
    [SerializeField] float healAmount;
    [SerializeField] float healMultiplier = 2f;

    public void OnPickUp(Character character)
    {
        character.Heal(healAmount * healMultiplier);
    }
}
