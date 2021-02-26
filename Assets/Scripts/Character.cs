using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Character : BaseBehaviour
{
   public override eUpdateFlags UpdateFlags => eUpdateFlags.RequireUpdate;

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

   public override void OnUpdate(float DeltaTime) => _ActiveState?.OnUpdate();
   private void OnAnimatorIK(int LayerIDx)        => _ActiveState?.OnAnimatorIK(LayerIDx);
}

public class CharacterState
{
   public InputManager InputManager => InputManager.Instance;
   
   public virtual void OnEnter()  {}
   public virtual void OnUpdate() {}
   public virtual void OnAnimatorIK(int LayerIDx) {}
   public virtual void OnLeave() {}
}
