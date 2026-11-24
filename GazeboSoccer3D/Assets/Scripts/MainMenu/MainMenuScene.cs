using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement; // Scene yönetimi için gerekli kütüphane

public class MainMenuScene : MonoBehaviour
{
    public void LoadMultiplayerScene()
    {
        SceneManager.LoadScene("Menu");
    }

    public void LoadVSScene()
    {
        SceneManager.LoadScene("1V1");
    }

    public void LoadAgentsScene()
    {
        SceneManager.LoadScene("Agents");
    }
}
