using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Playables;
using UnityEngine.UI;

public class MainMenu : BaseBehaviour
{
   public GameObject       UI_Root;
   public PlayableDirector Cutscene_Director;
   
   private List<MainMenuCar> m_Cars = new List<MainMenuCar>();
   private Coroutine         m_Cutscene_Routine;
   
   public void StartGame()
   {
      if (m_Cutscene_Routine != null)
         return;
      
      
      Cursor.lockState = CursorLockMode.None;
      Cursor.visible   = true;
      
      m_Cutscene_Routine = StartCoroutine(StartGameRoutine());
   }

   private IEnumerator StartGameRoutine()
   {
      int Cars_Len = GetBehaviours(m_Cars);
      for (int i = 0; i < Cars_Len; i++)
         m_Cars[i].StopMoving = true; //Will force the cars to stop moving

      yield return FadeUI();
      
      //Wait for the Cars to stop moving
      for (int i = 0; i < Cars_Len; i++)
      {
         while (m_Cars[i].enabled)
            yield return new WaitForEndOfFrame();
      }
      
      Cutscene_Director.Play();

      //Wait for the cutscene to finish
      while (Cutscene_Director.state == PlayState.Playing)
         yield return new WaitForEndOfFrame();
      
      //Load the Level!
      LoadingManager.LoadLevel("BlockOut");
   }

   private IEnumerator FadeUI()
   {
      Graphic[] UI_Elements        = UI_Root.GetComponentsInChildren<Graphic>();
      float[]   UI_Elements_Alphas = new float[UI_Elements.Length];
      
      for (int i = 0; i < UI_Elements_Alphas.Length; i++)
         UI_Elements_Alphas[i] = UI_Elements[i].color.a;

      float Alpha = 0f;
      while (Alpha < 1f)
      {
         UpdateAlpha();
         yield return new WaitForEndOfFrame();
         Alpha += Time.deltaTime * 0.5f;
      }

      Alpha = 1f;
      UpdateAlpha();
      
      UI_Root.SetActive(false);

      void UpdateAlpha()
      {
         for (int i = 0; i < UI_Elements_Alphas.Length; i++)
         {
            Color c = UI_Elements[i].color;
            c.a                   = Mathf.Lerp(UI_Elements_Alphas[i], 0f, Alpha);
            UI_Elements[i].color  = c;
         }
      }
   }
}
