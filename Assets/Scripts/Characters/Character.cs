﻿using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[Serializable]
public struct DialogueBool
{
   public string Key;
   public bool   Value;
}

public abstract class Character : BaseBehaviour
{
   public override eUpdateFlags UpdateFlags => eUpdateFlags.RequireUpdate;

   public string         Name; //Used to match in dialogue
   public DialogueBool[] Bools; //Used to determine the story context
   public List<Item>     Inventory = new List<Item>(); //Serialized so characters can start the game with items in their inventory

   [NonSerialized] public Animator AnimController;
   
   private CharacterState _ActiveState;
   public CharacterState ActiveState
   {
      get => _ActiveState;
      set
      {
         if (_ActiveState == value)
            return;
         _ActiveState?.OnLeave();
         _ActiveState = value;
         _ActiveState?.OnEnter();
      }
   }

   public override void OnEnable()
   {
      base.OnEnable();

      AnimController = GetComponent<Animator>();
   }

   public void AddItem(Item NewItem)
   {
      Inventory.Add(NewItem);
      NewItem.OnAddedToInventory(this);
   }

   public Item RemoveItem(int IDx)
   {
      if (IDx < 0 || IDx > Inventory.Count)
         return null;

      Item Target = Inventory[IDx];
      RemoveItem(Target);

      return Target;
   }
   
   public void RemoveItem(Item NewItem)
   {
      if (!Inventory.Remove(NewItem))
         return;
      
      NewItem.OnRemoveFromInventory(this);
   }
   
   //using the item name is bad practice, IE if we wanted to do localization. But its fine for this jam
   public int GetInventoryItemIDx(string ItemName)
   {
      int Inventory_Len = Inventory.Count;
      for (int i = 0; i < Inventory_Len; i++)
      {
         if (Inventory[i].ItemName == ItemName)
            return i;
      }

      return -1;
   }

   public abstract void OnEnterDialogue(Character[] Participants);
   public abstract void OnLeaveDialogue(Character[] Participants);

   public static T GetCharacterOutOfArray<T>(Character[] Characters) where T : Character
   {
      int Characters_Len = Characters.Length;
      for (int i = 0; i < Characters_Len; i++)
      {
         if (Characters[i] is T)
            return (T) Characters[i];
      }

      return default;
   }
   
   public int GetDialogueBoolIDx(string Key)
   {
      int Len = Bools.Length;
      for (int i = 0; i < Len; i++)
      {
         if (Bools[i].Key == Key)
            return i;
      }

      return -1;
   }


   public override void OnUpdate(float DeltaTime)    => _ActiveState?.OnUpdate();
   protected virtual void OnAnimatorIK(int LayerIDx) => _ActiveState?.OnAnimatorIK(LayerIDx);
}

public class CharacterState
{
   public Character Character;
   public float     TimeInState = 0;
   
   public InputManager InputManager   => InputManager.Instance;
   public Animator     AnimController => Character.AnimController;

   public CharacterState(Character Character) => this.Character = Character;

   public virtual void OnEnter()  => TimeInState = 0;
   public virtual void OnUpdate() => TimeInState += Time.unscaledDeltaTime;
   public virtual void OnAnimatorIK(int LayerIDx) {}
   public virtual void OnLeave() {}
}
