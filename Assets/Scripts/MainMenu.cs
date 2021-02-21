using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class MainMenu : BaseBehaviour
{
   public void LoadSampleScene()
   {
      LoadingManager.LoadLevel("Sample");
   }
}
