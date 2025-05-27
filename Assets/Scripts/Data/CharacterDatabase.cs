// Assets/Scripts/Data/CharacterDatabase.cs

using System;
using System.Collections.Generic;
using UnityEngine;
using Assets.Scripts.Data;


// SCRIPTABLEOBJECT: Place this in Assets/Scripts/Data/
[CreateAssetMenu(fileName = "CharacterDatabase", menuName = "Vespeyr/Character Database", order = 0)]
public class CharacterDatabase : ScriptableObject
{
    public List<CharacterDTO> Characters = new List<CharacterDTO>();
}



