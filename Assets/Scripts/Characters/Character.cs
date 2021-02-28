using System;
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

   public abstract void OnEnterDialogue();
   public abstract void OnLeaveDialogue();

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
